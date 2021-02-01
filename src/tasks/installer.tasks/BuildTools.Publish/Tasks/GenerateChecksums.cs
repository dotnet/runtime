// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GenerateChecksums : BuildTask
    {
        /// <summary>
        /// An item collection of files for which to generate checksums.  Each item must have metadata
        /// 'DestinationPath' that specifies the path of the checksum file to create.
        /// </summary>
        [Required]
        public ITaskItem[] Items { get; set; }

        public override bool Execute()
        {
            foreach (ITaskItem item in Items)
            {
                try
                {
                    string destinationPath = item.GetMetadata("DestinationPath");
                    if (string.IsNullOrEmpty(destinationPath))
                    {
                        throw new Exception($"Metadata 'DestinationPath' is missing for item '{item.ItemSpec}'.");
                    }

                    if (!File.Exists(item.ItemSpec))
                    {
                        throw new Exception($"The file '{item.ItemSpec}' does not exist.");
                    }

                    Log.LogMessage(
                        MessageImportance.High,
                        "Generating checksum for '{0}' into '{1}'...",
                        item.ItemSpec,
                        destinationPath);

                    using (FileStream stream = File.OpenRead(item.ItemSpec))
                    {
                        using(HashAlgorithm hashAlgorithm = SHA512.Create())
                        {
                            byte[] hash = hashAlgorithm.ComputeHash(stream);
                            string checksum = BitConverter.ToString(hash).Replace("-", string.Empty);
                            File.WriteAllText(destinationPath, checksum);
                        }
                    }
                }
                catch (Exception e)
                {
                    // We have 2 log calls because we want a nice error message but we also want to capture the
                    // callstack in the log.
                    Log.LogError("An exception occurred while trying to generate a checksum for '{0}'.", item.ItemSpec);
                    Log.LogMessage(MessageImportance.Low, e.ToString());
                    return false;
                }
            }

            return true;
        }
    }
}
