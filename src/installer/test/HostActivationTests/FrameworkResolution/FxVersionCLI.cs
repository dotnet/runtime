// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class FXVersionCLI :
        FrameworkResolutionBase,
        IClassFixture<FXVersionCLI.SharedTestState>
    {
        private const string MiddleWare = "MiddleWare";

        private SharedTestState SharedState { get; }

        public FXVersionCLI(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Theory]
        [InlineData("1.0.0", "2.5.5", "2.5.5")]
        [InlineData("2.0.0", "2.5.5", "2.5.5")]
        [InlineData("2.5.4", "2.5.5", "2.5.5")]
        [InlineData("2.5.5", "2.5.5", "2.5.5")]
        [InlineData("2.5.5", "2.5.4", "2.5.4")]
        [InlineData("2.5.5", "2.5.3", null)]
        [InlineData("2.5.5", "2.5.5-preview1", null)]
        public void OverridesFrameworkReferences(string frameworkReferenceVersion, string fxVersion, string resolvedFramework)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, frameworkReferenceVersion))
                    .WithCommandLine(Constants.FxVersion.CommandLineArgument, fxVersion),
                resolvedFramework: resolvedFramework);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(0, null)]
        [InlineData(0, true)]
        [InlineData(1, null)]
        [InlineData(1, true)]
        [InlineData(2, null)]
        [InlineData(0, false)]
        public void IgnoresOtherSettings(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "2.5.4"))
                    .WithCommandLine(Constants.FxVersion.CommandLineArgument, "2.5.5")
                    .With(RollForwardOnNoCandidateFxSetting(SettingLocation.CommandLine, rollForwardOnNoCandidateFx))
                    .With(ApplyPatchesSetting(SettingLocation.RuntimeOptions, applyPatches)),
                resolvedFramework: "2.5.5");
        }

        [Fact]
        public void AppliesToFirstFrameworkReference_NETCoreAppFirst()
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "1.0.0")
                        .WithFramework(MiddleWare, "2.1.2"))
                    .WithCommandLine(Constants.FxVersion.CommandLineArgument, "2.5.5"),
                resolvedFramework: "2.5.5");
        }

        [Fact]
        public void AppliesToFirstFrameworkReference_MiddleWareFirst()
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MiddleWare, "1.0.0")
                        .WithFramework(MicrosoftNETCoreApp, "2.5.0"))
                    .WithCommandLine(Constants.FxVersion.CommandLineArgument, "2.1.2"),
                resolvedFramework: "2.5.5");
        }

        private void RunTest(
            TestSettings testSettings,
            string resolvedFramework = null)
        {
            RunTest(
                SharedState.DotNetWithFrameworks,
                SharedState.FrameworkReferenceApp,
                testSettings,
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
                });
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithFrameworks { get; }

            public SharedTestState()
            {
                DotNetWithFrameworks = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFramework("2.5.4")
                    .AddMicrosoftNETCoreAppFramework("2.5.5")
                    .AddFramework(
                        MiddleWare, "2.1.2",
                        runtimeConfig => runtimeConfig.WithFramework(MicrosoftNETCoreApp, "2.5.5"))
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
