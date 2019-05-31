// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public class CreateFrameworkListFile : BuildTask
    {
        /// <summary>
        /// Files to extract basic information from and include in the list.
        /// </summary>
        [Required]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// A list of assembly names to add Profile="%(Profile)" attributes to, if the assembly
        /// names exist in Files.
        /// 
        /// %(Identity): Assembly name (including ".dll").
        /// %(Profile): List of profiles that apply, semicolon-delimited.
        /// </summary>
        public ITaskItem[] FileProfiles { get; set; }

        [Required]
        public string TargetFile { get; set; }

        public string[] TargetFilePrefixes { get; set; }

        /// <summary>
        /// Extra attributes to place on the root node.
        /// 
        /// %(Identity): Attribute name.
        /// %(Value): Attribute value.
        /// </summary>
        public ITaskItem[] RootAttributes { get; set; }

        public override bool Execute()
        {
            XAttribute[] rootAttributes = RootAttributes
                ?.Select(item => new XAttribute(item.ItemSpec, item.GetMetadata("Value")))
                .ToArray();

            var frameworkManifest = new XElement("FileList", rootAttributes);

            Dictionary<string, string> fileProfileLookup = (FileProfiles ?? Array.Empty<ITaskItem>())
                .ToDictionary(
                    item => item.ItemSpec,
                    item => item.GetMetadata("Profile"),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var f in Files
                .Where(IsTargetPathIncluded)
                .Select(item => new
                {
                    Item = item,
                    Filename = Path.GetFileName(item.ItemSpec),
                    TargetPath = item.GetMetadata("TargetPath"),
                    AssemblyName = FileUtilities.GetAssemblyName(item.ItemSpec),
                    FileVersion = FileUtilities.GetFileVersion(item.ItemSpec),
                    IsNative = item.GetMetadata("IsNative") == "true",
                    IsSymbolFile = item.GetMetadata("IsSymbolFile") == "true"
                })
                .Where(f =>
                    !f.IsSymbolFile &&
                    (f.Filename.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || f.IsNative))
                .OrderBy(f => f.TargetPath, StringComparer.Ordinal)
                .ThenBy(f => f.Filename, StringComparer.Ordinal))
            {
                var element = new XElement(
                    "File",
                    new XAttribute("Type", f.IsNative ? "Native" : "Managed"),
                    new XAttribute(
                        "Path",
                        Path.Combine(f.TargetPath, f.Filename).Replace('\\', '/')));

                if (f.AssemblyName != null)
                {
                    byte[] publicKeyToken = f.AssemblyName.GetPublicKeyToken();
                    string publicKeyTokenHex;

                    if (publicKeyToken != null)
                    {
                        publicKeyTokenHex = BitConverter.ToString(publicKeyToken)
                            .ToLowerInvariant()
                            .Replace("-", "");
                    }
                    else
                    {
                        Log.LogError($"No public key token found for assembly {f.Item.ItemSpec}");
                        publicKeyTokenHex = "";
                    }

                    element.Add(
                        new XAttribute("AssemblyName", f.AssemblyName.Name),
                        new XAttribute("PublicKeyToken", publicKeyTokenHex),
                        new XAttribute("AssemblyVersion", f.AssemblyName.Version));
                }
                else if (!f.IsNative)
                {
                    // This file isn't managed and isn't native. Leave it off the list.
                    continue;
                }

                element.Add(new XAttribute("FileVersion", f.FileVersion));

                if (fileProfileLookup.TryGetValue(f.Filename, out string profile))
                {
                    element.Add(new XAttribute("Profile", profile));
                }

                frameworkManifest.Add(element);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(TargetFile));
            File.WriteAllText(TargetFile, frameworkManifest.ToString());

            return !Log.HasLoggedErrors;
        }

        private bool IsTargetPathIncluded(ITaskItem item)
        {
            return TargetFilePrefixes
                ?.Any(prefix => item.GetMetadata("TargetPath")?.StartsWith(prefix) == true) ?? true;
        }
    }
}
