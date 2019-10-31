// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO.Compression;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public sealed class ZipFileGetEntries : BuildTask
    {
        /// <summary>
        /// The path to the archive.
        /// </summary>
        [Required]
        public string TargetArchive { get; set; }

        /// <summary>
        /// Generated items where each ItemSpec is the relative location of a
        /// file entry in the zip archive.
        /// </summary>
        [Output]
        public ITaskItem[] Entries { get; set; }

        public override bool Execute()
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(TargetArchive))
                {
                    Entries = archive.Entries
                        // Escape '%' so encoded '+' in the nupkg stays encoded through MSBuild.
                        .Select(e => new TaskItem(e.FullName.Replace("%", "%25")))
                        .ToArray();
                }
            }
            catch (Exception e)
            {
                // We have 2 log calls because we want a nice error message but we also want to capture the callstack in the log.
                Log.LogError($"An exception has occurred while trying to read entries from '{TargetArchive}'.");
                Log.LogErrorFromException(e, /*show stack=*/ true, /*show detail=*/ true, TargetArchive);
                return false;
            }
            return true;
        }
    }
}
