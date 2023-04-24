// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace HostActivation.Tests
{
    internal static class InstallLocationCommandResultExtensions
    {
        private static bool IsRunningInWoW64(string rid) => OperatingSystem.IsWindows() && Environment.Is64BitOperatingSystem && rid.Equals("win-x86");

        public static AndConstraint<CommandResultAssertions> HaveUsedDotNetRootInstallLocation(this CommandResultAssertions assertion, string installLocation, string rid)
        {
            return assertion.HaveUsedDotNetRootInstallLocation(installLocation, rid, null);
        }

        public static AndConstraint<CommandResultAssertions> HaveUsedDotNetRootInstallLocation(this CommandResultAssertions assertion,
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

        public static AndConstraint<CommandResultAssertions> HaveUsedRegisteredInstallLocation(this CommandResultAssertions assertion, string installLocation)
        {
            return assertion.HaveStdErrContaining($"Found registered install location '{installLocation}'.");
        }

        public static AndConstraint<CommandResultAssertions> HaveUsedGlobalInstallLocation(this CommandResultAssertions assertion, string installLocation)
        {
            return assertion.HaveStdErrContaining($"Using global installation location [{installLocation}]");
        }

        public static AndConstraint<CommandResultAssertions> HaveLookedForDefaultInstallLocation(this CommandResultAssertions assertion, string installLocationPath)
        {
            return assertion.HaveStdErrContaining($"Looking for install_location file in '{Path.Combine(installLocationPath, "install_location")}'.");
        }

        public static AndConstraint<CommandResultAssertions> HaveLookedForArchitectureSpecificInstallLocation(this CommandResultAssertions assertion, string installLocationPath, string architecture)
        {
            return assertion.HaveStdErrContaining($"Looking for architecture-specific install_location file in '{Path.Combine(installLocationPath, "install_location_" + architecture.ToLowerInvariant())}'.");
        }
    }
}
