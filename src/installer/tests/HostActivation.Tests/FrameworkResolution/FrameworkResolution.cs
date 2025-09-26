// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;
using static Microsoft.DotNet.CoreSetup.Test.Constants;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class FrameworkResolution :
        FrameworkResolutionBase,
        IClassFixture<FrameworkResolution.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public FrameworkResolution(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Theory]
        // Minor roll forward by default - closest minor (exact or lowest higher), latest patch
        [InlineData("6.0.0", "6.1.3")]
        [InlineData("6.1.2", "6.1.3")]
        [InlineData("6.1.4", ResolvedFramework.NotFound)]
        [InlineData("7.0.0", "7.2.3")]
        public void Default(string requestedVersion, string resolvedVersion)
        {
            CommandResult result = RunTest(
                new TestSettings().WithRuntimeConfigCustomizer(rc =>
                    rc.WithFramework(MicrosoftNETCoreApp, requestedVersion)));

            result.ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedVersion);
            if (SharedState.EmptyVersions.Contains(requestedVersion))
                result.Should().HaveStdErrContaining($"Ignoring FX version [{requestedVersion}] without .deps.json");
        }

        [Fact]
        public void FrameworkResolutionError()
        {
            IEnumerable<string> installedVersions = SharedState.InstalledVersions.Select(v => $"  {v} at [{SharedState.InstalledDotNet.SharedFxPath}]{Environment.NewLine}");
            string expectedOutput =
                $"""
                The following frameworks were found:
                {string.Join(string.Empty, installedVersions)}
                """;

            RunTest(
                new TestSettings().WithRuntimeConfigCustomizer(rc =>
                    rc.WithFramework(MicrosoftNETCoreApp, "9999.9.9")))
                .Should().Fail()
                .And.HaveStdErrContaining(expectedOutput)
                .And.HaveStdErrContaining("https://aka.ms/dotnet/app-launch-failed")
                .And.HaveStdErrContaining("Ignoring FX version [9999.9.9] without .deps.json");
        }

        [Fact]
        public void FrameworkResolutionError_ListOtherArchitectures()
        {
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(SharedState.InstalledDotNet.GreatestVersionHostFxrFilePath))
            using (var otherArchArtifact = TestArtifact.Create("otherArch"))
            {
                string requestedVersion = "9999.9.9";
                string[] otherArchs = ["arm64", "x64", "x86"];
                var installLocations = new (string, string)[otherArchs.Length];
                for (int i = 0; i < otherArchs.Length; i++)
                {
                    string arch = otherArchs[i];

                    // Create a .NET install with Microsoft.NETCoreApp at the registered location
                    var dotnet = new DotNetBuilder(otherArchArtifact.Location, TestContext.BuiltDotNet.BinPath, arch)
                        .AddMicrosoftNETCoreAppFrameworkMockHostPolicy(requestedVersion)
                        .Build();
                    installLocations[i] = (arch, dotnet.BinPath);
                }

                registeredInstallLocationOverride.SetInstallLocation(installLocations);

                CommandResult result = RunTest(
                    new TestSettings()
                        .WithRuntimeConfigCustomizer(c => c.WithFramework(MicrosoftNETCoreApp, requestedVersion))
                        .WithEnvironment(TestOnlyEnvironmentVariables.RegisteredConfigLocation, registeredInstallLocationOverride.PathValueOverride));

                result.ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion)
                    .And.HaveStdErrContaining("The following frameworks for other architectures were found:");

                // Error message should list framework found for other architectures
                foreach ((string arch, string path) in installLocations)
                {
                    if (arch == TestContext.BuildArchitecture)
                        continue;

                    string expectedPath = System.Text.RegularExpressions.Regex.Escape(Path.Combine(path, "shared", MicrosoftNETCoreApp));
                    result.Should()
                        .HaveStdErrMatching($@"{arch}\s*{requestedVersion} at \[{expectedPath}\]", System.Text.RegularExpressions.RegexOptions.Multiline);
                }
            }
        }

        [Theory]
        [InlineData("6.1.0", "", "6.1.3")]      // Roll forward to 6.1.3 - empty value has no effect on resolution
        [InlineData("6.1.0", "   ", "6.1.3")]   // Roll forward to 6.1.3 - whitespace value has no effect on resolution
        [InlineData("6.1.0", "6.1.2", "6.1.3")] // Roll forward to 6.1.3 when 6.1.2 is disabled
        [InlineData("6.1.0", "6.1.3", "6.1.2")] // Roll forward to 6.1.2 when 6.1.3 is disabled
        [InlineData("6.1.3", "6.1.3", ResolvedFramework.NotFound)] // Fail when the only matching version is disabled
        [InlineData("7.2.0", "7.2.3", ResolvedFramework.NotFound)] // Fail when the only matching version is disabled
        [InlineData("6.1.0", "6.1.2;6.1.3", ResolvedFramework.NotFound)] // Fail when all matching versions are disabled
        [InlineData("6.1.0", "invalid;6.1.3", "6.1.2")]  // Roll forward to 6.1.2 - invalid value ignored, 6.1.3 disabled
        [InlineData("6.1.0", "v6.1.3;;6.1.0", "6.1.3")]  // Roll forward to 6.1.3 - invalid or non-existent versions have no effect on resolution
        public void DisabledVersions(string requestedVersion, string disabledVersions, string expectedResolution)
        {
            CommandResult result = RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(rc => rc.WithFramework(MicrosoftNETCoreApp, requestedVersion))
                    .WithEnvironment(Constants.DisableRuntimeVersions.EnvironmentVariable, disabledVersions));

            result.ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, expectedResolution);
            if (string.IsNullOrWhiteSpace(disabledVersions))
            {
                result.Should().NotHaveStdErrContaining($"Ignoring disabled version");
            }
            else
            {
                foreach (string value in disabledVersions.Split(';'))
                {
                    if (SharedState.InstalledVersions.Contains(value))
                    {
                        result.Should().HaveStdErrContaining($"Ignoring disabled version [{value}]");
                        if (expectedResolution == ResolvedFramework.NotFound)
                        {
                            result.Should().HaveStdErrContaining(
                                $"""
                                  {value} at [{SharedState.InstalledDotNet.SharedFxPath}]
                                    Disabled via {Constants.DisableRuntimeVersions.EnvironmentVariable} environment variable
                                """);
                                
                        }
                    }
                }
            }
        }

        [Fact]
        public void DisabledVersions_NoRollForward()
        {
            string disabledVersion = "6.1.2";
            CommandResult result = RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(rc => rc
                        .WithFramework(MicrosoftNETCoreApp, disabledVersion)
                        .WithRollForward(Constants.RollForwardSetting.Disable))
                    .WithEnvironment(Constants.DisableRuntimeVersions.EnvironmentVariable, disabledVersion));

            result.ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, disabledVersion)
                .And.HaveStdErrContaining($"Ignoring disabled version [{disabledVersion}]");
        }

        private CommandResult RunTest(TestSettings testSettings, [CallerMemberName] string caller = "")
        {
            return RunTest(
                SharedState.InstalledDotNet,
                SharedState.App,
                testSettings,
                caller: caller);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp App { get; }

            public DotNetCli InstalledDotNet { get; }

            // Versions are assumed to be in ascending order. We use this property to check against the
            // exact expected output of --list-sdks and --list-runtimes.
            public string[] InstalledVersions { get; } = ["6.1.2", "6.1.3", "7.2.3"];

            public string[] EmptyVersions { get; } = ["6.0.0", "7.0.0", "9999.9.9"];

            public SharedTestState()
            {
                var builder = DotNet("DotNet");
                foreach (string version in InstalledVersions)
                    builder.AddMicrosoftNETCoreAppFrameworkMockHostPolicy(version);

                InstalledDotNet = builder.Build();

                // Empty Microsoft.NETCore.App directory - should not be recognized as a valid framework
                // Version is the best match for some test cases, but they should be ignored
                foreach (string version in EmptyVersions)
                    Directory.CreateDirectory(Path.Combine(InstalledDotNet.SharedFxPath, version));

                // Enable test-only behaviour. We always do this - even for tests that don't need the behaviour.
                // On macOS with system integrity protection enabled, if the binary is loaded, modified (test-only
                // behaviour rewrites part of the binary), and loaded again, the process will crash.
                // We don't bother disabling it later, as we just delete the containing folder after tests run.
                _ = TestOnlyProductBehavior.Enable(InstalledDotNet.GreatestVersionHostFxrFilePath);

                App = CreateFrameworkReferenceApp();
            }
        }
    }
}
