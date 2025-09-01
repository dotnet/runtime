// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DotNet.Cli.Build;
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
        public void Info_ListEnvironment()
        {
            var command = TestContext.BuiltDotNet.Exec("--info")
                .CaptureStdOut();

            // Add DOTNET_ROOT environment variables
            (string Architecture, string Path)[] dotnetRootEnvVars = [
                ("arm64", "/arm64/dotnet/root"),
                ("x64", "/x64/dotnet/root"),
                ("x86", "/x86/dotnet/root"),
                ("unknown", "/unknown/dotnet/root")
            ];
            foreach (var envVar in dotnetRootEnvVars)
            {
                command = command.DotNetRoot(envVar.Path, envVar.Architecture);
            }

            string dotnetRootNoArch = "/dotnet/root";
            command = command.DotNetRoot(dotnetRootNoArch);

            // Add additional DOTNET_* environment variables
            (string Name, string Value)[] envVars = [
                ("DOTNET_ROLL_FORWARD", "Major"),
                ("DOTNET_SOME_SETTING", "/some/setting"),
                ("DOTNET_HOST_TRACE", "1")
            ];

            (string Name, string Value)[] differentCaseEnvVars = [
                ("dotnet_env_var", "dotnet env var value"),
                ("dOtNeT_setting", "doOtNeT setting value"),
            ];
            foreach ((string name, string value) in envVars.Concat(differentCaseEnvVars))
            {
                command = command.EnvironmentVariable(name, value);
            }

            string otherEnvVar = "OTHER";
            command = command.EnvironmentVariable(otherEnvVar, "value");

            var result = command.Execute();
            result.Should().Pass()
                .And.HaveStdOutContaining("Environment variables:")
                .And.HaveStdOutMatching($@"{Constants.DotnetRoot.EnvironmentVariable}\s*\[{dotnetRootNoArch}\]")
                .And.NotHaveStdOutContaining(otherEnvVar);

            foreach ((string architecture, string path) in dotnetRootEnvVars)
            {
                result.Should()
                    .HaveStdOutMatching($@"{Constants.DotnetRoot.ArchitectureEnvironmentVariablePrefix}{architecture.ToUpper()}\s*\[{path}\]");
            }

            foreach ((string name, string value) in envVars)
            {
                result.Should().HaveStdOutMatching($@"{name}\s*\[{value}\]");
            }

            foreach ((string name, string value) in differentCaseEnvVars)
            {
                if (OperatingSystem.IsWindows())
                {
                    // Environment variables are case-insensitive on Windows
                    result.Should().HaveStdOutMatching($@"{name}\s*\[{value}\]");
                }
                else
                {
                    result.Should().NotHaveStdOutContaining(name);
                }
            }
        }

        [Fact]
        public void Info_ListEnvironment_LegacyPrefixDetection()
        {
            string comPlusEnvVar = "COMPlus_ReadyToRun";
            TestContext.BuiltDotNet.Exec("--info")
                .EnvironmentVariable(comPlusEnvVar, "0")
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Environment variables:")
                .And.NotHaveStdOutContaining(comPlusEnvVar)
                .And.HaveStdOutContaining("Detected COMPlus_* environment variable(s). Consider transitioning to DOTNET_* equivalent.");
        }

        [Fact]
        public void Info_GlobalJson_InvalidJson()
        {
            using (TestArtifact workingDir = TestArtifact.Create(nameof(Info_GlobalJson_InvalidJson)))
            {
                string globalJsonPath = GlobalJson.Write(workingDir.Location, "{ \"sdk\": { }");
                TestContext.BuiltDotNet.Exec("--info")
                    .WorkingDirectory(workingDir.Location)
                    .CaptureStdOut().CaptureStdErr()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining($"Invalid [{globalJsonPath}]")
                    .And.HaveStdOutContaining("JSON parsing exception:")
                    .And.NotHaveStdErr();
            }
        }

        [Theory]
        [InlineData("9")]
        [InlineData("9.0")]
        [InlineData("9.0.x")]
        [InlineData("invalid")]
        public void Info_GlobalJson_InvalidData(string version)
        {
            using (TestArtifact workingDir = TestArtifact.Create(nameof(Info_GlobalJson_InvalidData)))
            {
                string globalJsonPath = GlobalJson.CreateWithVersion(workingDir.Location, version);
                TestContext.BuiltDotNet.Exec("--info")
                    .WorkingDirectory(workingDir.Location)
                    .CaptureStdOut().CaptureStdErr()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining($"Invalid [{globalJsonPath}]")
                    .And.HaveStdOutContaining($"Version '{version}' is not valid for the 'sdk/version' value")
                    .And.HaveStdOutContaining($"Invalid global.json is ignored for SDK resolution")
                    .And.NotHaveStdErr();
            }
        }

        [Theory]
        [InlineData("9.0.0")]
        [InlineData("9.1.99")]
        public void Info_GlobalJson_NonExistentFeatureBand(string version)
        {
            using (TestArtifact workingDir = TestArtifact.Create(nameof(Info_GlobalJson_NonExistentFeatureBand)))
            {
                string globalJsonPath = GlobalJson.CreateWithVersion(workingDir.Location, version);
                var result = TestContext.BuiltDotNet.Exec("--info")
                    .WorkingDirectory(workingDir.Location)
                    .CaptureStdOut().CaptureStdErr()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining($"Invalid [{globalJsonPath}]")
                    .And.HaveStdOutContaining($"Version '{version}' is not valid for the 'sdk/version' value. SDK feature bands start at 1 - for example, {Version.Parse(version).ToString(2)}.100")
                    .And.NotHaveStdOutContaining($"Invalid global.json is ignored for SDK resolution")
                    .And.NotHaveStdErr();
            }
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
