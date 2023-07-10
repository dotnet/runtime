// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class Binaries
    {
        public static string GetExeFileNameForCurrentPlatform(string exeName) =>
            exeName + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty);

        public static (string, string) GetSharedLibraryPrefixSuffix()
        {
            if (OperatingSystem.IsWindows())
                return (string.Empty, ".dll");

            if (OperatingSystem.IsMacOS())
                return ("lib", ".dylib");

            return ("lib", ".so");
        }

        public static string GetSharedLibraryFileNameForCurrentPlatform(string libraryName)
        {
            (string prefix, string suffix) = GetSharedLibraryPrefixSuffix();
            return prefix + libraryName + suffix;
        }

        public static class AppHost
        {
            public static string FileName = GetExeFileNameForCurrentPlatform("apphost");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);
        }

        public static class CoreClr
        {
            public static string FileName = GetSharedLibraryFileNameForCurrentPlatform("coreclr");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);

            public static string MockName = GetSharedLibraryFileNameForCurrentPlatform("mockcoreclr");
            public static string MockPath = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, MockName);
        }

        public static class DotNet
        {
            public static string FileName = GetExeFileNameForCurrentPlatform("dotnet");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);
        }

        public static class HostFxr
        {
            public static string FileName = GetSharedLibraryFileNameForCurrentPlatform("hostfxr");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);

            public static string MockName_2_2 = GetSharedLibraryFileNameForCurrentPlatform("mockhostfxr_2_2");
            public static string MockName_5_0 = GetSharedLibraryFileNameForCurrentPlatform("mockhostfxr_5_0");
            public static string MockPath_2_2 = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, MockName_2_2);
            public static string MockPath_5_0 = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, MockName_5_0);
        }

        public static class HostPolicy
        {
            public static string FileName = GetSharedLibraryFileNameForCurrentPlatform("hostpolicy");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);

            public static string MockName = GetSharedLibraryFileNameForCurrentPlatform("mockhostpolicy");
            public static string MockPath = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, MockName);
        }

        public static class NetHost
        {
            public static string FileName = GetSharedLibraryFileNameForCurrentPlatform("nethost");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);
        }

        public static class SingleFileHost
        {
            public static string FileName = GetExeFileNameForCurrentPlatform("singlefilehost");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);
        }
    }
}
