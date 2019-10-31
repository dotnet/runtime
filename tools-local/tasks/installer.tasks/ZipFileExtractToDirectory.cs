// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System;
using System.IO;
using System.IO.Compression;

namespace Microsoft.DotNet.Build.Tasks
{
    public sealed class ZipFileExtractToDirectory : BuildTask
    {
        /// <summary>
        /// The path to the archive to be extracted.
        /// </summary>
        [Required]
        public string SourceArchive { get; set; }

        /// <summary>
        /// The path of the directory to extract into.
        /// </summary>
        [Required]
        public string DestinationDirectory { get; set; }

        /// <summary>
        /// Indicates if the destination directory should be overwritten if it already exists.
        /// </summary>
        public bool OverwriteDestination { get; set; }

        /// <summary>
        /// File entries to include in the extraction. Entries are relative
        /// paths inside the archive. If null or empty, all files are extracted.
        /// </summary>
        public ITaskItem[] Include { get; set; }

        public override bool Execute()
        {
            try
            {
                if (Directory.Exists(DestinationDirectory))
                {
                    if (OverwriteDestination)
                    {
                        Log.LogMessage(MessageImportance.Low, $"'{DestinationDirectory}' already exists, trying to delete before unzipping...");
                        Directory.Delete(DestinationDirectory, recursive: true);
                    }
                    else
                    {
                        Log.LogWarning($"'{DestinationDirectory}' already exists. Did you forget to set '{nameof(OverwriteDestination)}' to true?");
                    }
                }

                Log.LogMessage(MessageImportance.High, "Decompressing '{0}' into '{1}'...", SourceArchive, DestinationDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(DestinationDirectory));

                using (ZipArchive archive = ZipFile.OpenRead(SourceArchive))
                {
                    if (Include?.Length > 0)
                    {
                        foreach (ITaskItem entryItem in Include)
                        {
                            ZipArchiveEntry entry = archive.GetEntry(entryItem.ItemSpec);
                            string destinationPath = Path.Combine(DestinationDirectory, entryItem.ItemSpec);

                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            entry.ExtractToFile(destinationPath, overwrite: false);
                        }
                    }
                    else
                    {
                        archive.ExtractToDirectory(DestinationDirectory);
                    }
                }
            }
            catch (Exception e)
            {
                // We have 2 log calls because we want a nice error message but we also want to capture the callstack in the log.
                Log.LogError("An exception has occurred while trying to decompress '{0}' into '{1}'.", SourceArchive, DestinationDirectory);
                Log.LogErrorFromException(e, /*show stack=*/ true, /*show detail=*/ true, DestinationDirectory);
                return false;
            }
            return true;
        }
    }
}
