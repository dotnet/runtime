// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public void FrameworkHiveSelection_GlobalHiveWithBetterMatch()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Multiple hives are only supported on Windows.
                return;
            }

            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2");
        }

        [Fact]
        public void FrameworkHiveSelection_MainHiveWithBetterMatch()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Multiple hives are only supported on Windows.
                return;
            }

            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "6.0.0"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "6.1.2");
        }

        [Fact]
        public void FrameworkHiveSelection_CurrentDirectoryIsIgnored()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Multiple hives are only supported on Windows.
                return;
            }

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
                        .WithEnvironment(Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath, SharedState.DotNetGlobalHive.BinPath),
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
