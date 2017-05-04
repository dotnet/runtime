// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.DotNet.Build.Tasks.Utility
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
            string destinationBlob)
        {
           
            string resourceUrl = string.Format("https://{0}.blob.core.windows.net/{1}", AccountName, ContainerName);

            string fileName = destinationBlob;
            fileName = fileName.Replace("\\", "/");
            string blobUploadUrl = resourceUrl + "/" + fileName;
            int size = (int)new FileInfo(filePath).Length;
            int blockSize = 4 * 1024 * 1024; //4MB max size of a block blob
            int bytesLeft = size;
            List<string> blockIds = new List<string>();
            int numberOfBlocks = (size / blockSize) + 1;
            int countForId = 0;
            using (FileStream fileStreamTofilePath = new FileStream(filePath, FileMode.Open))
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
                        Func<HttpRequestMessage> createRequest = () =>
                        {
                            DateTime dt = DateTime.UtcNow;
                            var req = new HttpRequestMessage(HttpMethod.Put, blockUploadUrl);
                            req.Headers.Add(
                                AzureHelper.DateHeaderString,
                                dt.ToString("R", CultureInfo.InvariantCulture));
                            req.Headers.Add(AzureHelper.VersionHeaderString, AzureHelper.StorageApiVersion);
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

                    var body = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><BlockList>");
                    foreach (object item in blockIds)
                        body.AppendFormat("<Latest>{0}</Latest>", item);

                    body.Append("</BlockList>");
                    byte[] bodyData = Encoding.UTF8.GetBytes(body.ToString());
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
                            ""));
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
    }
}
