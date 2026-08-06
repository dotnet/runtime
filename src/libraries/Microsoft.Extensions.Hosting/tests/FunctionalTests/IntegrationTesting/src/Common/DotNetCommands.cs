// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public static class DotNetCommands
    {
        private const string _dotnetFolderName = ".dotnet";

        // Set by the test run script to the host it was handed via --runtime-path: the locally built
        // testhost for a local run, the Helix correlation payload in CI. See the SetTestDotNetHostPath
        // target in Microsoft.Extensions.Hosting.Functional.Tests.csproj.
        private const string HostPathVariableName = "__TestDotNetHostPath";

        internal static string DotNetHome { get; } = GetDotNetHome();

        /// <summary>
        /// Gets the full path of the muxer that portable applications are launched with, or
        /// <see langword="null"/> when the current test environment has not got one.
        /// </summary>
        public static string DotNetMuxerPath { get; } = FindDotNetMuxer();

        public static string DotNetExecutableName
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

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
            => Path.Combine(GetDotNetInstallDir(arch), DotNetExecutableName);

        public static bool IsRunningX86OnX64(RuntimeArchitecture arch)
        {
            return (RuntimeInformation.OSArchitecture == Architecture.X64 || RuntimeInformation.OSArchitecture == Architecture.Arm64)
                && arch == RuntimeArchitecture.x86;
        }

        private static string FindDotNetMuxer()
        {
            var fromRunScript = Environment.GetEnvironmentVariable(HostPathVariableName);
            if (!string.IsNullOrEmpty(fromRunScript) && File.Exists(fromRunScript))
            {
                return fromRunScript;
            }

#if NETFRAMEWORK
            return null;
#else
            // Outside the run script the only host we can vouch for is the one running the tests, which is
            // the muxer itself on every leg that does not publish the tests as a self-contained application.
            var processPath = Environment.ProcessPath;
            return string.Equals(Path.GetFileName(processPath), DotNetExecutableName, StringComparison.OrdinalIgnoreCase)
                ? processPath
                : null;
#endif
        }
    }
}
