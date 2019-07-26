// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    /// <summary>
    /// In a WiX source file, replaces the Id of a File with some given string in order to stabilize
    /// it. This allows external tooling such as signature validators to rely on a stable identifier
    /// for certain files.
    /// </summary>
    public class StabilizeWixFileId : BuildTask
    {
        /// <summary>
        /// File to read from. This is expected to be an output from heat.exe.
        /// 
        /// Expected format:
        /// 
        ///   <?xml version="1.0" encoding="utf-8"?>
        ///   <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
        ///       <Fragment>
        ///           <ComponentGroup Id="InstallFiles">
        ///               <Component Id="cmp680C9..." Directory="dir14B9F..." Guid="{C31...}">
        ///                   <File Id="filE57B7..." KeyPath="yes" Source="$(var.PackSrc)\packs\...\native\apphost.exe" />
        ///                   ...
        /// </summary>
        [Required]
        public string SourceFile { get; set; }

        /// <summary>
        /// File to write to. May be the same as SourceFile.
        /// </summary>
        [Required]
        public string OutputFile { get; set; }

        /// <summary>
        /// Set of files to stabilize. This matches the end of the "Source" attribute in the WiX
        /// source file. If exactly one match isn't found in the WiX source file, this task fails.
        /// 
        /// %(Identity): The file source to replace.
        /// %(ReplacementId): The replacement for Id that won't change per-build.
        /// </summary>
        [Required]
        public ITaskItem[] FileElementToStabilize { get; set; }

        public override bool Execute()
        {
            XDocument content = XDocument.Load(SourceFile);

            XNamespace rootNamespace = content.Root.GetDefaultNamespace();
            XName GetQualifiedName(string name) => rootNamespace.GetName(name);

            foreach (var file in FileElementToStabilize)
            {
                string replacement = file.GetMetadata("ReplacementId");

                if (string.IsNullOrEmpty(replacement))
                {
                    Log.LogError($"{nameof(FileElementToStabilize)} {file.ItemSpec} has null/empty ReplacementId metadata.");
                    continue;
                }

                XElement[] matchingFileElements = content.Element(GetQualifiedName("Wix"))
                    .Elements(GetQualifiedName("Fragment"))
                    .SelectMany(f => f.Elements(GetQualifiedName("ComponentGroup")))
                    .SelectMany(cg => cg.Elements(GetQualifiedName("Component")))
                    .SelectMany(c => c.Elements(GetQualifiedName("File")))
                    .Where(f => f.Attribute("Source")?.Value
                        ?.EndsWith(file.ItemSpec, StringComparison.OrdinalIgnoreCase) == true)
                    .ToArray();

                if (matchingFileElements.Length != 1)
                {
                    Log.LogError(
                        $"Expected 1 match for '{file.ItemSpec}', found {matchingFileElements.Length}: " +
                        string.Join(", ", matchingFileElements.Select(e => e.ToString())));

                    continue;
                }

                XAttribute nameAttribute = matchingFileElements[0].Attribute("Id");

                if (nameAttribute is null)
                {
                    Log.LogError($"Match has no Id attribute: {matchingFileElements[0]}");
                    continue;
                }

                Log.LogMessage(
                    $"Setting '{file.ItemSpec}' Id to '{replacement}' for File with Source " +
                    matchingFileElements[0].Attribute("Source").Value);

                nameAttribute.Value = replacement;
            }

            content.Save(OutputFile);

            return !Log.HasLoggedErrors;
        }
    }
}
