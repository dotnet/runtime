using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class PublishStringToAzureBlob : Utility.AzureConnectionStringBuildTask
    {
        [Required]
        public string BlobName { get; set; }
        [Required]
        public string ContainerName { get; set; }
        [Required]
        public string Content { get; set; }

        public override bool Execute()
        {
            ParseConnectionString();

            string blobUrl = $"https://{AccountName}.blob.core.windows.net/{ContainerName}/{BlobName}";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Tuple<string, string> headerBlobType = new Tuple<string, string>("x-ms-blob-type", "BlockBlob");
                    List<Tuple<string, string>> additionalHeaders = new List<Tuple<string, string>>() { headerBlobType };
                    var request = Utility.AzureHelper.RequestMessage("PUT", blobUrl, AccountName, AccountKey, additionalHeaders, Content);

                    Utility.AzureHelper.RequestWithRetry(Log, client, request).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                }
            }

            return !Log.HasLoggedErrors;
        }

        public static bool Execute(string accountName,
                                   string accountKey,
                                   string connectionString,
                                   string containerName,
                                   string blobName,
                                   string content,
                                   IBuildEngine buildengine,
                                   ITaskHost taskHost)
        {
            PublishStringToAzureBlob publishStringToBlob = new PublishStringToAzureBlob()
            {
                AccountName = accountName,
                AccountKey = accountKey,
                ContainerName = containerName,
                BlobName = blobName,
                Content = content,
                BuildEngine = buildengine,
                HostObject = taskHost
            };
            return publishStringToBlob.Execute();
        }
    }
}
