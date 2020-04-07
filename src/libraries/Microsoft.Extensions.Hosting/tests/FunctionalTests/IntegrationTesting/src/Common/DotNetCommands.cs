// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public static class DotNetCommands
    {
        private const string _dotnetFolderName = ".dotnet";

        internal static string DotNetHome { get; } = GetDotNetHome();

        // Compare to https://github.com/aspnet/BuildTools/blob/314c98e4533217a841ff9767bb38e144eb6c93e4/tools/KoreBuild.Console/Commands/CommandContext.cs#L76
        public static string GetDotNetHome()
        {
            var dotnetHome = Environment.GetEnvironmentVariable("DOTNET_HOME");
            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
            var home = Environment.GetEnvironmentVariable("HOME");

            var result = Path.Combine(Directory.GetCurrentDirectory(), _dotnetFolderName);
            if (!string.IsNullOrEmpty(dotnetHome))
            {
                result = dotnetHome;
            }
            else if (!string.IsNullOrEmpty(dotnetRoot))
            {
                // DOTNET_ROOT has x64 appended to the path, which we append again in GetDotNetInstallDir
                result = dotnetRoot.Substring(0, dotnetRoot.Length - 3);
            }
            else if (!string.IsNullOrEmpty(userProfile))
            {
                result = Path.Combine(userProfile, _dotnetFolderName);
            }
            else if (!string.IsNullOrEmpty(home))
            {
                result = home;
            }

            return result;
        }

        public static string GetDotNetInstallDir(RuntimeArchitecture arch)
        {
            var dotnetDir = DotNetHome;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dotnetDir = Path.Combine(dotnetDir, arch.ToString());
            }

            return dotnetDir;
        }

        public static string GetDotNetExecutable(RuntimeArchitecture arch)
        {
            var dotnetDir = GetDotNetInstallDir(arch);

            var dotnetFile = "dotnet";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dotnetFile += ".exe";
            }

            return Path.Combine(dotnetDir, dotnetFile);
        }

        public static bool IsRunningX86OnX64(RuntimeArchitecture arch)
        {
            return (RuntimeInformation.OSArchitecture == Architecture.X64 || RuntimeInformation.OSArchitecture == Architecture.Arm64)
                && arch == RuntimeArchitecture.x86;
        }
    }
}
