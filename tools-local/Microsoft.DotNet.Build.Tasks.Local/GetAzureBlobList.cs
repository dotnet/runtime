// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml;
using System.Threading.Tasks;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class GetAzureBlobList : Utility.AzureConnectionStringBuildTask
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

        public static string [] Execute(string accountName,
                                   string accountKey,
                                   string connectionString,
                                   string containerName,
                                   string filterBlobNames,
                                   IBuildEngine buildengine,
                                   ITaskHost taskHost)
        {
            GetAzureBlobList getAzureBlobList = new GetAzureBlobList()
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

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            List<string> blobsNames = new List<string>();
            string urlListBlobs = string.Format("https://{0}.blob.core.windows.net/{1}?restype=container&comp=list", AccountName, ContainerName);

            Log.LogMessage(MessageImportance.Low, "Sending request to list blobsNames for container '{0}'.", ContainerName);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var createRequest = Utility.AzureHelper.RequestMessage("GET", urlListBlobs, AccountName, AccountKey);

                    XmlDocument responseFile;
                    string nextMarker = string.Empty;
                    using (HttpResponseMessage response = await Utility.AzureHelper.RequestWithRetry(Log, client, createRequest))
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
                        urlListBlobs = string.Format($"https://{AccountName}.blob.core.windows.net/{ContainerName}?restype=container&comp=list&marker={nextMarker}");
                        using (HttpResponseMessage response = Utility.AzureHelper.RequestWithRetry(Log, client, createRequest).GetAwaiter().GetResult())
                        {
                            responseFile = new XmlDocument();
                            responseFile.LoadXml(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                            XmlNodeList elemList = responseFile.GetElementsByTagName("Name");

                            blobsNames.AddRange(elemList.Cast<XmlNode>()
                                                        .Select(x => x.InnerText)
                                                        .ToList());

                            nextMarker = responseFile.GetElementsByTagName("NextMarker").Cast<XmlNode>().FirstOrDefault()?.InnerText;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(FilterBlobNames))
                    {
                        BlobNames = blobsNames.Where(b => b.StartsWith(FilterBlobNames)).ToArray();
                    }
                    else
                    {
                        BlobNames = blobsNames.ToArray();
                    }
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                }
                return !Log.HasLoggedErrors;
            }
        }
    }

}
