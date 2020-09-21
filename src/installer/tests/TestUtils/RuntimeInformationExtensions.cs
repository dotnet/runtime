// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class RuntimeInformationExtensions
    {
        public static string GetExeFileNameForCurrentPlatform(string exeName) =>
            exeName + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty);

        public static string GetSharedLibraryFileNameForCurrentPlatform(string libraryName)
        {
            string prefix;
            string suffix;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                prefix = string.Empty;
                suffix = ".dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                prefix = "lib";
                suffix = ".dylib";
            }
            else
            {
                prefix = "lib";
                suffix = ".so";
            }

            return prefix + libraryName + suffix;
        }
    }
}
