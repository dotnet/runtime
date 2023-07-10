// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Microsoft.Android.Build.Ndk;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Android.Build.Tasks
{
    public class NdkToolFinderTask : Task
    {
        /// <summary>
        /// The dotnet specific target android architecture
        /// </summary>
        [Required]
        [NotNull]
        public string? Architecture { get; set; }

        /// <summary>
        /// The dotnet specific host OS being used (windows, linux, osx)
        /// </summary>
        [Required]
        [NotNull]
        public string? HostOS { get; set; }

        /// <summary>
        /// The minimum API level supported. This is important when using clang
        /// </summary>
        [Required]
        [NotNull]
        public string? MinApiLevel { get; set; }

        /// <summary>
        /// The path to the folder that contains the android native assembler (as).
        /// May not be supported in newer NDK versions.
        /// </summary>
        [Output]
        public string? AsPrefixPath { get; set; } = ""!;

        /// <summary>
        /// The path to the api level specific clang being used
        /// </summary>
        [Output]
        public string? ClangPath { get; set; } = ""!;

        /// <summary>
        /// The name of the linker being used.
        /// </summary>
        [Output]
        public string? LdName { get; set; } = ""!;

        /// <summary>
        /// The path to the linker being used
        /// </summary>
        [Output]
        public string? LdPath { get; set; } = ""!;

        /// <summary>
        /// The path to the NDK toolchain bin folder
        /// </summary>
        [Output]
        public string? ToolPrefixPath { get; set; } = ""!;

        /// <summary>
        /// The LLVM triple for the android target.
        [Output]
        public string? Triple { get; set; } = ""!;

        public override bool Execute()
        {
            NdkTools tools = new NdkTools(Architecture, HostOS, MinApiLevel);
            AsPrefixPath = tools.AsPrefixPath;
            ToolPrefixPath = tools.ToolPrefixPath;
            Triple = tools.Triple;
            LdName = tools.LdName;
            LdPath = tools.LdPath;
            ClangPath = tools.ClangPath;

            return true;
        }
    }
}
