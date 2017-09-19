using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

using System.Diagnostics;
using Kiosk.Component.Logging;

namespace S3FileSync
{
    class S3Sample
    {



        public static void Main(string[] args)
        {

            Syncer s3syncer = new Syncer();
            s3syncer.Start();
        }




        
    }
}