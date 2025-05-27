// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.CoreSetup.Test.HostActivation;
using Microsoft.DotNet.TestUtils;
using Xunit;

namespace HostActivation.Tests
{
    public class HostCommands : IClassFixture<HostCommands.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public HostCommands(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Fact]
        public void ListRuntimes_OtherArchitectures()
        {
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(SharedState.DotNet.GreatestVersionHostFxrFilePath))
            {
                registeredInstallLocationOverride.SetInstallLocation(SharedState.OtherArchInstallLocations);
                foreach ((string arch, string installLocation) in SharedState.OtherArchInstallLocations)
                {
                    // Verifiy exact match of command output. The output of --list-runtimes is intended to be machine-readable
                    // and must not change in a way that breaks existing parsing.
                    string expectedOutput = GetListRuntimesOutput(installLocation, SharedState.InstalledVersions);
                    SharedState.DotNet.Exec("--list-runtimes", "--arch", arch)
                        .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                        .CaptureStdOut()
                        .Execute()
                        .Should().Pass()
                        .And.HaveStdOut(expectedOutput);
                }
            }
        }

        [Fact]
        public void ListSdks_OtherArchitectures()
        {
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(SharedState.DotNet.GreatestVersionHostFxrFilePath))
            {
                registeredInstallLocationOverride.SetInstallLocation(SharedState.OtherArchInstallLocations);
                foreach ((string arch, string installLocation) in SharedState.OtherArchInstallLocations)
                {
                    // Verifiy exact match of command output. The output of --list-sdks is intended to be machine-readable
                    // and must not change in a way that breaks existing parsing.
                    string expectedOutput = GetListSdksOutput(installLocation, SharedState.InstalledVersions);
                    SharedState.DotNet.Exec("--list-sdks", "--arch", arch)
                        .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                        .CaptureStdOut()
                        .Execute()
                        .Should().Pass()
                        .And.HaveStdOut(expectedOutput);
                }
            }
        }

        private static string GetListRuntimesOutput(string installLocation, string[] versions)
        {
            string runtimePath = Path.Combine(installLocation, "shared", Constants.MicrosoftNETCoreApp);
            return string.Join(string.Empty, versions.Select(v => $"{Constants.MicrosoftNETCoreApp} {v} [{runtimePath}]{Environment.NewLine}"));
        }

        private static string GetListSdksOutput(string installLocation, string[] versions)
        {
            string sdkPath = Path.Combine(installLocation, "sdk");
            return string.Join(string.Empty, versions.Select(v => $"{v} [{sdkPath}]{Environment.NewLine}"));
        }

        public sealed class SharedTestState : IDisposable
        {
            public DotNetCli DotNet { get; }

            // Versions are assumed to be in ascending order. We use this property to check against the
            // exact expected output of --list-sdks and --list-runtimes.
            public string[] InstalledVersions { get; } = ["5.0.5", "10.0.0", "9999.0.1"];

            public (string, string)[] OtherArchInstallLocations { get; }

            private TestArtifact _artifact;

            public SharedTestState()
            {
                _artifact = TestArtifact.Create(nameof(HostCommands));

                var builder = new DotNetBuilder(_artifact.Location, TestContext.BuiltDotNet.BinPath, "exe");
                foreach (string version in InstalledVersions)
                {
                    builder.AddMicrosoftNETCoreAppFrameworkMockHostPolicy(version);
                    builder.AddMockSDK(version, version);
                }

                DotNet = builder.Build();

                // Add runtimes and SDKs for other architectures
                string[] otherArchs = new[] { "arm", "arm64", "x64", "x86" }.Where(a => a != TestContext.BuildArchitecture).ToArray();
                OtherArchInstallLocations = new (string, string)[otherArchs.Length];
                for (int i = 0; i < otherArchs.Length; i++)
                {
                    string arch = otherArchs[i];
                    string installLocation = Directory.CreateDirectory(Path.Combine(_artifact.Location, arch)).FullName;
                    foreach (string version in InstalledVersions)
                    {
                        DotNetBuilder.AddMicrosoftNETCoreAppFrameworkMockHostPolicy(installLocation, version);
                        DotNetBuilder.AddMockSDK(installLocation, version, version);
                    }

                    OtherArchInstallLocations[i] = (arch, installLocation);
                }


                // Enable test-only behavior for the copied .NET. We don't bother disabling the behaviour later,
                // as we just delete the entire copy after the tests run.
                _ = TestOnlyProductBehavior.Enable(DotNet.GreatestVersionHostFxrFilePath);

            }

            public void Dispose()
            {
                _artifact.Dispose();
            }
        }
    }
}
