// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace HostActivation.Tests
{
    internal static class InstallLocationCommandResultExtensions
    {
        private static bool IsRunningInWoW64(string rid) => OperatingSystem.IsWindows() && Environment.Is64BitOperatingSystem && rid.Equals("win-x86");

        // Methods for CommandResult (used in InstallLocation.cs with imperative style)
        public static void AssertUsedDotNetRootInstallLocation(this CommandResult result, string installLocation, string rid)
        {
            result.AssertUsedDotNetRootInstallLocation(installLocation, rid, null);
        }

        public static void AssertUsedDotNetRootInstallLocation(this CommandResult result,
            string installLocation,
            string rid,
            string arch)
        {
            // If no arch is passed and we are on Windows, we need the used RID for determining whether or not we are running on WoW64.
            if (string.IsNullOrEmpty(arch))
                Assert.NotNull(rid);

            string expectedEnvironmentVariable = !string.IsNullOrEmpty(arch) ? $"DOTNET_ROOT_{arch.ToUpper()}" :
                IsRunningInWoW64(rid) ? "DOTNET_ROOT(x86)" : "DOTNET_ROOT";

            Assert.Contains($"Using environment variable {expectedEnvironmentVariable}=[{installLocation}] as runtime location.", result.StdErr);
        }

        public static void AssertUsedRegisteredInstallLocation(this CommandResult result, string installLocation)
        {
            Assert.Contains($"Found registered install location '{installLocation}'.", result.StdErr);
        }

        public static void AssertUsedGlobalInstallLocation(this CommandResult result, string installLocation)
        {
            Assert.Contains($"Using global install location [{installLocation}]", result.StdErr);
        }

        public static void AssertUsedAppLocalInstallLocation(this CommandResult result, string installLocation)
        {
            Assert.Contains($"Using app-local location [{installLocation}]", result.StdErr);
        }

        public static void AssertUsedAppRelativeInstallLocation(this CommandResult result, string installLocation)
        {
            Assert.Contains($"Using app-relative location [{installLocation}]", result.StdErr);
        }

        public static void AssertLookedForDefaultInstallLocation(this CommandResult result, string installLocationPath)
        {
            Assert.Contains($"Looking for install_location file in '{Path.Combine(installLocationPath, "install_location")}'.", result.StdErr);
        }

        public static void AssertLookedForArchitectureSpecificInstallLocation(this CommandResult result, string installLocationPath, string architecture)
        {
            Assert.Contains($"Looking for architecture-specific install_location file in '{Path.Combine(installLocationPath, "install_location_" + architecture.ToLowerInvariant())}'.", result.StdErr);
        }

        // Methods for CommandResultAssertions (used in other files with fluent style)
        public static CommandResultAssertions HaveUsedDotNetRootInstallLocation(this CommandResultAssertions assertion, string installLocation, string rid)
        {
            return assertion.HaveUsedDotNetRootInstallLocation(installLocation, rid, null);
        }

        public static CommandResultAssertions HaveUsedDotNetRootInstallLocation(this CommandResultAssertions assertion,
            string installLocation,
            string rid,
            string arch)
        {
            // If no arch is passed and we are on Windows, we need the used RID for determining whether or not we are running on WoW64.
            if (string.IsNullOrEmpty(arch))
                Assert.NotNull(rid);

            string expectedEnvironmentVariable = !string.IsNullOrEmpty(arch) ? $"DOTNET_ROOT_{arch.ToUpper()}" :
                IsRunningInWoW64(rid) ? "DOTNET_ROOT(x86)" : "DOTNET_ROOT";

            return assertion.HaveStdErrContaining($"Using environment variable {expectedEnvironmentVariable}=[{installLocation}] as runtime location.");
        }

        public static CommandResultAssertions HaveUsedRegisteredInstallLocation(this CommandResultAssertions assertion, string installLocation)
        {
            return assertion.HaveStdErrContaining($"Found registered install location '{installLocation}'.");
        }

        public static CommandResultAssertions HaveUsedGlobalInstallLocation(this CommandResultAssertions assertion, string installLocation)
        {
            return assertion.HaveStdErrContaining($"Using global install location [{installLocation}]");
        }

        public static CommandResultAssertions HaveUsedAppLocalInstallLocation(this CommandResultAssertions assertion, string installLocation)
        {
            return assertion.HaveStdErrContaining($"Using app-local location [{installLocation}]");
        }

        public static CommandResultAssertions HaveUsedAppRelativeInstallLocation(this CommandResultAssertions assertion, string installLocation)
        {
            return assertion.HaveStdErrContaining($"Using app-relative location [{installLocation}]");
        }

        public static CommandResultAssertions HaveLookedForDefaultInstallLocation(this CommandResultAssertions assertion, string installLocationPath)
        {
            return assertion.HaveStdErrContaining($"Looking for install_location file in '{Path.Combine(installLocationPath, "install_location")}'.");
        }

        public static CommandResultAssertions HaveLookedForArchitectureSpecificInstallLocation(this CommandResultAssertions assertion, string installLocationPath, string architecture)
        {
            return assertion.HaveStdErrContaining($"Looking for architecture-specific install_location file in '{Path.Combine(installLocationPath, "install_location_" + architecture.ToLowerInvariant())}'.");
        }
    }
}
