// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class DownloadBlobFromAzure : Utility.AzureConnectionStringBuildTask
    {
        /// <summary>
        /// The name of the container to access.  The specified name must be in the correct format, see the
        /// following page for more info.  https://msdn.microsoft.com/en-us/library/azure/dd135715.aspx
        /// </summary>
        [Required]
        public string ContainerName { get; set; }

        [Required]
        public string BlobName { get; set; }

        /// <summary>
        /// Directory to download blob files to.
        /// </summary>
        [Required]
        public string DownloadDirectory { get; set; }

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

            Log.LogMessage(MessageImportance.Normal, "Downloading blob {0} from container {1} at storage account '{2}' to directory {3}.",
                BlobName, ContainerName, AccountName, DownloadDirectory);

            List<string> blobsNames = new List<string>();
            string urlListBlobs = $"https://{AccountName}.blob.core.windows.net/{ContainerName}/{BlobName}";

            Log.LogMessage(MessageImportance.Low, "Sending request to list blobsNames for container '{0}'.", ContainerName);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Func<HttpRequestMessage> createRequest = () =>
                    {
                        DateTime dateTime = DateTime.UtcNow;
                        var request = new HttpRequestMessage(HttpMethod.Get, urlListBlobs);
                        request.Headers.Add(Utility.AzureHelper.DateHeaderString, dateTime.ToString("R", CultureInfo.InvariantCulture));
                        request.Headers.Add(Utility.AzureHelper.VersionHeaderString, Utility.AzureHelper.StorageApiVersion);
                        request.Headers.Add(Utility.AzureHelper.AuthorizationHeaderString, Utility.AzureHelper.AuthorizationHeader(
                                AccountName,
                                AccountKey,
                                "GET",
                                dateTime,
                                request));
                        return request;
                    };

                    // track the number of blobs that fail to download
                    string blob = Path.GetFileName(BlobName);
                    string filename = Path.Combine(DownloadDirectory, blob);
                    using (HttpResponseMessage response = await Utility.AzureHelper.RequestWithRetry(Log, client, createRequest))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            // Blobs can be files but have the name of a directory.  We'll skip those and log something weird happened.
                            if (!string.IsNullOrEmpty(Path.GetFileName(filename)))
                            {
                                Stream responseStream = await response.Content.ReadAsStreamAsync();

                                using (FileStream sourceStream = File.Open(filename, FileMode.Create))
                                {
                                    responseStream.CopyTo(sourceStream);
                                }
                            }
                            else
                            {
                                Log.LogWarning($"Unable to download blob '{blob}' as it has a directory-like name.  This may cause problems if it was needed.");
                            }
                        }
                        else
                        {
                            Log.LogError("Failed to retrieve blob {0}, the status code was {1}", blob, response.StatusCode);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                }
                return !Log.HasLoggedErrors;
            }
        }

        public static bool Execute(string accountName,
                                       string accountKey,
                                       string connectionString,
                                       string containerName,
                                       string blobName,
                                       string downloadDirectory,
                                       IBuildEngine buildengine,
                                       ITaskHost taskHost)
        {
            DownloadBlobFromAzure downloadBlobFromAzure = new DownloadBlobFromAzure()
            {
                AccountName = accountName,
                AccountKey = accountKey,
                ContainerName = containerName,
                BlobName = blobName,
                DownloadDirectory = downloadDirectory,
                BuildEngine = buildengine,
                HostObject = taskHost
            };
            return downloadBlobFromAzure.Execute();
        }

        public static Task<bool> ExecuteAsync(string accountName,
                                       string accountKey,
                                       string connectionString,
                                       string containerName,
                                       string blobName,
                                       string downloadDirectory,
                                       IBuildEngine buildengine,
                                       ITaskHost taskHost)
        {
            DownloadBlobFromAzure downloadBlobFromAzure = new DownloadBlobFromAzure()
            {
                AccountName = accountName,
                AccountKey = accountKey,
                ContainerName = containerName,
                BlobName = blobName,
                DownloadDirectory = downloadDirectory,
                BuildEngine = buildengine,
                HostObject = taskHost
            };
            return downloadBlobFromAzure.ExecuteAsync();
        }
    }
}
