// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public static class Constants
    {
        //public static readonly string ProjectFileName = "project.json";
        public static readonly string ExeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

        // Priority order of runnable suffixes to look for and run
        public static readonly string[] RunnableSuffixes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                                         ? new string[] { ".exe", ".cmd", ".bat" }
                                                         : new string[] { string.Empty };

        public static readonly string DynamicLibPrefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "" : "lib";

        public static readonly string DynamicLibSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll" :
                                                         RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".dylib" : ".so";
    }
}
