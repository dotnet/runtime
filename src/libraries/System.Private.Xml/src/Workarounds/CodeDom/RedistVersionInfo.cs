// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.CodeDom.Compiler;
    using System.Configuration;
    using System.Collections.Generic;

    using Microsoft.Win32;

    // The default compiler is the one corresponding to the current-running version of the runtime.
    // Customers can choose to use a different one by setting a provider option.
    internal static class RedistVersionInfo
    {
        // Version identifier added for Dev10.  Takes the full path, doesn't depend on registry key
        internal const String DirectoryPath = "CompilerDirectoryPath";  // location

        // Version identifier added for Orcas.  Depends on registry key.
        internal const String NameTag = "CompilerVersion";    // name of the tag for specifying the version

        internal const String DefaultVersion = InPlaceVersion;      // should match one of the versions below
        internal const String InPlaceVersion = "v4.0";        // Default
        internal const String RedistVersion = "v3.5";        // May change with servicing
        internal const String RedistVersion20 = "v2.0";

        private const string MSBuildToolsPath = "MSBuildToolsPath";
        private const string dotNetFrameworkRegistryPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\MSBuild\\ToolsVersions\\";

        /// this method returns the location of the Orcas compilers, but will return whatever 
        /// version is requested via the COMPlus_ environment variables first
        private static string GetCompilerPathFromRegistry(String versionVal)
        {
            string dir = null;

            // if this is running in a private running environment such as Razzle, we would use the path
            // based on the environment variables: COMPLUS_InstallRoot and COMPLUS_Version.
            string comPlus_InstallRoot = Environment.GetEnvironmentVariable("COMPLUS_InstallRoot");
            string comPlus_Version = Environment.GetEnvironmentVariable("COMPLUS_Version");

            if (!string.IsNullOrEmpty(comPlus_InstallRoot) && !string.IsNullOrEmpty(comPlus_Version))
            {
                dir = Path.Combine(comPlus_InstallRoot, comPlus_Version);
                if (Directory.Exists(dir))
                    return dir;
            }

            String versionWithoutV = versionVal.Substring(1);
            String registryPath = dotNetFrameworkRegistryPath + versionWithoutV;
            dir = Registry.GetValue(registryPath, MSBuildToolsPath, null) as string;

            if (dir != null && Directory.Exists(dir))
            {
                return dir;
            }
            return null;
        }
    }
}