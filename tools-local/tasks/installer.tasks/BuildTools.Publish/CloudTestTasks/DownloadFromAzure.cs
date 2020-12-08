// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Build.CloudTestTasks
{
    public sealed class DownloadFromAzure : AzureConnectionStringBuildTask
    {
        /// <summary>
        /// The name of the container to access.  The specified name must be in the correct format, see the
        /// following page for more info.  https://msdn.microsoft.com/en-us/library/azure/dd135715.aspx
        /// </summary>
        [Required]
        public string ContainerName { get; set; }

        /// <summary>
        /// Directory to download blob files to.
        /// </summary>
        [Required]
        public string DownloadDirectory { get; set; }

        public string BlobNamePrefix { get; set; }

        public string BlobNameExtension { get; set; }

        public ITaskItem[] BlobNames { get; set; }

        public bool DownloadFlatFiles { get; set; }

        public int MaxClients { get; set; } = 8;

        private static readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        private static readonly CancellationToken CancellationToken = TokenSource.Token;

        public void Cancel()
        {
            TokenSource.Cancel();
        }

        public override bool Execute()
        {
            return ExecuteAsync(CancellationToken).GetAwaiter().GetResult();
        }

        public async Task<bool> ExecuteAsync(CancellationToken ct)
        {
            ParseConnectionString();
            // If the connection string AND AccountKey & AccountName are provided, error out.
            if (Log.HasLoggedErrors)
            {
                return false;
            }

            Log.LogMessage(MessageImportance.Normal, "Downloading contents of container {0} from storage account '{1}' to directory {2}.",
                ContainerName, AccountName, DownloadDirectory);

            try
            {
                List<string> blobNames = new List<string>();
                if (BlobNames == null)
                {
                    ListAzureBlobs listAzureBlobs = new ListAzureBlobs()
                    {
                        AccountName = AccountName,
                        AccountKey = AccountKey,
                        ContainerName = ContainerName,
                        FilterBlobNames = BlobNamePrefix,
                        BuildEngine = this.BuildEngine,
                        HostObject = this.HostObject
                    };
                    listAzureBlobs.Execute();
                    blobNames = listAzureBlobs.BlobNames.ToList();
                }
                else
                {
                    blobNames = BlobNames.Select(b => b.ItemSpec).ToList<string>();
                    if (BlobNamePrefix != null)
                    {
                        blobNames = blobNames.Where(b => b.StartsWith(BlobNamePrefix)).ToList<string>();
                    }
                }

                if (BlobNameExtension != null)
                {
                    blobNames = blobNames.Where(b => Path.GetExtension(b) == BlobNameExtension).ToList<string>();
                }

                if (!Directory.Exists(DownloadDirectory))
                {
                    Directory.CreateDirectory(DownloadDirectory);
                }
                using (var clientThrottle = new SemaphoreSlim(this.MaxClients, this.MaxClients))
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    await Task.WhenAll(blobNames.Select(item => DownloadItem(client, ct, item, clientThrottle)));
                }
            }
            catch (Exception e)
            {
                Log.LogError(e.ToString());
            }
            return !Log.HasLoggedErrors;
        }

        private async Task DownloadItem(HttpClient client, CancellationToken ct, string blob, SemaphoreSlim clientThrottle)
        {
            await clientThrottle.WaitAsync();
            string filename = string.Empty;
            try {
                Log.LogMessage(MessageImportance.Low, "Downloading BLOB - {0}", blob);
                string blobUrl = AzureHelper.GetBlobRestUrl(AccountName, ContainerName, blob);
                filename = Path.Combine(DownloadDirectory, Path.GetFileName(blob));

                if (!DownloadFlatFiles)
                {
                    int dirIndex = blob.LastIndexOf("/");
                    string blobDirectory = string.Empty;
                    string blobFilename = string.Empty;

                    if (dirIndex == -1)
                    {
                        blobFilename = blob;
                    }
                    else
                    {
                        blobDirectory = blob.Substring(0, dirIndex);
                        blobFilename = blob.Substring(dirIndex + 1);

                        // Trim blob name prefix (directory part) from download to blob directory
                        if (BlobNamePrefix != null)
                        {
                            if (BlobNamePrefix.Length > dirIndex)
                            {
                                BlobNamePrefix = BlobNamePrefix.Substring(0, dirIndex);
                            }
                            blobDirectory = blobDirectory.Substring(BlobNamePrefix.Length);
                        }
                    }
                    string downloadBlobDirectory = Path.Combine(DownloadDirectory, blobDirectory);
                    if (!Directory.Exists(downloadBlobDirectory))
                    {
                        Directory.CreateDirectory(downloadBlobDirectory);
                    }
                    filename = Path.Combine(downloadBlobDirectory, blobFilename);
                }

                var createRequest = AzureHelper.RequestMessage("GET", blobUrl, AccountName, AccountKey);

                using (HttpResponseMessage response = await AzureHelper.RequestWithRetry(Log, client, createRequest))
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
            catch (PathTooLongException)
            {
                Log.LogError($"Unable to download blob as it exceeds the maximum allowed path length. Path: {filename}. Length:{filename.Length}");
            }
            catch (Exception ex)
            {
                Log.LogError(ex.ToString());
            }
            finally
            {
                clientThrottle.Release();
            }
        }
    }
}
