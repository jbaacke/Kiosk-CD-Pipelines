using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.IO;

using Amazon.Lambda.Core;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Amazon.S3;
using Amazon.S3.Util;
using Amazon.S3.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SelfUpdatingMicroservice
{
    public class Function
    {

        

        #region Function Constructors
        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            //S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            //this.S3Client = s3Client;
        }
        #endregion


        public async Task<string> FunctionHandler(S3EventNotification evnt, ILambdaContext context)
        {

            // S3 Client and the associated Call Variables
            IAmazonS3 S3Client = new AmazonS3Client();
            S3EventNotification.S3Entity s3Event = new S3EventNotification.S3Entity();
            GetObjectRequest ParameterFileRequest = new GetObjectRequest();
            GetObjectResponse ParameterFileResponse = new GetObjectResponse();
            GetPreSignedUrlRequest PresignedKeyRequest = new GetPreSignedUrlRequest();
            GetObjectResponse CommitInfoResponse = new GetObjectResponse();

            // Cloudformation Client and associated Variables
            AmazonCloudFormationClient CloudformationClient = new AmazonCloudFormationClient();
            List<Parameter> Parameters = new List<Parameter>();
            DescribeStacksRequest CurrentStacksRequest = new DescribeStacksRequest();
            DescribeStacksResponse CurrentStacksResponse = new DescribeStacksResponse();

            // A flag to determine if I want to create a new stack or update an existing one
            bool newstack = new bool();

            // The name of the stack which will be generated by the deployment process. 
            string TargetStackName;

            // The fixed file name values the deployment process will require. If all three files are not present no deployment will take place. 
            string DeploymentFileName = "master.template";
            string ParameterFileName = "Parameters.txt";
            string CommitInfoFileName = "commitinfo.txt";


            // Write details of the s3 event to my logger
            s3Event = evnt.Records?[0].S3;
            context.Logger.Log("S3 Cloudformation Template Upload Event Recieved. Processing potential deployment.");
            context.Logger.Log("Bucket: " + s3Event.Bucket.Name);
            context.Logger.Log("Key: " + s3Event.Object.Key);
            
            // Preform a check to make sure the filename matches the standard required. 
            if (s3Event.Object.Key.EndsWith(DeploymentFileName))
            {
                context.Logger.Log("S3 Event corresponds to a properly formatted master.template CloudFormation document. Commencing deployment.");
            }
            else
            {
                context.Logger.Log("S3 Event does not match deployment requirements. Candidates for deployment must contain the primary CloudFormation template in a master.template file.");
                return "Impropper filename. No deployment processed";
            }

            // Display the commitinfo from the deployment
            string CommitInfoKeyName = s3Event.Object.Key.Replace(DeploymentFileName, CommitInfoFileName);
            context.Logger.Log($"Looking for accompanying commitinfo file: {CommitInfoKeyName}");
            try
            {
                CommitInfoResponse = await S3Client.GetObjectAsync(s3Event.Bucket.Name, CommitInfoKeyName);
                using (StreamReader reader = new StreamReader(CommitInfoResponse.ResponseStream))
                {
                    string contents = reader.ReadToEnd();
                    context.Logger.Log(contents);

                }
            }
            catch (Exception e)
            {
                context.Logger.Log(e.Message);
                context.Logger.Log("No commitinfo.txt file detected. Aborting Deployment");
                return "No accompanying commitinfo.txt. No deployment Processed";
            }

            // Get and set associated parameters
            string ParameterKeyName = s3Event.Object.Key.Replace(DeploymentFileName, ParameterFileName);
            context.Logger.Log($"Looking for accompanying parameter file: {ParameterKeyName}");
            try
            {
                ParameterFileResponse = await S3Client.GetObjectAsync(s3Event.Bucket.Name, ParameterKeyName);
                StreamReader reader = new StreamReader(ParameterFileResponse.ResponseStream);
                string paramline = reader.ReadLine();
                context.Logger.Log("Parameter file line being processed: " + paramline);
                while (!string.IsNullOrWhiteSpace(paramline))
                {
                    string[] paramstrings = paramline.Split(':');
                    if (paramstrings.Length == 2)
                    {
                        Parameters.Add(new Parameter()
                        {
                            ParameterKey = paramstrings[0],
                            ParameterValue = paramstrings[1]
                        });
                    }
                    paramline = reader.ReadLine();
                    context.Logger.Log("Parameter file line being processed: " + paramline);
                }

            }
            catch (Exception e)
            {
                context.Logger.Log(e.Message);
                context.Logger.Log("No parameter file detected. Aborting Deployment.");
                return "No accompanying commitinfo.txt.No deployment Processed";
            }

            // The name of the stack will be based on the folder structure containing the master.template document. 
            // As an example, a template deployed to the S3 key Knect/RCC/master.template would generate the stack Knect-RCC
            TargetStackName = s3Event.Object.Key.Replace("/", "-");
            TargetStackName = TargetStackName.Replace("-" + DeploymentFileName, ""); 
            context.Logger.Log("Cloudformation Stack Name: " + TargetStackName);
            
            // Gets a presigned url for the cloudformation client so it can access the master.template document.
            PresignedKeyRequest.BucketName = s3Event.Bucket.Name;
            PresignedKeyRequest.Key = s3Event.Object.Key;
            PresignedKeyRequest.Expires = DateTime.Now.AddMinutes(5);
            string PresignedS3Key = S3Client.GetPreSignedURL(PresignedKeyRequest);
            
            // If a stack with the target name already exists I want to update it. Otherwise I want to create a new stack. 
            try
            {
                CurrentStacksRequest.StackName = TargetStackName;
                CurrentStacksResponse = await CloudformationClient.DescribeStacksAsync(CurrentStacksRequest);
                context.Logger.Log("A stack for the target name already exists. The existing stack will be updated.");

                newstack = false;
            }
            catch
            {
                context.Logger.Log("No stack with the target name exists. A new stack will be created.");

                newstack = true;
            }

            foreach (Parameter param in Parameters)
            {
                context.Logger.Log($"Parameter is set Key: {param.ParameterKey} with value {param.ParameterValue}");
            }
            
            // If there is an existing stack I will update it. Otherwise I will create a new stack
            if (newstack == true)
            {
                // Create a new stack
                CreateStackRequest CreateStack = new CreateStackRequest();
                CreateStack.StackName = TargetStackName;
                CreateStack.TemplateURL = PresignedS3Key;
                CreateStack.Parameters = Parameters;
                CreateStack.Capabilities.Add("CAPABILITY_NAMED_IAM");

                await CloudformationClient.CreateStackAsync(CreateStack);

                return "A stack creation request was successfully generated";
            }
            else
            {
                UpdateStackRequest updatereq = new UpdateStackRequest();
                updatereq.StackName = TargetStackName;
                updatereq.TemplateURL = PresignedS3Key;
                updatereq.Parameters = Parameters;
                updatereq.Capabilities.Add("CAPABILITY_NAMED_IAM");

                await CloudformationClient.UpdateStackAsync(updatereq);

                return "A stack update request was successfully generated";
            }



            
        }
    }
}
