// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Build.CloudTestTasks
{
    public partial class CopyAzureBlobToBlob : AzureConnectionStringBuildTask
    {
        [Required]
        public string ContainerName { get; set; }
        [Required]
        public string SourceBlobName { get; set; }
        [Required]
        public string DestinationBlobName { get; set; }

        public override bool Execute()
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }

        public async Task<bool> ExecuteAsync()
        {
            ParseConnectionString();
            if (Log.HasLoggedErrors)
            {
                return false;
            }

            string sourceUrl = AzureHelper.GetBlobRestUrl(AccountName, ContainerName, SourceBlobName);
            string destinationUrl = AzureHelper.GetBlobRestUrl(AccountName, ContainerName, DestinationBlobName);
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Tuple<string, string> leaseAction = new Tuple<string, string>("x-ms-lease-action", "acquire");
                    Tuple<string, string> leaseDuration = new Tuple<string, string>("x-ms-lease-duration", "60" /* seconds */);
                    Tuple<string, string> headerSource = new Tuple<string, string>("x-ms-copy-source", sourceUrl);
                    List<Tuple<string, string>> additionalHeaders = new List<Tuple<string, string>>() { leaseAction, leaseDuration, headerSource };
                    var request = AzureHelper.RequestMessage("PUT", destinationUrl, AccountName, AccountKey, additionalHeaders);
                    using (HttpResponseMessage response = await AzureHelper.RequestWithRetry(Log, client, request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                }
            }
            return false;
        }
        public static bool Execute(string accountName,
                                       string accountKey,
                                       string connectionString,
                                       string containerName,
                                       string sourceBlobName,
                                       string destinationBlobName,
                                       IBuildEngine buildengine,
                                       ITaskHost taskHost)
        {
            CopyAzureBlobToBlob copyAzureBlobToBlob = new CopyAzureBlobToBlob()
            {
                AccountName = accountName,
                AccountKey = accountKey,
                ContainerName = containerName,
                SourceBlobName = sourceBlobName,
                DestinationBlobName = destinationBlobName,
                BuildEngine = buildengine,
                HostObject = taskHost
            };
            return copyAzureBlobToBlob.Execute();
        }
        public static Task<bool> ExecuteAsync(string accountName,
                                              string accountKey,
                                              string connectionString,
                                              string containerName,
                                              string sourceBlobName,
                                              string destinationBlobName,
                                              IBuildEngine buildengine,
                                              ITaskHost taskHost)
        {
            CopyAzureBlobToBlob copyAzureBlobToBlob = new CopyAzureBlobToBlob()
            {
                AccountName = accountName,
                AccountKey = accountKey,
                ContainerName = containerName,
                SourceBlobName = sourceBlobName,
                DestinationBlobName = destinationBlobName,
                BuildEngine = buildengine,
                HostObject = taskHost
            };
            return copyAzureBlobToBlob.ExecuteAsync();
        }
    }
}
