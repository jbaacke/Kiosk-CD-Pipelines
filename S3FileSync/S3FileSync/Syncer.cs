using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

using System.Diagnostics;
using Kiosk.Component.Logging;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace S3FileSync
{

    class Syncer
    {

        // Change the AWSProfileName to the profile you want to use in the App.config file.
        // See http://aws.amazon.com/credentials  for more details.
        // You must also sign up for an Amazon S3 account for this to work
        // See http://aws.amazon.com/s3/ for details on creating an Amazon S3 account
        // Change the bucketName and keyName fields to values that match your bucketname and keyname
        private static AmazonS3Client _s3client;

        // Declare my variables out here and 
        GetObjectRequest _getObjectRequest = new GetObjectRequest();
        GetObjectResponse _getObjectResponse = new GetObjectResponse();

        GetObjectTaggingRequest _getTagRequest = new GetObjectTaggingRequest();
        GetObjectTaggingResponse _getTagResponse = new GetObjectTaggingResponse();

        ListObjectsRequest _listObjectsRequest = new ListObjectsRequest();
        ListObjectsResponse _listObjectsResponse = new ListObjectsResponse();

        int _KArtifactID = new Int32();

        string _localPath = "";

        string _fullFilePath = "";

        WinEventLogger _winEventLogger = new WinEventLogger();

        string[] _localfiles = { "Why", "no", "constructor?", "Stupid", "C#" };

        List<string> _requiredConfigSettings = new List<string>( new string[] { "AccessKey", "SecretAccessKey", "BucketName", "S3FolderPath", "LocalFolderPath" });

        List<string> _missingConfigSettings = new List<string>();

        public void Start()
        {

            #region Initial configuration
            // Start the logger
            var logger = new Logger("S3SyncLogger");
            logger.LogInfo("Starting S3Sync");

            // Pull the things out of my app config file
            NameValueCollection appConfig = ConfigurationManager.AppSettings;
            logger.LogInfo("Config settings read");
            
            // Check and make sure I have all the nessisary config settings
            foreach (string setting in _requiredConfigSettings)
            {
                if (appConfig[setting] == null)
                {
                    logger.LogError($"The variable {setting} is not set.");
                    _missingConfigSettings.Add(setting);
                }
            }

            // If there were missing settings I want to stop the program now. 
            if (_missingConfigSettings.Count > 0)
            {
                logger.LogError($"The following values were not set in the app.config file: {_missingConfigSettings.ToString()}");
                return;
            }

            // Configure my s3 client with the access key and secret access key from the config
            _s3client = new AmazonS3Client(appConfig["AccessKey"], appConfig["SecretAccessKey"]);
            logger.LogInfo("S3Client Configured");

            // Set up my _listObjectRequest request once
            _listObjectsRequest.BucketName = appConfig["BucketName"];
            _listObjectsRequest.Prefix = appConfig["S3FolderPath"];
            logger.LogInfo("ListObject interactions configured");

            // Set up the get object request once
            _getObjectRequest.BucketName = appConfig["BucketName"];
            logger.LogInfo("GetObject request configured");

            // Set up the get Tagg request once
            _getTagRequest.BucketName = appConfig["BucketName"];
            logger.LogInfo("GetObject request configured");

            // Identify my target directory
            _localPath = appConfig["LocalFolderPath"];

            #endregion

            while (true)
            {

                // Get the list of files in my S3 bucket.
                try
                {
                    logger.LogTrace("Getting a list of the objects in the bucket");
                    _listObjectsResponse = _s3client.ListObjects(_listObjectsRequest);
                }
                catch (Exception e)
                {
                    logger.LogError($"{e.Message}", e);
                    throw;
                }

                // For each of the files in the Bucket, check to see if they are in my folder
                foreach (S3Object entry in _listObjectsResponse.S3Objects)
                {

                    // Set the local full filepath
                    _fullFilePath = _localPath + "//" + entry.Key;

                    // Skip folders as they will be automatically created if they have any objects in them. 
                    if (entry.Key.Last<char>().Equals('/'))
                    {
                        continue;
                    }

                    // See if the file already exists
                    if (File.Exists(_fullFilePath))
                    {
                        // If the last write time of the local file is greater do nothing
                        if (File.GetLastWriteTimeUtc(_fullFilePath) > entry.LastModified.ToUniversalTime())
                        {
                            continue;
                        }
                    }

                    // Set the key of the get object request to the object I want
                    _getObjectRequest.Key = entry.Key;
                    _getTagRequest.Key = entry.Key;

                    // Get the taggs associated with the object and run the required logic against them.
                    try
                    {

                        // Get the taggs associated with the entry
                        logger.LogInfo(String.Format("Getting taggs for {0}.", entry.Key));
                        _getTagResponse = _s3client.GetObjectTagging(_getTagRequest);

                        // I am interested in two taggs in particular 
                        // Update: Determines wether or not I should update the file
                        // KArtifactID: is an integer which will get passed to the windows event logger. This will be used to determine the alert to associate the upload with.

                        // If the Update Tag does not exist I do not want to update 
                        if (!_getTagResponse.Tagging.Exists(x => x.Key == "Update"))
                        {
                            logger.LogInfo(String.Format("Taggs retrieved for {0}.", entry.Key));
                            continue;
                        }

                        // If the Update tag does exist and is note true I do not want to update
                        if (_getTagResponse.Tagging.Find(x => x.Key == "Update").Value.ToLower() != "true")
                        {
                            logger.LogInfo(String.Format("The object {0} was not updated because the Update tag was not true.", entry.Key));
                            continue;
                        }

                        // If there is no KArtifactID I want to skip it
                        if (!_getTagResponse.Tagging.Exists(x => x.Key == "KArtifactID"))
                        {
                            logger.LogInfo(String.Format("The object {0} was not updated because it did not contain the KArtifactID Tag.", entry.Key));
                            continue;
                        }

                        // If the KArtifactID cannot be converted into an int I do not want to update the file
                        try
                        {
                            _KArtifactID = Convert.ToInt32(_getTagResponse.Tagging.Find(x => x.Key == "KArtifactID").Value);
                        }
                        catch (Exception e)
                        {
                            logger.LogInfo(String.Format("The object {0} was not updated because it's KArtifactID was not an Int.", entry.Key));
                            logger.LogError($"{e.Message}", e);
                            continue;
                        }
                        

                        // Let the logger know I finished grabbing the taggs
                        logger.LogInfo(String.Format("Taggs retrieved for {0}.", entry.Key));


                        // Preform the get object operation
                        logger.LogInfo(String.Format("Getting {0} last updated time is {1}.", entry.Key, entry.LastModified.ToUniversalTime()));
                        _getObjectResponse = _s3client.GetObject(_getObjectRequest);
                        _getObjectResponse.WriteResponseStreamToFile(_fullFilePath);
                        logger.LogInfo(String.Format("Retrieved {0}.", entry.Key));

                        // Write the key to the event log so I can trigger off it in Kaseya.
                        logger.LogInfo(string.Format("Writing to event log that {0} has completed sync", entry.Key));
                        _winEventLogger.ELog(entry.Key, _KArtifactID);
                        
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"An error occured while trying to handle item: {entry.Key}");
                        logger.LogError($"{e.Message}", e);
                        throw e;
                    }
                    
                }

                // I will only check every 30 seconds
                logger.LogTrace("Update complete.");
                System.Threading.Thread.Sleep(30000);
            }
        }
    }
}
