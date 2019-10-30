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
        /// A list of assembly names with classification info such as Profile. A
        /// Profile="%(Profile)" attribute is included in the framework list for the matching Files
        /// item if %(Profile) contains text.
        ///
        /// If *any* FileClassifications are passed:
        ///
        ///   *Every* file that ends up listed in the framework list must have a matching
        ///   FileClassification.
        ///
        ///   Additionally, every FileClassification must find exactly one File.
        ///
        /// This task fails if the conditions aren't met. This ensures the classification doesn't
        /// become out of date when the list of files changes.
        ///
        /// %(Identity): Assembly name (including ".dll").
        /// %(Profile): List of profiles that apply, semicolon-delimited.
        /// %(ReferencedByDefault): Empty (default) or "true"/"false". Indicates whether this file
        ///   should be referenced by default when the SDK uses this framework.
        /// </summary>
        public ITaskItem[] FileClassifications { get; set; }

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

            Dictionary<string, ITaskItem> fileClassLookup = FileClassifications
                ?.ToDictionary(
                    item => item.ItemSpec,
                    item => item,
                    StringComparer.OrdinalIgnoreCase);

            var usedFileClasses = new HashSet<string>();

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
                    IsSymbolFile = item.GetMetadata("IsSymbolFile") == "true",
                    IsResourceFile = item.ItemSpec
                        .EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase)
                })
                .Where(f =>
                    !f.IsSymbolFile &&
                    (f.Filename.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || f.IsNative))
                // Remove duplicate files this task is given.
                .GroupBy(f => f.Item.ItemSpec)
                .Select(g => g.First())
                // Make order stable between builds.
                .OrderBy(f => f.TargetPath, StringComparer.Ordinal)
                .ThenBy(f => f.Filename, StringComparer.Ordinal))
            {
                string type = "Managed";

                if (f.IsNative)
                {
                    type = "Native";
                }
                else if (f.IsResourceFile)
                {
                    type = "Resources";
                }

                string path = Path.Combine(f.TargetPath, f.Filename).Replace('\\', '/');

                if (path.StartsWith("runtimes/"))
                {
                    var pathParts = path.Split('/');
                    if (pathParts.Length > 1 && pathParts[1].Contains("_"))
                    {
                        // This file is a runtime file with a "rid" containing "_". This is assumed
                        // to mean it's a cross-targeting tool and shouldn't be deployed in a
                        // self-contained app. Leave it off the list.
                        continue;
                    }
                }

                var element = new XElement(
                    "File",
                    new XAttribute("Type", type),
                    new XAttribute("Path", path));

                if (f.IsResourceFile)
                {
                    element.Add(
                        new XAttribute("Culture", Path.GetFileName(Path.GetDirectoryName(path))));
                }

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

                if (fileClassLookup != null)
                {
                    if (fileClassLookup.TryGetValue(f.Filename, out ITaskItem classItem))
                    {
                        string profile = classItem.GetMetadata("Profile");

                        if (!string.IsNullOrEmpty(profile))
                        {
                            element.Add(new XAttribute("Profile", profile));
                        }

                        string referencedByDefault = classItem.GetMetadata("ReferencedByDefault");

                        if (!string.IsNullOrEmpty(referencedByDefault))
                        {
                            element.Add(new XAttribute("ReferencedByDefault", referencedByDefault));
                        }

                        usedFileClasses.Add(f.Filename);
                    }
                    else
                    {
                        Log.LogError($"File matches no classification: {f.Filename}");
                    }
                }

                frameworkManifest.Add(element);
            }

            foreach (var unused in fileClassLookup
                ?.Keys.Except(usedFileClasses).OrderBy(p => p)
                ?? Enumerable.Empty<string>())
            {
                Log.LogError($"Classification matches no files: {unused}");
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
