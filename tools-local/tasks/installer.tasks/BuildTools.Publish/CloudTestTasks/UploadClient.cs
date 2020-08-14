// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.DotNet.Build.CloudTestTasks
{
    public class UploadClient
    {
        private TaskLoggingHelper log;

        public UploadClient(TaskLoggingHelper loggingHelper)
        {
            log = loggingHelper;
        }

        public string EncodeBlockIds(int numberOfBlocks, int lengthOfId)
        {
            string numberOfBlocksString = numberOfBlocks.ToString("D" + lengthOfId);
            if (Encoding.UTF8.GetByteCount(numberOfBlocksString) <= 64)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(numberOfBlocksString);
                return Convert.ToBase64String(bytes);
            }
            else
            {
                throw new Exception("Task failed - Could not encode block id.");
            }
        }

        public async Task UploadBlockBlobAsync(
            CancellationToken ct,
            string AccountName,
            string AccountKey,
            string ContainerName,
            string filePath,
            string destinationBlob,
            string contentType,
            int uploadTimeout,
            string leaseId = "")
        {
            string resourceUrl = AzureHelper.GetContainerRestUrl(AccountName, ContainerName);

            string fileName = destinationBlob;
            fileName = fileName.Replace("\\", "/");
            string blobUploadUrl = resourceUrl + "/" + fileName;
            int size = (int)new FileInfo(filePath).Length;
            int blockSize = 4 * 1024 * 1024; //4MB max size of a block blob
            int bytesLeft = size;
            List<string> blockIds = new List<string>();
            int numberOfBlocks = (size / blockSize) + 1;
            int countForId = 0;
            using (FileStream fileStreamTofilePath = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int offset = 0;

                while (bytesLeft > 0)
                {
                    int nextBytesToRead = (bytesLeft < blockSize) ? bytesLeft : blockSize;
                    byte[] fileBytes = new byte[blockSize];
                    int read = fileStreamTofilePath.Read(fileBytes, 0, nextBytesToRead);

                    if (nextBytesToRead != read)
                    {
                        throw new Exception(string.Format(
                            "Number of bytes read ({0}) from file {1} isn't equal to the number of bytes expected ({2}) .",
                            read, fileName, nextBytesToRead));
                    }

                    string blockId = EncodeBlockIds(countForId, numberOfBlocks.ToString().Length);

                    blockIds.Add(blockId);
                    string blockUploadUrl = blobUploadUrl + "?comp=block&blockid=" + WebUtility.UrlEncode(blockId);

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Clear();

                        // In random occassions the request fails if the network is slow and it takes more than 100 seconds to upload 4MB. 
                        client.Timeout = TimeSpan.FromMinutes(uploadTimeout);
                        Func<HttpRequestMessage> createRequest = () =>
                        {
                            DateTime dt = DateTime.UtcNow;
                            var req = new HttpRequestMessage(HttpMethod.Put, blockUploadUrl);
                            req.Headers.Add(
                                AzureHelper.DateHeaderString,
                                dt.ToString("R", CultureInfo.InvariantCulture));
                            req.Headers.Add(AzureHelper.VersionHeaderString, AzureHelper.StorageApiVersion);
                            if (!string.IsNullOrWhiteSpace(leaseId))
                            {
                                log.LogMessage($"Sending request: {leaseId} {blockUploadUrl}");
                                req.Headers.Add("x-ms-lease-id", leaseId);
                            }
                            req.Headers.Add(
                                AzureHelper.AuthorizationHeaderString,
                                AzureHelper.AuthorizationHeader(
                                    AccountName,
                                    AccountKey,
                                    "PUT",
                                    dt,
                                    req,
                                    string.Empty,
                                    string.Empty,
                                    nextBytesToRead.ToString(),
                                    string.Empty));

                            Stream postStream = new MemoryStream();
                            postStream.Write(fileBytes, 0, nextBytesToRead);
                            postStream.Seek(0, SeekOrigin.Begin);
                            req.Content = new StreamContent(postStream);
                            return req;
                        };

                        log.LogMessage(MessageImportance.Low, "Sending request to upload part {0} of file {1}", countForId, fileName);

                        using (HttpResponseMessage response = await AzureHelper.RequestWithRetry(log, client, createRequest))
                        {
                            log.LogMessage(
                                MessageImportance.Low,
                                "Received response to upload part {0} of file {1}: Status Code:{2} Status Desc: {3}",
                                countForId,
                                fileName,
                                response.StatusCode,
                                await response.Content.ReadAsStringAsync());
                        }
                    }

                    offset += read;
                    bytesLeft -= nextBytesToRead;
                    countForId += 1;
                }
            }

            string blockListUploadUrl = blobUploadUrl + "?comp=blocklist";

            using (HttpClient client = new HttpClient())
            {
                Func<HttpRequestMessage> createRequest = () =>
                {
                    DateTime dt1 = DateTime.UtcNow;
                    var req = new HttpRequestMessage(HttpMethod.Put, blockListUploadUrl);
                    req.Headers.Add(AzureHelper.DateHeaderString, dt1.ToString("R", CultureInfo.InvariantCulture));
                    req.Headers.Add(AzureHelper.VersionHeaderString, AzureHelper.StorageApiVersion);
                    if (string.IsNullOrEmpty(contentType))
                    {
                        contentType = DetermineContentTypeBasedOnFileExtension(filePath);
                    }
                    if (!string.IsNullOrEmpty(contentType))
                    {
                        req.Headers.Add(AzureHelper.ContentTypeString, contentType);
                    }
                    string cacheControl = DetermineCacheControlBasedOnFileExtension(filePath);
                    if (!string.IsNullOrEmpty(cacheControl))
                    {
                        req.Headers.Add(AzureHelper.CacheControlString, cacheControl);
                    }

                    var body = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><BlockList>");
                    foreach (object item in blockIds)
                        body.AppendFormat("<Latest>{0}</Latest>", item);

                    body.Append("</BlockList>");
                    byte[] bodyData = Encoding.UTF8.GetBytes(body.ToString());
                    if (!string.IsNullOrWhiteSpace(leaseId))
                    {
                        log.LogMessage($"Sending list request: {leaseId} {blockListUploadUrl}");
                        req.Headers.Add("x-ms-lease-id", leaseId);
                    }
                    req.Headers.Add(
                        AzureHelper.AuthorizationHeaderString,
                        AzureHelper.AuthorizationHeader(
                            AccountName,
                            AccountKey,
                            "PUT",
                            dt1,
                            req,
                            string.Empty,
                            string.Empty,
                            bodyData.Length.ToString(),
                            string.Empty));

                    Stream postStream = new MemoryStream();
                    postStream.Write(bodyData, 0, bodyData.Length);
                    postStream.Seek(0, SeekOrigin.Begin);
                    req.Content = new StreamContent(postStream);
                    return req;
                };

                using (HttpResponseMessage response = await AzureHelper.RequestWithRetry(log, client, createRequest))
                {
                    log.LogMessage(
                        MessageImportance.Low,
                        "Received response to combine block list for file {0}: Status Code:{1} Status Desc: {2}",
                        fileName,
                        response.StatusCode,
                        await response.Content.ReadAsStringAsync());
                }
            }
        }

        public async Task<bool> FileEqualsExistingBlobAsync(
            string accountName,
            string accountKey,
            string containerName,
            string filePath,
            string destinationBlob,
            int uploadTimeout)
        {
            using (var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(uploadTimeout)
            })
            {
                log.LogMessage(
                    MessageImportance.Low,
                    $"Downloading blob {destinationBlob} to check if identical.");

                string blobUrl = AzureHelper.GetBlobRestUrl(accountName, containerName, destinationBlob);
                var createRequest = AzureHelper.RequestMessage("GET", blobUrl, accountName, accountKey);

                using (HttpResponseMessage response = await AzureHelper.RequestWithRetry(
                    log,
                    client,
                    createRequest))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException(
                            $"Failed to retrieve existing blob {destinationBlob}, " +
                            $"status code {response.StatusCode}.");
                    }

                    byte[] existingBytes = await response.Content.ReadAsByteArrayAsync();
                    byte[] localBytes = File.ReadAllBytes(filePath);

                    bool equal = localBytes.SequenceEqual(existingBytes);

                    if (equal)
                    {
                        log.LogMessage(
                            MessageImportance.Normal,
                            "Item exists in blob storage, and is verified to be identical. " +
                            $"File: '{filePath}' Blob: '{destinationBlob}'");
                    }

                    return equal;
                }
            }
        }

        private string DetermineContentTypeBasedOnFileExtension(string filename)
        {
            if (Path.GetExtension(filename) == ".svg")
            {
                return "image/svg+xml";
            }
            else if (Path.GetExtension(filename) == ".version")
            {
                return "text/plain";
            }
            return string.Empty;
        }
        private string DetermineCacheControlBasedOnFileExtension(string filename)
        {
            if (Path.GetExtension(filename) == ".svg")
            {
                return "No-Cache";
            }
            return string.Empty;
        }
    }
}
