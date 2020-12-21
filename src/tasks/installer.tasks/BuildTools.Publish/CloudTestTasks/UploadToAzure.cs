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
using System.Xml;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Microsoft.DotNet.Build.CloudTestTasks
{

    public class UploadToAzure : AzureConnectionStringBuildTask, ICancelableTask
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
        /// Enables idempotency when Overwrite is false.
        /// 
        /// false: (default) Attempting to upload an item that already exists fails.
        /// 
        /// true: When an item already exists, download the existing blob to check if it's
        /// byte-for-byte identical to the one being uploaded. If so, pass. If not, fail.
        /// </summary>
        public bool PassIfExistingItemIdentical { get; set; }

        /// <summary>
        /// Specifies the maximum number of clients to concurrently upload blobs to azure
        /// </summary>
        public int MaxClients { get; set; } = 8;

        public int UploadTimeoutInMinutes { get; set; } = 5;

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
            string checkListUrl = $"{AzureHelper.GetContainerRestUrl(AccountName, ContainerName)}?restype=container&comp=list"; 

            HashSet<string> blobsPresent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var createRequest = AzureHelper.RequestMessage("GET", checkListUrl, AccountName, AccountKey);

                    Log.LogMessage(MessageImportance.Low, "Sending request to check whether Container blobs exist");
                    using (HttpResponseMessage response = await AzureHelper.RequestWithRetry(Log, client, createRequest))
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

            UploadClient uploadClient = new UploadClient(Log);

            if (!Overwrite && blobsPresent.Contains(relativeBlobPath))
            {
                if (PassIfExistingItemIdentical &&
                    await ItemEqualsExistingBlobAsync(item, relativeBlobPath, uploadClient, clientThrottle))
                {
                    return;
                }

                throw new Exception(string.Format("The blob '{0}' already exists.", relativeBlobPath));
            }

            string contentType = item.GetMetadata("ContentType");

            await clientThrottle.WaitAsync();

            try
            {
                Log.LogMessage("Uploading {0} to {1}.", item.ItemSpec, ContainerName);
                await
                    uploadClient.UploadBlockBlobAsync(
                        ct,
                        AccountName,
                        AccountKey,
                        ContainerName,
                        item.ItemSpec,
                        relativeBlobPath,
                        contentType,
                        UploadTimeoutInMinutes);
            }
            finally
            {
                clientThrottle.Release();
            }
        }

        private async Task<bool> ItemEqualsExistingBlobAsync(
            ITaskItem item,
            string relativeBlobPath,
            UploadClient client,
            SemaphoreSlim clientThrottle)
        {
            await clientThrottle.WaitAsync();
            try
            {
                return await client.FileEqualsExistingBlobAsync(
                    AccountName,
                    AccountKey,
                    ContainerName,
                    item.ItemSpec,
                    relativeBlobPath,
                    UploadTimeoutInMinutes);
            }
            finally
            {
                clientThrottle.Release();
            }
        }
    }
}
