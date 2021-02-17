// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.WebAssembly.Build.Tasks
{
    public class ReadEmccVersion : Task
    {
        [NotNull]
        [Required]
        public string? VersionFile { get; set; }

        [Output]
        public string? Version { get; set; }

        public override bool Execute()
        {
            if (!File.Exists(VersionFile))
            {
                Log.LogError($"Could not find version file {VersionFile}");
                return false;
            }

            Version = File.ReadLines(VersionFile).First();
            return true;
        }
    }
}
