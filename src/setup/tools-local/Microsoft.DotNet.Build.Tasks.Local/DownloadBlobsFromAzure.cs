// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class DownloadBlobsFromAzure : Utility.AzureConnectionStringBuildTask
    {
        /// <summary>
        /// The name of the container to access.  The specified name must be in the correct format, see the
        /// following page for more info.  https://msdn.microsoft.com/en-us/library/azure/dd135715.aspx
        /// </summary>
        [Required]
        public string ContainerName { get; set; }

        [Required]
        public ITaskItem[] BlobNames { get; set; }

        /// <summary>
        /// Directory to download blob files to.
        /// </summary>
        [Required]
        public string DownloadDirectory { get; set; }

        public override bool Execute()
        {
            var tasks = ExecuteAsync();
            if(tasks == null)
            {
                return false;
            }
            Task.WaitAll(tasks.ToArray());
            return !tasks.Any(t => t.Result == false);
        }

        public List<Task<bool>> ExecuteAsync()
        {
            ParseConnectionString();
            if (Log.HasLoggedErrors)
            {
                return null;
            }
            List<Task<bool>> downloadTasks = new List<Task<bool>>();
            foreach (var blobFile in BlobNames)
            {
                string localBlobFile = Path.Combine(DownloadDirectory, Path.GetFileName(blobFile.ItemSpec));
                Log.LogMessage($"Downloading {blobFile} to {localBlobFile}");

                downloadTasks.Add(DownloadBlobFromAzure.ExecuteAsync(AccountName, AccountKey, ConnectionString, ContainerName, blobFile.ItemSpec, DownloadDirectory, BuildEngine, HostObject));
            }
            return downloadTasks;
        }
    }
}
