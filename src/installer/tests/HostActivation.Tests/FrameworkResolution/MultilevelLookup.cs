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
    // Multi-level lookup was only supported on Windows.
    [PlatformSpecific(TestPlatforms.Windows)]
    public class MultilevelLookup :
        FrameworkResolutionBase,
        IClassFixture<MultilevelLookup.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public MultilevelLookup(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Theory]
        // MLL was enabled by default before 7.0
        // Global hive with better match (higher patch)
        [InlineData("6.0.0", "netcoreapp3.1", true, "6.1.4")]
        [InlineData("6.0.0", "netcoreapp3.1", null, "6.1.4")] // MLL is on by default, so same as true
        [InlineData("6.0.0", "netcoreapp3.1", false, "6.1.3")] // No global hive, so the main hive version is picked
        // Main hive with better match (higher patch)
        [InlineData("6.2.0", "net6.0", true, "6.2.1")]
        [InlineData("6.2.0", "net6.0", null, "6.2.1")]
        [InlineData("6.2.0", "net6.0", false, "6.2.1")]
        // MLL is disabled for 7.0+
        [InlineData("7.0.0", "net8.0", true, "7.1.2")] // MLL disabled for 7.0+ - setting it doesn't change anything
        [InlineData("7.0.0", "net8.0", null, "7.1.2")]
        [InlineData("7.0.0", "net8.0", false, "7.1.2")]
        public void FrameworkHiveSelection(string requestedVersion, string tfm, bool? multiLevelLookup, string resolvedVersion)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithTfm(tfm)
                    .WithFramework(MicrosoftNETCoreApp, requestedVersion),
                multiLevelLookup)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedVersion);
        }

        [Fact]
        public void FrameworkHiveSelection_CurrentDirectoryIsIgnored()
        {
            RunTest(new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithTfm("net6.0")
                        .WithFramework(MicrosoftNETCoreApp, "6.0.0"))
                    .WithWorkingDirectory(SharedState.DotNetCurrentHive.BinPath),
                multiLevelLookup: true)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "6.1.4");
        }

        private record struct FrameworkInfo(string Name, string Version, int Level, string Path);

        private List<FrameworkInfo> GetExpectedFrameworks()
        {
            // The runtimes should be ordered by version number
            List<FrameworkInfo> expectedList = new();
            expectedList.AddRange(
                SharedState.MainHiveVersions.Select(
                    v => new FrameworkInfo(MicrosoftNETCoreApp, v, 1, SharedState.DotNetMainHive.BinPath)));
            expectedList.AddRange(
                SharedState.GlobalHiveVersions.Select(
                    v => new FrameworkInfo(MicrosoftNETCoreApp, v, 2, SharedState.DotNetGlobalHive.BinPath)));

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

        [Fact]
        public void FrameworkResolutionError()
        {
            string expectedOutput =
                $"The following frameworks were found:{Environment.NewLine}" +
                string.Join(string.Empty,
                    GetExpectedFrameworks()
                        .Select(t => $"  {t.Version} at [{Path.Combine(t.Path, "shared", MicrosoftNETCoreApp)}]{Environment.NewLine}"));

            RunTest(
                runtimeConfig => runtimeConfig
                    .WithTfm("net6.0") // MLL can only be enabled before 7.0
                    .WithFramework(MicrosoftNETCoreApp, "9999.9.9"),
                multiLevelLookup: true)
                .Should().Fail()
                .And.HaveStdErrContaining(expectedOutput)
                .And.HaveStdErrContaining("https://aka.ms/dotnet/app-launch-failed");
        }

        private CommandResult RunTest(Func<RuntimeConfig, RuntimeConfig> runtimeConfig, bool? multiLevelLookup, [CallerMemberName] string caller = "")
            => RunTest(new TestSettings().WithRuntimeConfigCustomizer(runtimeConfig), multiLevelLookup, caller);

        private CommandResult RunTest(TestSettings testSettings, bool? multiLevelLookup, [CallerMemberName] string caller = "")
        {
            Command command = GetTestCommand(
                SharedState.DotNetMainHive,
                SharedState.App,
                testSettings
                    .WithEnvironment(Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath, SharedState.DotNetGlobalHive.BinPath)
                    .WithEnvironment( // Redirect the default install location to an invalid location so that a machine-wide install is not used
                        Constants.TestOnlyEnvironmentVariables.DefaultInstallPath,
                        System.IO.Path.Combine(SharedState.DotNetMainHive.BinPath, "invalid")));
            return command.MultilevelLookup(multiLevelLookup)
                .Execute(caller);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp App { get; }

            public DotNetCli DotNetMainHive { get; }
            public string[] MainHiveVersions { get; } = ["6.1.3", "6.2.1", "7.1.2" ];

            public DotNetCli DotNetGlobalHive { get; }
            public string[] GlobalHiveVersions { get; } = [ "6.1.4", "6.2.0", "7.1.2" ];

            public DotNetCli DotNetCurrentHive { get; }

            public SharedTestState()
            {
                DotNetBuilder mainHive = DotNet("MainHive");
                foreach (string version in MainHiveVersions)
                    mainHive.AddMicrosoftNETCoreAppFrameworkMockHostPolicy(version);

                DotNetMainHive = mainHive.Build();

                DotNetBuilder globalHive = DotNet("GlobalHive");
                foreach (string version in GlobalHiveVersions)
                    globalHive.AddMicrosoftNETCoreAppFrameworkMockHostPolicy(version);

                DotNetGlobalHive = globalHive.Build();

                DotNetCurrentHive = DotNet("CurrentHive")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.3.0")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("7.3.0")
                    .Build();

                App = CreateFrameworkReferenceApp();

                // Enable test-only behaviour. We don't bother disabling the behaviour later,
                // as we just delete the entire copy after the tests run.
                _ = TestOnlyProductBehavior.Enable(DotNetMainHive.GreatestVersionHostFxrFilePath);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
            }
        }
    }
}
