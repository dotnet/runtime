// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.DotNet.Build.CloudTestTasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Build.Tasks
{
    public class FinalizeBuild : AzureConnectionStringBuildTask
    {
        [Required]
        public string SemaphoreBlob { get; set; }
        [Required]
        public string FinalizeContainer { get; set; }
        public string MaxWait { get; set; }
        public string Delay { get; set; }
        [Required]
        public string ContainerName { get; set; }
        [Required]
        public string Channel { get; set; }
        [Required]
        public string SharedFrameworkNugetVersion { get; set; }
        [Required]
        public string SharedHostNugetVersion { get; set; }
        [Required]
        public string ProductVersion { get; set; }
        [Required]
        public string Version { get; set; }
        [Required]
        public string CommitHash { get; set; }
        public bool ForcePublish { get; set; }

        private Regex _versionRegex = new Regex(@"(?<version>\d+\.\d+\.\d+)(-(?<prerelease>[^-]+-)?(?<major>\d+)-(?<minor>\d+))?");

        public override bool Execute()
        {
            ParseConnectionString();

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            if (!FinalizeContainer.EndsWith("/"))
            {
                FinalizeContainer = $"{FinalizeContainer}/";
            }
            string targetVersionFile = $"{FinalizeContainer}{Version}";

            CreateBlobIfNotExists(SemaphoreBlob);

            AzureBlobLease blobLease = new AzureBlobLease(AccountName, AccountKey, ConnectionString, ContainerName, SemaphoreBlob, Log);
            Log.LogMessage($"Acquiring lease on semaphore blob '{SemaphoreBlob}'");
            blobLease.Acquire();

            // Prevent race conditions by dropping a version hint of what version this is. If we see this file
            // and it is the same as our version then we know that a race happened where two+ builds finished
            // at the same time and someone already took care of publishing and we have no work to do.
            if (IsLatestSpecifiedVersion(targetVersionFile) && !ForcePublish)
            {
                Log.LogMessage(MessageImportance.Low, $"version hint file for publishing finalization is {targetVersionFile}");
                Log.LogMessage(MessageImportance.High, $"Version '{Version}' is already published, skipping finalization.");
                Log.LogMessage($"Releasing lease on semaphore blob '{SemaphoreBlob}'");
                blobLease.Release();
                return true;
            }
            else
            {

                // Delete old version files
                GetBlobList(FinalizeContainer)
                    .Select(s => s.Replace("/dotnet/", ""))
                    .Where(w => _versionRegex.Replace(Path.GetFileName(w), "") == "")
                    .ToList()
                    .ForEach(f => TryDeleteBlob(f));


                // Drop the version file signaling such for any race-condition builds (see above comment).
                CreateBlobIfNotExists(targetVersionFile);

                try
                {
                    CopyBlobs($"Runtime/{ProductVersion}/", $"Runtime/{Channel}/");

                    // Generate the latest version text file
                    string sfxVersion = GetSharedFrameworkVersionFileContent();
                    PublishStringToBlob(ContainerName, $"Runtime/{Channel}/latest.version", sfxVersion, "text/plain");
                }
                finally
                {
                    blobLease.Release();
                }
            }
            return !Log.HasLoggedErrors;
        }

        private string GetSharedFrameworkVersionFileContent()
        {
            string returnString = $"{CommitHash}{Environment.NewLine}";
            returnString += $"{SharedFrameworkNugetVersion}{Environment.NewLine}";
            return returnString;
        }

        public bool CopyBlobs(string sourceFolder, string destinationFolder)
        {
            bool returnStatus = true;
            List<Task<bool>> copyTasks = new List<Task<bool>>();
            string[] blobs = GetBlobList(sourceFolder);
            foreach (string blob in blobs)
            {
                string targetName = Path.GetFileName(blob)
                                        .Replace(SharedFrameworkNugetVersion, "latest")
                                        .Replace(SharedHostNugetVersion, "latest");
                string sourceBlob = blob.Replace($"/{ContainerName}/", "");
                string destinationBlob = $"{destinationFolder}{targetName}";
                Log.LogMessage($"Copying blob '{sourceBlob}' to '{destinationBlob}'");
                copyTasks.Add(CopyBlobAsync(sourceBlob, destinationBlob));
            }
            Task.WaitAll(copyTasks.ToArray());
            copyTasks.ForEach(c => returnStatus &= c.Result);
            return returnStatus;
        }

        public bool TryDeleteBlob(string path)
        {
            return DeleteBlob(ContainerName, path);
        }

        public void CreateBlobIfNotExists(string path)
        {
            var blobList = GetBlobList(path);
            if(blobList.Count() == 0)
            {
                PublishStringToBlob(ContainerName, path, DateTime.Now.ToString());
            }
        }

        public bool IsLatestSpecifiedVersion(string versionFile)
        {
            var blobList = GetBlobList(versionFile);
            return blobList.Count() != 0;
        }

        public bool DeleteBlob(string container, string blob)
        {
            return DeleteAzureBlob.Execute(AccountName,
                                           AccountKey,
                                            ConnectionString,
                                            container,
                                            blob,
                                            BuildEngine,
                                            HostObject);
        }

        public Task<bool> CopyBlobAsync(string sourceBlobName, string destinationBlobName)
        {
            return CopyAzureBlobToBlob.ExecuteAsync(AccountName,
                                               AccountKey,
                                               ConnectionString,
                                               ContainerName,
                                               sourceBlobName,
                                               destinationBlobName,
                                               BuildEngine,
                                               HostObject);
        }

        public string[] GetBlobList(string path)
        {
            return ListAzureBlobs.Execute(AccountName,
                                            AccountKey,
                                            ConnectionString,
                                            ContainerName,
                                            path,
                                            BuildEngine,
                                            HostObject);
        }

        public bool PublishStringToBlob(string container, string blob, string contents, string contentType = null)
        {
            return PublishStringToAzureBlob.Execute(AccountName,
                                                    AccountKey,
                                                    ConnectionString,
                                                    container,
                                                    blob,
                                                    contents,
                                                    contentType,
                                                    BuildEngine,
                                                    HostObject);
        }
    }
}
