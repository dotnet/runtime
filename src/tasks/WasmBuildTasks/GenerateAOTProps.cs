// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks
{
    public class GenerateAOTProps : Task
    {
        [NotNull]
        [Required]
        public ITaskItem[]? Properties { get; set; }

        public ITaskItem[] Items { get; set; } = Array.Empty<ITaskItem>();

        [NotNull]
        [Required]
        public string? OutputFile { get; set; }

        private const string s_originalItemNameMetadata = "OriginalItemName__";
        private const string s_conditionToUseMetadata = "ConditionToUse__";
        private static readonly HashSet<string> s_metadataNamesToSkip = new()
        {
            "FullPath",
            "RootDir",
            "Filename",
            "Extension",
            "RelativeDir",
            "Directory",
            "RecursiveDir",
            "Identity",
            "ModifiedTime",
            "CreatedTime",
            "AccessedTime",
            "DefiningProjectFullPath",
            "DefiningProjectDirectory",
            "DefiningProjectName",
            "DefiningProjectExtension",
            s_originalItemNameMetadata,
            s_conditionToUseMetadata
        };

        public override bool Execute()
        {
            var outDir = Path.GetDirectoryName(OutputFile);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            StringBuilder sb = new();

            sb.AppendLine("<Project>");
            sb.AppendLine("\t<PropertyGroup>");

            foreach (ITaskItem2 prop in Properties)
            {
                string value = prop.EvaluatedIncludeEscaped;
                string? name = prop.GetMetadata("Name");
                string? condition = prop.GetMetadataValueEscaped(s_conditionToUseMetadata);

                if (!string.IsNullOrEmpty(condition))
                    sb.AppendLine($"\t\t<{name} Condition=\"{condition}\">{value}</{name}>");
                else
                    sb.AppendLine($"\t\t<{name}>{value}</{name}>");
            }

            sb.AppendLine("\t</PropertyGroup>");

            sb.AppendLine("\t<ItemGroup>");
            foreach (ITaskItem2 item in Items)
            {
                string value = item.EvaluatedIncludeEscaped;
                string name = item.GetMetadata(s_originalItemNameMetadata);

                if (string.IsNullOrEmpty(name))
                {
                    Log.LogError($"Item {value} is missing {s_originalItemNameMetadata} metadata, for the item name");
                    return false;
                }

                sb.AppendLine($"\t\t<{name} Include=\"{value}\"");

                string? condition = item.GetMetadataValueEscaped(s_conditionToUseMetadata);
                if (!string.IsNullOrEmpty(condition))
                    sb.AppendLine($"\t\t\tCondition=\"{condition}\"");

                foreach (string mdName in item.MetadataNames)
                {
                    if (!s_metadataNamesToSkip.Contains(mdName))
                        sb.AppendLine($"\t\t\t{mdName}=\"{item.GetMetadataValueEscaped(mdName)}\"");
                }

                sb.AppendLine($"\t\t/>");
            }

            sb.AppendLine("\t</ItemGroup>");
            sb.AppendLine("</Project>");

            File.WriteAllText(OutputFile, sb.ToString());
            return !Log.HasLoggedErrors;
        }

    }
}
