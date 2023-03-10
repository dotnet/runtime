// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class Binaries
    {
        public static class HostFxr
        {
            public static string FileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);
        }

        public static class HostPolicy
        {
            public static string FileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);

            public static string Mock = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("mockhostpolicy");
            public static string MockFilePath = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, Mock);
        }

        public static class CoreClr
        {
            public static string FileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("coreclr");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);

            public static string Mock = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("mockcoreclr");
            public static string MockFilePath = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, Mock);
        }
    }
}
