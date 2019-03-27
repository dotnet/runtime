// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
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
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                "5.1.2");
        }

        [Fact]
        public void FrameworkHiveSelection_MainHiveWithBetterMatch()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "6.0.0"),
                "6.1.2");
        }

        private void RunTest(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            string resolvedFramework = null)
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
                    .WithRuntimeConfigCustomizer(runtimeConfig)
                    .WithEnvironment(Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath, SharedState.DotNetGlobalHive.BinPath),
                commandResult =>
                {
                    if (resolvedFramework != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                },
                // Must enable multi-level lookup otherwise multiple hives are not enabled
                multiLevelLookup: true);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetMainHive { get; }

            public DotNetCli DotNetGlobalHive { get; }

            public SharedTestState()
            {
                DotNetMainHive = DotNet("MainHive")
                    .AddMicrosoftNETCoreAppFramework("5.2.0")
                    .AddMicrosoftNETCoreAppFramework("6.1.2")
                    .Build();

                DotNetGlobalHive = DotNet("GlobalHive")
                    .AddMicrosoftNETCoreAppFramework("5.1.2")
                    .AddMicrosoftNETCoreAppFramework("6.2.0")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
