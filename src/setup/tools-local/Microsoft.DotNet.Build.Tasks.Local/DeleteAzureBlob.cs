using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class DeleteAzureBlob: Utility.AzureConnectionStringBuildTask
    {
        [Required]
        public string ContainerName { get; set; }
        [Required]
        public string BlobName { get; set; }

        public override bool Execute()
        {
            ParseConnectionString();
            if (Log.HasLoggedErrors)
            {
                return false;
            }

            string deleteUrl = $"https://{AccountName}.blob.core.windows.net/{ContainerName}/{BlobName}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Tuple<string, string> snapshots = new Tuple<string, string>("x-ms-lease-delete-snapshots", "include");
                    List<Tuple<string, string>> additionalHeaders = new List<Tuple<string, string>>() { snapshots };
                    var request = Utility.AzureHelper.RequestMessage("DELETE", deleteUrl, AccountName, AccountKey, additionalHeaders);
                    using (HttpResponseMessage response = Utility.AzureHelper.RequestWithRetry(Log, client, request).GetAwaiter().GetResult())
                    {
                        return response.IsSuccessStatusCode;
                    }
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
                                       IBuildEngine buildengine,
                                       ITaskHost taskHost)
            {
            DeleteAzureBlob deleteAzureoBlob = new DeleteAzureBlob()
            {
                AccountName = accountName,
                AccountKey = accountKey,
                ContainerName = containerName,
                BlobName = blobName,
                BuildEngine = buildengine,
                HostObject = taskHost
            };
            return deleteAzureoBlob.Execute();
        }
    }
}
