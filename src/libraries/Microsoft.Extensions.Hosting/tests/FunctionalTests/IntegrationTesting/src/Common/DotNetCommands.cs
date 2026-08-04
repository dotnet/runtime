// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public static class DotNetCommands
    {
        private const string _dotnetFolderName = ".dotnet";

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
            var fileName = DotNetExecutableName;

            foreach (var directory in GetMuxerProbingDirectories())
            {
                if (string.IsNullOrEmpty(directory))
                {
                    continue;
                }

                string candidate;
                try
                {
                    candidate = Path.Combine(directory, fileName);
                }
                catch (ArgumentException)
                {
                    // Ignore malformed PATH entries.
                    continue;
                }

                if (IsExecutable(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        // Process.Start only accepts a candidate that is executable, so a stray non-executable file named
        // "dotnet" must not shadow a real muxer further along the search path.
        private static bool IsExecutable(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

#if NETFRAMEWORK
            return true;
#else
            if (OperatingSystem.IsWindows())
            {
                return true;
            }

            const UnixFileMode ExecuteBits = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            return (File.GetUnixFileMode(path) & ExecuteBits) != 0;
#endif
        }

        // Probe the same places Process.Start searches when it is handed a bare file name: the directory
        // holding the executable that is running the tests, the current directory, and then every entry on
        // PATH. See ResolvePath in System.Diagnostics.Process.
        private static IEnumerable<string> GetMuxerProbingDirectories()
        {
            yield return HostExecutableDirectory;
            yield return Directory.GetCurrentDirectory();
            
            const StringSplitOptions splitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
            var path = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var directory in path.Split(Path.PathSeparator, splitOptions))
                {
                    yield return directory;
                }
            }
        }

        private static string HostExecutableDirectory =>
#if NETFRAMEWORK
            // On .NET Framework the entry executable is the application itself, so the app base
            // directory is the directory holding it.
            AppContext.BaseDirectory;
#else
            Path.GetDirectoryName(Environment.ProcessPath);
#endif
    }
}
