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

        [NotNull]
        [Required]
        public string? OutputFile { get; set; }

        public override bool Execute()
        {
            var outDir = Path.GetDirectoryName(OutputFile);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            StringBuilder sb = new();

            sb.AppendLine("<Project>");
            sb.AppendLine("    <PropertyGroup>");

            foreach (var prop in Properties)
            {
                string value = prop.ItemSpec;
                string? name = prop.GetMetadata("Name");
                string? condition = prop.GetMetadata("ConditionToUse");

                if (!string.IsNullOrEmpty(condition))
                    sb.AppendLine($"        <{name} Condition=\"{condition}\">{value}</{name}>");
                else
                    sb.AppendLine($"        <{name}>{value}</{name}>");
            }

            sb.AppendLine("    </PropertyGroup>");
            sb.AppendLine("</Project>");

            File.WriteAllText(OutputFile, sb.ToString());
            return !Log.HasLoggedErrors;
        }

    }
}
