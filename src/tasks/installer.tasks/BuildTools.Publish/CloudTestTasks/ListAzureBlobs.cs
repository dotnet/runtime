// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.DotNet.Build.CloudTestTasks
{
    public partial class ListAzureBlobs : AzureConnectionStringBuildTask
    {

        /// <summary>
        /// The name of the container to access.  The specified name must be in the correct format, see the
        /// following page for more info.  https://msdn.microsoft.com/en-us/library/azure/dd135715.aspx
        /// </summary>
        [Required]
        public string ContainerName { get; set; }

        [Output]
        public string[] BlobNames { get; set; }

        public string FilterBlobNames { get; set; }

        public override bool Execute()
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }

        public static string[] Execute(string accountName,
                                   string accountKey,
                                   string connectionString,
                                   string containerName,
                                   string filterBlobNames,
                                   IBuildEngine buildengine,
                                   ITaskHost taskHost)
        {
            ListAzureBlobs getAzureBlobList = new ListAzureBlobs()
            {
                AccountName = accountName,
                AccountKey = accountKey,
                ContainerName = containerName,
                FilterBlobNames = filterBlobNames,
                BuildEngine = buildengine,
                HostObject = taskHost
            };
            getAzureBlobList.Execute();
            return getAzureBlobList.BlobNames;
        }

        // This code is duplicated in BuildTools task DownloadFromAzure, and that code should be refactored to permit blob listing.
        public async Task<bool> ExecuteAsync()
        {
            ParseConnectionString();
            try
            {
                List<string> blobNames = await ListBlobs(Log, AccountName, AccountKey, ContainerName, FilterBlobNames);
                BlobNames = blobNames.ToArray();
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, true);
            }
            return !Log.HasLoggedErrors;
        }

        public static async Task<List<string>> ListBlobs(TaskLoggingHelper Log, string AccountName, string AccountKey, string ContainerName, string FilterBlobNames)
        {
            List<string> blobsNames = new List<string>();
            string urlListBlobs = string.Format("https://{0}.blob.core.windows.net/{1}?restype=container&comp=list", AccountName, ContainerName);
            if (!string.IsNullOrWhiteSpace(FilterBlobNames))
            {
                urlListBlobs += $"&prefix={FilterBlobNames}";
            }
            Log.LogMessage(MessageImportance.Low, "Sending request to list blobsNames for container '{0}'.", ContainerName);

            using (HttpClient client = new HttpClient())
            {

                var createRequest = AzureHelper.RequestMessage("GET", urlListBlobs, AccountName, AccountKey);

                XmlDocument responseFile;
                string nextMarker = string.Empty;
                using (HttpResponseMessage response = await AzureHelper.RequestWithRetry(Log, client, createRequest))
                {
                    responseFile = new XmlDocument();
                    responseFile.LoadXml(await response.Content.ReadAsStringAsync());
                    XmlNodeList elemList = responseFile.GetElementsByTagName("Name");

                    blobsNames.AddRange(elemList.Cast<XmlNode>()
                                                .Select(x => x.InnerText)
                                                .ToList());

                    nextMarker = responseFile.GetElementsByTagName("NextMarker").Cast<XmlNode>().FirstOrDefault()?.InnerText;
                }
                while (!string.IsNullOrEmpty(nextMarker))
                {
                    urlListBlobs = $"https://{AccountName}.blob.core.windows.net/{ContainerName}?restype=container&comp=list&marker={nextMarker}";
                    if (!string.IsNullOrWhiteSpace(FilterBlobNames))
                    {
                        urlListBlobs += $"&prefix={FilterBlobNames}";
                    }
                    var nextRequest = AzureHelper.RequestMessage("GET", urlListBlobs, AccountName, AccountKey);
                    using (HttpResponseMessage nextResponse = AzureHelper.RequestWithRetry(Log, client, nextRequest).GetAwaiter().GetResult())
                    {
                        responseFile = new XmlDocument();
                        responseFile.LoadXml(await nextResponse.Content.ReadAsStringAsync());
                        XmlNodeList elemList = responseFile.GetElementsByTagName("Name");

                        blobsNames.AddRange(elemList.Cast<XmlNode>()
                                                    .Select(x => x.InnerText)
                                                    .ToList());

                        nextMarker = responseFile.GetElementsByTagName("NextMarker").Cast<XmlNode>().FirstOrDefault()?.InnerText;
                    }
                }
            }
            return blobsNames;
        }
    }

}
