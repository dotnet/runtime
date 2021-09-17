// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
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

        // Validates that --fx-version <fxVersion> overrides framework version specified
        // in the runtime config in the framework reference <fxRefVer>
        [Theory] // fxRefVer fxVersion         resolvedFramework
        [InlineData("1.0.0", "2.5.5",          "2.5.5")]
        [InlineData("2.0.0", "2.5.5",          "2.5.5")]
        [InlineData("2.5.4", "2.5.5",          "2.5.5")]
        [InlineData("2.5.5", "2.5.5",          "2.5.5")]
        [InlineData("2.5.5", "2.5.4",          "2.5.4")]
        [InlineData("2.5.5", "2.5.3",          ResolvedFramework.NotFound)]
        [InlineData("2.5.5", "2.5.5-preview1", ResolvedFramework.NotFound)]
        public void OverridesFrameworkReferences(string frameworkReferenceVersion, string fxVersion, string resolvedFramework)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, frameworkReferenceVersion))
                    .WithCommandLine(Constants.FxVersion.CommandLineArgument, fxVersion))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Validates that --fx-version ignores any <rollForwardOnNoCandidateFx> or <applyPatches> settings
        [Theory] // rollForwardOnNoCandidateFx applyPatches
        [InlineData(null,                      null )]
        [InlineData(0,                         null )]
        [InlineData(0,                         true )]
        [InlineData(1,                         null )]
        [InlineData(1,                         true )]
        [InlineData(2,                         null )]
        [InlineData(0,                         false)]
        public void IgnoresRollForwardOnNoCandidateFxAndApplyPatchesSettings(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "2.5.4"))
                    .WithCommandLine(Constants.FxVersion.CommandLineArgument, "2.5.5")
                    .With(RollForwardOnNoCandidateFxSetting(SettingLocation.CommandLine, rollForwardOnNoCandidateFx))
                    .With(ApplyPatchesSetting(SettingLocation.RuntimeOptions, applyPatches)))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "2.5.5");
        }

        // Validates that --fx-version ignores any rollForward <rollForward> settings
        [Theory] // rollForward
        [InlineData(null                                    )]
        [InlineData(Constants.RollForwardSetting.Disable    )]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor      )]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        [InlineData(Constants.RollForwardSetting.Major      )]
        [InlineData(Constants.RollForwardSetting.LatestMajor)]
        public void IgnoresRollForwardSettings(string rollForward)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "2.5.4"))
                    .WithCommandLine(Constants.FxVersion.CommandLineArgument, "2.5.5")
                    .With(RollForwardSetting(SettingLocation.CommandLine, rollForward)))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "2.5.5");
        }

        // Validates that --fx-version only applies to the first framework reference
        [Fact]
        public void AppliesToFirstFrameworkReference_NETCoreAppFirst()
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "1.0.0")
                        .WithFramework(MiddleWare, "2.1.2"))
                    .WithCommandLine(Constants.FxVersion.CommandLineArgument, "2.5.5"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "2.5.5");
        }

        // Validates that --fx-version only applies to the first framework reference
        [Fact]
        public void AppliesToFirstFrameworkReference_MiddleWareFirst()
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MiddleWare, "1.0.0")
                        .WithFramework(MicrosoftNETCoreApp, "2.5.0"))
                    .WithCommandLine(Constants.FxVersion.CommandLineArgument, "2.1.2"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "2.5.5");
        }

        private CommandResult RunTest(TestSettings testSettings) =>
            RunTest(SharedState.DotNetWithFrameworks, SharedState.FrameworkReferenceApp, testSettings);

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithFrameworks { get; }

            public SharedTestState()
            {
                DotNetWithFrameworks = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("2.5.4")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("2.5.5")
                    .AddFramework(
                        MiddleWare, "2.1.2",
                        runtimeConfig => runtimeConfig.WithFramework(MicrosoftNETCoreApp, "2.5.5"))
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
