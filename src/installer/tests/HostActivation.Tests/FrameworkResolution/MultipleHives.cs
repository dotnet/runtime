// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class MultipleHives :
        FrameworkResolutionBase,
        IClassFixture<MultipleHives.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public MultipleHives(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Theory]
        // MLL where global hive has a better match
        [InlineData("5.0.0", "net5.0", true, "5.1.2")]
        [InlineData("5.0.0", "net5.0", null, "5.1.2")] // MLL is on by default before 7.0, so same as true
        [InlineData("5.0.0", "net5.0", false, "5.2.0")] // No global hive allowed
        // MLL (where global hive has better match) with various TFMs
        [InlineData("5.0.0", "netcoreapp3.0", true, "5.1.2")]
        [InlineData("5.0.0", "netcoreapp3.0", null, "5.1.2")]
        [InlineData("5.0.0", "netcoreapp3.0", false, "5.2.0")]
        [InlineData("5.0.0", "netcoreapp3.1", true, "5.1.2")]
        [InlineData("5.0.0", "netcoreapp3.1", null, "5.1.2")]
        [InlineData("5.0.0", "netcoreapp3.1", false, "5.2.0")]
        [InlineData("5.0.0", "net6.0", true, "5.1.2")]
        [InlineData("5.0.0", "net6.0", null, "5.1.2")]
        [InlineData("5.0.0", "net6.0", false, "5.2.0")]
        // MLL is disabled for 7.0+
        [InlineData("7.0.0", "net7.0", true, "7.1.2")] // MLL disabled for 7.0+ - setting it doesn't change anything
        [InlineData("7.0.0", "net7.0", null, "7.1.2")]
        [InlineData("7.0.0", "net7.0", false, "7.1.2")]
        [InlineData("7.0.0", "net8.0", true, "7.1.2")] // MLL disabled for 7.0+ - setting it doesn't change anything
        [InlineData("7.0.0", "net8.0", null, "7.1.2")]
        [InlineData("7.0.0", "net8.0", false, "7.1.2")]
        // MLL where main hive has a better match
        [InlineData("6.0.0", "net6.0", true, "6.1.4")] // Global hive with better version (higher patch)
        [InlineData("6.0.0", "net6.0", null, "6.1.4")] // MLL is on by default, so same as true
        [InlineData("6.0.0", "net6.0", false, "6.1.3")] // No globla hive, so the main hive version is picked
        public void FrameworkHiveSelection(string requestedVersion, string tfm, bool? multiLevelLookup, string resolvedVersion)
        {
            // Multi-level lookup is only supported on Windows.
            if (!OperatingSystem.IsWindows() && multiLevelLookup != false)
                return;

            RunTest(
                runtimeConfig => runtimeConfig
                    .WithTfm(tfm)
                    .WithFramework(MicrosoftNETCoreApp, requestedVersion),
                multiLevelLookup)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Multiple hives are only supported on Windows.
        public void FrameworkHiveSelection_CurrentDirectoryIsIgnored()
        {
            RunTest(new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithTfm(Constants.Tfm.Net5)
                        .WithFramework(MicrosoftNETCoreApp, "5.0.0"))
                    .WithWorkingDirectory(SharedState.DotNetCurrentHive.BinPath),
                multiLevelLookup: true)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2");
        }

        [Theory]
        [InlineData("6.1.2", "net6.0", true, "6.1.2", false)] // No roll forward if --fx-version is used
        [InlineData("6.1.2", "net6.0", null, "6.1.2", false)]
        [InlineData("6.1.2", "net6.0", false, "6.1.2", false)]
        [InlineData("6.1.2", "net7.0", true, "6.1.2", false)]
        [InlineData("6.1.4", "net6.0", true, "6.1.4", true)]
        [InlineData("6.1.4", "net6.0", null, "6.1.4", true)]
        [InlineData("6.1.4", "net6.0", false, ResolvedFramework.NotFound, false)]
        [InlineData("6.1.4", "net7.0", true, ResolvedFramework.NotFound, false)]  // MLL disabled for 7.0+
        [InlineData("7.1.2", "net6.0", true, "7.1.2", false)]  // 7.1.2 is in both main and global hives - the main should always win with exact match
        [InlineData("7.1.2", "net6.0", null, "7.1.2", false)]
        [InlineData("7.1.2", "net6.0", false, "7.1.2", false)]
        [InlineData("7.1.2", "net7.0", true, "7.1.2", false)]
        public void FxVersionCLI(string fxVersion, string tfm, bool? multiLevelLookup, string resolvedVersion, bool fromGlobalHive)
        {
            // Multi-level lookup is only supported on Windows.
            if (!OperatingSystem.IsWindows() && multiLevelLookup != false)
                return;

            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(
                        runtimeConfig => runtimeConfig
                            .WithTfm(tfm)
                            .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .WithCommandLine(Constants.FxVersion.CommandLineArgument, fxVersion),
                multiLevelLookup)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedVersion, fromGlobalHive ? SharedState.DotNetGlobalHive.BinPath : SharedState.DotNetMainHive.BinPath);
        }

        private record struct FrameworkInfo(string Name, string Version, int Level, string Path);

        private List<FrameworkInfo> GetExpectedFrameworks(bool? multiLevelLookup)
        {
            // The runtimes should be ordered by version number
            List<FrameworkInfo> expectedList = new();
            expectedList.Add(new FrameworkInfo(MicrosoftNETCoreApp, "5.2.0", 1, SharedState.DotNetMainHive.BinPath));
            expectedList.Add(new FrameworkInfo(MicrosoftNETCoreApp, "6.1.2", 1, SharedState.DotNetMainHive.BinPath));
            expectedList.Add(new FrameworkInfo(MicrosoftNETCoreApp, "6.1.3", 1, SharedState.DotNetMainHive.BinPath));
            expectedList.Add(new FrameworkInfo(MicrosoftNETCoreApp, "7.1.2", 1, SharedState.DotNetMainHive.BinPath));
            if (multiLevelLookup is null || multiLevelLookup == true)
            {
                expectedList.Add(new FrameworkInfo(MicrosoftNETCoreApp, "5.1.2", 2, SharedState.DotNetGlobalHive.BinPath));
                expectedList.Add(new FrameworkInfo(MicrosoftNETCoreApp, "6.1.4", 2, SharedState.DotNetGlobalHive.BinPath));
                expectedList.Add(new FrameworkInfo(MicrosoftNETCoreApp, "6.2.0", 2, SharedState.DotNetGlobalHive.BinPath));
                expectedList.Add(new FrameworkInfo(MicrosoftNETCoreApp, "7.0.1", 2, SharedState.DotNetGlobalHive.BinPath));
                expectedList.Add(new FrameworkInfo(MicrosoftNETCoreApp, "7.1.2", 2, SharedState.DotNetGlobalHive.BinPath));
            }
            expectedList.Sort((a, b) => {
                int result = a.Name.CompareTo(b.Name);
                if (result != 0)
                    return result;

                if (!Version.TryParse(a.Version, out var aVersion))
                    return -1;

                if (!Version.TryParse(b.Version, out var bVersion))
                    return 1;

                result = aVersion.CompareTo(bVersion);
                if (result != 0)
                    return result;

                return b.Level.CompareTo(a.Level);
            });
            return expectedList;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(null)]
        [InlineData(false)]
        public void ListRuntimes(bool? multiLevelLookup)
        {
            // Multi-level lookup is only supported on Windows.
            if (!OperatingSystem.IsWindows() && multiLevelLookup != false)
                return;

            string expectedOutput = string.Join(
                string.Empty,
                GetExpectedFrameworks(false) // MLL Is always disabled for dotnet --list-runtimes
                    .Select(t => $"{MicrosoftNETCoreApp} {t.Version} [{Path.Combine(t.Path, "shared", MicrosoftNETCoreApp)}]{Environment.NewLine}"));

            // !!IMPORTANT!!: This test verifies the exact match of the entire output of the command (not a substring!)
            // This is important as the output of --list-runtimes is considered machine readable and thus must not change even in a minor way (unintentionally)
            RunTest(
                new TestSettings().WithCommandLine("--list-runtimes"),
                multiLevelLookup,
                testApp: null)
                .Should().HaveStdOut(expectedOutput);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(null)]
        [InlineData(false)]
        public void DotnetInfo(bool? multiLevelLookup)
        {
            // Multi-level lookup is only supported on Windows.
            if (!OperatingSystem.IsWindows() && multiLevelLookup != false)
                return;

            string expectedOutput =
                $".NET runtimes installed:{Environment.NewLine}" +
                string.Join(string.Empty,
                    GetExpectedFrameworks(false) // MLL is always disabled for dotnet --info
                        .Select(t => $"  {MicrosoftNETCoreApp} {t.Version} [{Path.Combine(t.Path, "shared", MicrosoftNETCoreApp)}]{Environment.NewLine}"));

            RunTest(
                new TestSettings().WithCommandLine("--info"),
                multiLevelLookup,
                testApp: null)
                .Should().HaveStdOutContaining(expectedOutput);
        }

        [Theory]
        [InlineData("net5.0", true, true)]
        [InlineData("net5.0", null, true)]
        [InlineData("net5.0", false, false)]
        // MLL is disabled for 7.0+
        [InlineData("net7.0", true, false)]
        [InlineData("net7.0", null, false)]
        [InlineData("net7.0", false, false)]
        public void FrameworkResolutionError(string tfm, bool? multiLevelLookup, bool effectiveMultiLevelLookup)
        {
            // Multi-level lookup is only supported on Windows.
            if (!OperatingSystem.IsWindows() && multiLevelLookup != false)
                return;

            string expectedOutput =
                $"The following frameworks were found:{Environment.NewLine}" +
                string.Join(string.Empty,
                    GetExpectedFrameworks(effectiveMultiLevelLookup)
                        .Select(t => $"  {t.Version} at [{Path.Combine(t.Path, "shared", MicrosoftNETCoreApp)}]{Environment.NewLine}"));

            RunTest(
                runtimeConfig => runtimeConfig
                    .WithTfm(tfm)
                    .WithFramework(MicrosoftNETCoreApp, "9999.9.9"),
                multiLevelLookup)
                .Should().Fail()
                .And.HaveStdErrContaining(expectedOutput)
                .And.HaveStdErrContaining("https://aka.ms/dotnet/app-launch-failed");
        }

        private CommandResult RunTest(Func<RuntimeConfig, RuntimeConfig> runtimeConfig, bool? multiLevelLookup = true)
            => RunTest(new TestSettings().WithRuntimeConfigCustomizer(runtimeConfig), multiLevelLookup);

        private CommandResult RunTest(TestSettings testSettings, bool? multiLevelLookup)
            => RunTest(testSettings, multiLevelLookup, SharedState.FrameworkReferenceApp);

        private CommandResult RunTest(TestSettings testSettings, bool? multiLevelLookup, TestApp testApp)
        {
            return RunTest(
                SharedState.DotNetMainHive,
                testApp,
                testSettings
                    .WithEnvironment(Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath, SharedState.DotNetGlobalHive.BinPath)
                    .WithEnvironment( // Redirect the default install location to an invalid location so that a machine-wide install is not used
                        Constants.TestOnlyEnvironmentVariables.DefaultInstallPath,
                        System.IO.Path.Combine(SharedState.DotNetMainHive.BinPath, "invalid")),
                // Must enable multi-level lookup otherwise multiple hives are not enabled
                multiLevelLookup: multiLevelLookup);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetMainHive { get; }

            public DotNetCli DotNetGlobalHive { get; }

            public DotNetCli DotNetCurrentHive { get; }

            private readonly IDisposable _testOnlyProductBehaviorScope;

            public SharedTestState()
            {
                DotNetMainHive = DotNet("MainHive")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.2.0")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.3")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("7.1.2")
                    .Build();

                DotNetGlobalHive = DotNet("GlobalHive")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.4")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.2.0")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("7.0.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("7.1.2")
                    .Build();

                DotNetCurrentHive = DotNet("CurrentHive")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.0")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("7.3.0")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();

                _testOnlyProductBehaviorScope = TestOnlyProductBehavior.Enable(DotNetMainHive.GreatestVersionHostFxrFilePath);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _testOnlyProductBehaviorScope.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
