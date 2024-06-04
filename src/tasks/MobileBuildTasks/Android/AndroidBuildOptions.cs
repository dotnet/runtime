// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Android.Build
{
    public sealed class AndroidBuildOptions
    {
        public List<string> CompilerArguments { get; } = new List<string>();

        public List<string> IncludePaths { get; } = new List<string>();

        public string IntermediateOutputPath { get; set; } = string.Empty;

        public List<string> LinkerArguments { get; } = new List<string>();

        public List<string> NativeLibraryPaths { get; } = new List<string>();

        public string OutputPath { get; set; } = string.Empty;

        public List<string> Sources { get; } = new List<string>();
    }
}
