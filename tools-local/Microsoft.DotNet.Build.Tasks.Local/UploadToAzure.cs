// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using System.Net.Http;

using Microsoft.Build.Framework;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Microsoft.DotNet.Build.Tasks
{

    public partial class UploadToAzure : Utility.AzureConnectionStringBuildTask
    {
        private static readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        private static readonly CancellationToken CancellationToken = TokenSource.Token;

        /// <summary>
        /// The name of the container to access.  The specified name must be in the correct format, see the
        /// following page for more info.  https://msdn.microsoft.com/en-us/library/azure/dd135715.aspx
        /// </summary>
        [Required]
        public string ContainerName { get; set; }

        /// <summary>
        /// An item group of files to upload.  Each item must have metadata RelativeBlobPath
        /// that specifies the path relative to ContainerName where the item will be uploaded.
        /// </summary>
        [Required]
        public ITaskItem[] Items { get; set; }

        /// <summary>
        /// Indicates if the destination blob should be overwritten if it already exists.  The default if false.
        /// </summary>
        public bool Overwrite { get; set; } = false;

        /// <summary>
        /// Specifies the maximum number of clients to concurrently upload blobs to azure
        /// </summary>
        public int MaxClients { get; set; } = 8;

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
            if (Log.HasLoggedErrors)
            {
                return false;
            }

            Log.LogMessage(
                MessageImportance.Normal, 
                "Begin uploading blobs to Azure account {0} in container {1}.", 
                AccountName, 
                ContainerName);

            if (Items.Length == 0)
            {
                Log.LogError("No items were provided for upload.");
                return false;
            }

            // first check what blobs are present
            string checkListUrl = string.Format(
                "https://{0}.blob.core.windows.net/{1}?restype=container&comp=list", 
                AccountName, 
                ContainerName);

            HashSet<string> blobsPresent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    Func<HttpRequestMessage> createRequest = () =>
                    {
                        DateTime dt = DateTime.UtcNow;
                        var req = new HttpRequestMessage(HttpMethod.Get, checkListUrl);
                        req.Headers.Add(Utility.AzureHelper.DateHeaderString, dt.ToString("R", CultureInfo.InvariantCulture));
                        req.Headers.Add(Utility.AzureHelper.VersionHeaderString, Utility.AzureHelper.StorageApiVersion);
                        req.Headers.Add(Utility.AzureHelper.AuthorizationHeaderString, Utility.AzureHelper.AuthorizationHeader(
                            AccountName,
                            AccountKey,
                            "GET",
                            dt,
                            req));
                        return req;
                    };

                    Log.LogMessage(MessageImportance.Low, "Sending request to check whether Container blobs exist");
                    using (HttpResponseMessage response = await Utility.AzureHelper.RequestWithRetry(Log, client, createRequest))
                    {
                        var doc = new XmlDocument();
                        doc.LoadXml(await response.Content.ReadAsStringAsync());

                        XmlNodeList nodes = doc.DocumentElement.GetElementsByTagName("Blob");

                        foreach (XmlNode node in nodes)
                        {
                            blobsPresent.Add(node["Name"].InnerText);
                        }

                        Log.LogMessage(MessageImportance.Low, "Received response to check whether Container blobs exist");
                    }
                }

                using (var clientThrottle = new SemaphoreSlim(this.MaxClients, this.MaxClients))
                {
                    await ThreadingTask.WhenAll(Items.Select(item => UploadAsync(ct, item, blobsPresent, clientThrottle)));
                }

                Log.LogMessage(MessageImportance.Normal, "Upload to Azure is complete, a total of {0} items were uploaded.", Items.Length);
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, true);
            }
            return !Log.HasLoggedErrors;
        }

        private async ThreadingTask UploadAsync(CancellationToken ct, ITaskItem item, HashSet<string> blobsPresent, SemaphoreSlim clientThrottle)
        {
            if (ct.IsCancellationRequested)
            {
                Log.LogError("Task UploadToAzure cancelled");
                ct.ThrowIfCancellationRequested();
            }

            string relativeBlobPath = item.GetMetadata("RelativeBlobPath");
            if (string.IsNullOrEmpty(relativeBlobPath))
                throw new Exception(string.Format("Metadata 'RelativeBlobPath' is missing for item '{0}'.", item.ItemSpec));

            if (!File.Exists(item.ItemSpec))
                throw new Exception(string.Format("The file '{0}' does not exist.", item.ItemSpec));

            if (!Overwrite && blobsPresent.Contains(relativeBlobPath))
                throw new Exception(string.Format("The blob '{0}' already exists.", relativeBlobPath));

            await clientThrottle.WaitAsync();

            try
            {
                Log.LogMessage("Uploading {0} to {1}.", item.ItemSpec, ContainerName);
                Utility.UploadClient uploadClient = new Utility.UploadClient(Log);
                await
                    uploadClient.UploadBlockBlobAsync(
                        ct,
                        AccountName,
                        AccountKey,
                        ContainerName,
                        item.ItemSpec,
                        relativeBlobPath);
            }
            finally
            {
                clientThrottle.Release();
            }
        }
    }
}
