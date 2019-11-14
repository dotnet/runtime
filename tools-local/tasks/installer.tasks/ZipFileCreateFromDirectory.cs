// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Build.Tasks
{
    public sealed class ZipFileCreateFromDirectory : Task
    {
        /// <summary>
        /// The path to the directory to be archived.
        /// </summary>
        [Required]
        public string SourceDirectory { get; set; }

        /// <summary>
        /// The path of the archive to be created.
        /// </summary>
        [Required]
        public string DestinationArchive { get; set; }

        /// <summary>
        /// Indicates if the destination archive should be overwritten if it already exists.
        /// </summary>
        public bool OverwriteDestination { get; set; }

        /// <summary>
        /// If zipping an entire folder without exclusion patterns, whether to include the folder in the archive.
        /// </summary>
        public bool IncludeBaseDirectory { get; set; }

        /// <summary>
        /// An item group of regular expressions for content to exclude from the archive.
        /// </summary>
        public ITaskItem[] ExcludePatterns { get; set; }

        public override bool Execute()
        {
            try
            {
                if (File.Exists(DestinationArchive))
                {
                    if (OverwriteDestination == true)
                    {
                        Log.LogMessage(MessageImportance.Low, "{0} already existed, deleting before zipping...", DestinationArchive);
                        File.Delete(DestinationArchive);
                    }
                    else
                    {
                        Log.LogWarning("'{0}' already exists. Did you forget to set '{1}' to true?", DestinationArchive, nameof(OverwriteDestination));
                    }
                }

                Log.LogMessage(MessageImportance.High, "Compressing {0} into {1}...", SourceDirectory, DestinationArchive);
                string destinationDirectory = Path.GetDirectoryName(DestinationArchive);
                if (!Directory.Exists(destinationDirectory) && !string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                if (ExcludePatterns == null)
                {
                    ZipFile.CreateFromDirectory(SourceDirectory, DestinationArchive, CompressionLevel.Optimal, IncludeBaseDirectory);
                }
                else
                {
                    // convert to regular expressions
                    Regex[] regexes = new Regex[ExcludePatterns.Length];
                    for (int i = 0; i < ExcludePatterns.Length; ++i)
                        regexes[i] = new Regex(ExcludePatterns[i].ItemSpec, RegexOptions.IgnoreCase);

                    using (FileStream writer = new FileStream(DestinationArchive, FileMode.CreateNew))
                    {
                        using (ZipArchive zipFile = new ZipArchive(writer, ZipArchiveMode.Create))
                        {
                            var files = Directory.GetFiles(SourceDirectory, "*", SearchOption.AllDirectories);

                            foreach (var file in files)
                            {
                                // look for a match
                                bool foundMatch = false;
                                foreach (var regex in regexes)
                                {
                                    if (regex.IsMatch(file))
                                    {
                                        foundMatch = true;
                                        break;
                                    }
                                }

                                if (foundMatch)
                                {
                                    Log.LogMessage(MessageImportance.Low, "Excluding {0} from archive.", file);
                                    continue;
                                }

                                var relativePath = MakeRelativePath(SourceDirectory, file);
                                zipFile.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // We have 2 log calls because we want a nice error message but we also want to capture the callstack in the log.
                Log.LogError("An exception has occurred while trying to compress '{0}' into '{1}'.", SourceDirectory, DestinationArchive);
                Log.LogErrorFromException(e, /*show stack=*/ true, /*show detail=*/ true, DestinationArchive);
                return false;
            }

            return true;
        }

        private string MakeRelativePath(string root, string subdirectory)
        {
            if (!subdirectory.StartsWith(root))
                throw new Exception(string.Format("'{0}' is not a subdirectory of '{1}'.", subdirectory, root));

            // returned string should not start with a directory separator
            int chop = root.Length;
            if (subdirectory[chop] == Path.DirectorySeparatorChar)
                ++chop;

            return subdirectory.Substring(chop);
        }
    }
}
