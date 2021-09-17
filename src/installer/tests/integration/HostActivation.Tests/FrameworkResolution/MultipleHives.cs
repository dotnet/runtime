// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Runtime.InteropServices;
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

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Multiple hives are only supported on Windows.
        public void FrameworkHiveSelection_GlobalHiveWithBetterMatch()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Multiple hives are only supported on Windows.
        public void FrameworkHiveSelection_MainHiveWithBetterMatch()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "6.0.0"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "6.1.2");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Multiple hives are only supported on Windows.
        public void FrameworkHiveSelection_CurrentDirectoryIsIgnored()
        {
            RunTest(
                SharedState.DotNetMainHive,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "5.0.0"))
                    .WithWorkingDirectory(SharedState.DotNetCurrentHive.BinPath))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.2.0");
        }

        private CommandResult RunTest(Func<RuntimeConfig, RuntimeConfig> runtimeConfig)
        {
            using (TestOnlyProductBehavior.Enable(SharedState.DotNetMainHive.GreatestVersionHostFxrFilePath))
            {
                return RunTest(
                    SharedState.DotNetMainHive,
                    SharedState.FrameworkReferenceApp,
                    new TestSettings()
                        .WithRuntimeConfigCustomizer(runtimeConfig)
                        .WithEnvironment(Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath, SharedState.DotNetGlobalHive.BinPath)
                        .WithEnvironment( // Redirect the default install location to an invalid location so that a machine-wide install is not used
                            Constants.TestOnlyEnvironmentVariables.DefaultInstallPath,
                            System.IO.Path.Combine(SharedState.DotNetMainHive.BinPath, "invalid")),
                    // Must enable multi-level lookup otherwise multiple hives are not enabled
                    multiLevelLookup: true);
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetMainHive { get; }

            public DotNetCli DotNetGlobalHive { get; }

            public DotNetCli DotNetCurrentHive { get; }

            public SharedTestState()
            {
                DotNetMainHive = DotNet("MainHive")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.2.0")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.2")
                    .Build();

                DotNetGlobalHive = DotNet("GlobalHive")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.2.0")
                    .Build();

                DotNetCurrentHive = DotNet("CurrentHive")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.0")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
