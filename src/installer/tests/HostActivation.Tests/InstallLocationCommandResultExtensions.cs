// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;

namespace HostActivation.Tests
{
    internal static class InstallLocationCommandResultExtensions
    {
        private static bool IsRunningInWoW64(string rid) => OperatingSystem.IsWindows() && Environment.Is64BitOperatingSystem && rid.Equals("win-x86");

        public static AndConstraint<CommandResultAssertions> HaveUsedDotNetRootInstallLocation(this CommandResultAssertions assertion, string installLocation)
        {
            return assertion.HaveUsedDotNetRootInstallLocation(installLocation, null, null);
        }

        public static AndConstraint<CommandResultAssertions> HaveUsedDotNetRootInstallLocation(this CommandResultAssertions assertion,
            string installLocation,
            string arch,
            string rid)
        {
            string expectedEnvironmentVariable = !string.IsNullOrEmpty(arch) ? $"DOTNET_ROOT_{arch.ToUpper()}" :
                IsRunningInWoW64(rid) ? "DOTNET_ROOT(x86)" : "DOTNET_ROOT";

            return assertion.HaveStdErrContaining($"Using environment variable {expectedEnvironmentVariable}=[{installLocation}] as runtime location.");
        }

        public static AndConstraint<CommandResultAssertions> HaveUsedConfigFileInstallLocation(this CommandResultAssertions assertion, string installLocation)
        {
            return assertion.HaveStdErrContaining($"Using install location '{installLocation}'.");
        }

        public static AndConstraint<CommandResultAssertions> HaveUsedGlobalInstallLocation(this CommandResultAssertions assertion, string installLocation)
        {
            return assertion.HaveStdErrContaining($"Using global installation location [{installLocation}]");
        }

        public static AndConstraint<CommandResultAssertions> HaveFoundDefaultInstallLocationInConfigFile(this CommandResultAssertions assertion, string installLocation)
        {
            return assertion.HaveStdErrContaining($"Found install location path '{installLocation}'.");
        }

        public static AndConstraint<CommandResultAssertions> HaveFoundArchSpecificInstallLocationInConfigFile(this CommandResultAssertions assertion, string installLocation, string arch)
        {
            return assertion.HaveStdErrContaining($"Found architecture-specific install location path: '{installLocation}' ('{arch}').");
        }
    }
}
