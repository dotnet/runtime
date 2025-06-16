// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.CoreSetup.Test.HostActivation;
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
        public void Info()
        {
            string expectedSdksOutput =
                $"""
                .NET SDKs installed:
                {GetListSdksOutput(SharedState.DotNet.BinPath, SharedState.InstalledVersions, "  ")}
                """;
            string expectedRuntimesOutput =
                $"""
                .NET runtimes installed:
                {GetListRuntimesOutput(SharedState.DotNet.BinPath, SharedState.InstalledVersions, "  ")}
                """;
            SharedState.DotNet.Exec("--info")
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining(expectedSdksOutput)
                .And.HaveStdOutContaining(expectedRuntimesOutput)
                .And.HaveStdOutMatching($@"Architecture:\s*{TestContext.BuildArchitecture}")
                // If an SDK exists, we rely on it to print the RID. The host should not print it again.
                .And.NotHaveStdOutMatching($@"RID:\s*{TestContext.BuildRID}");
        }

        [Fact]
        public void Info_NoSDK()
        {
            string expectedSdksOutput =
                """
                .NET SDKs installed:
                  No SDKs were found
                """;
            string expectedRuntimesOutput =
                $"""
                .NET runtimes installed:
                {GetListRuntimesOutput(TestContext.BuiltDotNet.BinPath, [TestContext.MicrosoftNETCoreAppVersion], "  ")}
                """;
            TestContext.BuiltDotNet.Exec("--info")
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining(expectedSdksOutput)
                .And.HaveStdOutContaining(expectedRuntimesOutput)
                .And.HaveStdOutMatching($@"Architecture:\s*{TestContext.BuildArchitecture}")
                .And.HaveStdOutMatching($@"RID:\s*{TestContext.BuildRID}");
        }

        [Fact]
        public void Info_Utf8Path()
        {
            string installLocation = Encoding.UTF8.GetString("utf8-龯蝌灋齅ㄥ䶱"u8);
            DotNetCli dotnet = new DotNetBuilder(SharedState.Artifact.Location, TestContext.BuiltDotNet.BinPath, installLocation)
                .Build();

            dotnet.Exec("--info")
                .DotNetRoot(Path.Combine(SharedState.Artifact.Location, installLocation))
                .CaptureStdOut(Encoding.UTF8)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutMatching($@"DOTNET_ROOT.*{installLocation}");
        }

        [Fact]
        public void ListRuntimes()
        {
            // Verify exact match of command output. The output of --list-runtimes is intended to be machine-readable
            // and must not change in a way that breaks existing parsing.
            string expectedOutput = GetListRuntimesOutput(SharedState.DotNet.BinPath, SharedState.InstalledVersions);
            SharedState.DotNet.Exec("--list-runtimes")
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOut(expectedOutput);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ListRuntimes_OtherArchitecture(bool useRegisteredLocation)
        {
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(SharedState.DotNet.GreatestVersionHostFxrFilePath))
            {
                if (useRegisteredLocation)
                {
                    registeredInstallLocationOverride.SetInstallLocation(SharedState.OtherArchInstallLocations);
                }

                foreach ((string arch, string installLocation) in SharedState.OtherArchInstallLocations)
                {
                    // Verify exact match of command output. The output of --list-runtimes is intended to be machine-readable
                    // and must not change in a way that breaks existing parsing.
                    string expectedOutput = GetListRuntimesOutput(installLocation, SharedState.InstalledVersions);
                    SharedState.DotNet.Exec("--list-runtimes", "--arch", arch)
                        .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                        .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.DefaultInstallPath, useRegisteredLocation ? null : installLocation)
                        .CaptureStdOut()
                        .Execute()
                        .Should().Pass()
                        .And.HaveStdOut(expectedOutput);
                }
            }
        }

        [Fact]
        public void ListRuntimes_OtherArchitecture_NoInstalls()
        {
            // Non-host architecture - arm64 if current architecture is x64, otherwise x64
            string arch = TestContext.BuildArchitecture == "x64" ? "arm64" : "x64";
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(SharedState.DotNet.GreatestVersionHostFxrFilePath))
            {
                // Register a location that should have no runtimes installed
                registeredInstallLocationOverride.SetInstallLocation((arch, SharedState.Artifact.Location));
                SharedState.DotNet.Exec("--list-runtimes", "--arch", arch)
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOut(string.Empty);
            }
        }

        [Fact]
        public void ListRuntimes_UnknownArchitecture()
        {
            TestContext.BuiltDotNet.Exec("--list-runtimes", "--arch", "invalid")
                .CaptureStdOut()
                .Execute()
                .Should().Fail()
                .And.ExitWith(Constants.ErrorCode.InvalidArgFailure);
        }

        [Fact]
        public void ListSdks()
        {
            // Verify exact match of command output. The output of --list-sdks is intended to be machine-readable
            // and must not change in a way that breaks existing parsing.
            string expectedOutput = GetListSdksOutput(SharedState.DotNet.BinPath, SharedState.InstalledVersions);
            SharedState.DotNet.Exec("--list-sdks")
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOut(expectedOutput);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ListSdks_OtherArchitecture(bool useRegisteredLocation)
        {
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(SharedState.DotNet.GreatestVersionHostFxrFilePath))
            {
                if (useRegisteredLocation)
                {
                    registeredInstallLocationOverride.SetInstallLocation(SharedState.OtherArchInstallLocations);
                }

                foreach ((string arch, string installLocation) in SharedState.OtherArchInstallLocations)
                {
                    // Verify exact match of command output. The output of --list-sdks is intended to be machine-readable
                    // and must not change in a way that breaks existing parsing.
                    string expectedOutput = GetListSdksOutput(installLocation, SharedState.InstalledVersions);
                    SharedState.DotNet.Exec("--list-sdks", "--arch", arch)
                        .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                        .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.DefaultInstallPath, useRegisteredLocation ? null : installLocation)
                        .CaptureStdOut()
                        .Execute()
                        .Should().Pass()
                        .And.HaveStdOut(expectedOutput);
                }
            }
        }

        [Fact]
        public void ListSdks_OtherArchitecture_NoInstalls()
        {
            // Non-host architecture - arm64 if current architecture is x64, otherwise x64
            string arch = TestContext.BuildArchitecture == "x64" ? "arm64" : "x64";
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(SharedState.DotNet.GreatestVersionHostFxrFilePath))
            {
                // Register a location that should have no SDKs installed
                registeredInstallLocationOverride.SetInstallLocation((arch, SharedState.Artifact.Location));
                SharedState.DotNet.Exec("--list-sdks", "--arch", arch)
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOut(string.Empty);
            }
        }

        [Fact]
        public void ListSdks_UnknownArchitecture()
        {
            TestContext.BuiltDotNet.Exec("--list-sdks", "--arch", "invalid")
                .CaptureStdOut()
                .Execute()
                .Should().Fail()
                .And.ExitWith(Constants.ErrorCode.InvalidArgFailure);
        }

        private static string GetListRuntimesOutput(string installLocation, string[] versions, string prefix = "")
        {
            string runtimePath = Path.Combine(installLocation, "shared", Constants.MicrosoftNETCoreApp);
            return string.Join(string.Empty, versions.Select(v => $"{prefix}{Constants.MicrosoftNETCoreApp} {v} [{runtimePath}]{Environment.NewLine}"));
        }

        private static string GetListSdksOutput(string installLocation, string[] versions, string prefix = "")
        {
            string sdkPath = Path.Combine(installLocation, "sdk");
            return string.Join(string.Empty, versions.Select(v => $"{prefix}{v} [{sdkPath}]{Environment.NewLine}"));
        }

        public sealed class SharedTestState : IDisposable
        {
            public TestArtifact Artifact { get; }
            public DotNetCli DotNet { get; }

            // Versions are assumed to be in ascending order. We use this property to check against the
            // exact expected output of --list-sdks and --list-runtimes.
            public string[] InstalledVersions { get; } = ["5.0.5", "10.0.0", "9999.0.1"];

            public (string, string)[] OtherArchInstallLocations { get; }

            public SharedTestState()
            {
                Artifact = TestArtifact.Create(nameof(HostCommands));

                var builder = new DotNetBuilder(Artifact.Location, TestContext.BuiltDotNet.BinPath, "exe");
                foreach (string version in InstalledVersions)
                {
                    builder.AddMicrosoftNETCoreAppFrameworkMockHostPolicy(version);
                    builder.AddMockSDK(version, version);
                }

                DotNet = builder.Build();

                // Add runtimes and SDKs for other architectures
                string[] otherArchs = new[] { "arm64", "x64", "x86" }.Where(a => a != TestContext.BuildArchitecture).ToArray();
                OtherArchInstallLocations = new (string, string)[otherArchs.Length];
                for (int i = 0; i < otherArchs.Length; i++)
                {
                    string arch = otherArchs[i];
                    string installLocation = Directory.CreateDirectory(Path.Combine(Artifact.Location, arch)).FullName;
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
                Artifact.Dispose();
            }
        }
    }
}
