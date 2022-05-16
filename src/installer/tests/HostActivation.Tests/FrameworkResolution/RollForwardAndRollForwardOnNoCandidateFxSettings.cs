// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardAndRollForwardOnNoCandidateFxSettings :
        FrameworkResolutionBase,
        IClassFixture<RollForwardAndRollForwardOnNoCandidateFxSettings.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public RollForwardAndRollForwardOnNoCandidateFxSettings(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        // Verifies that rollForward can't be used together with rollForwrdOnNoCandidateFx or applyPatches in the same runtime config
        [Theory] // rollForwardLocation                 rollForwardOnNoCandidateFxLocation  applyPatchesLocation                passes
        [InlineData(SettingLocation.RuntimeOptions,     SettingLocation.None,               SettingLocation.None,               true)]
        [InlineData(SettingLocation.RuntimeOptions,     SettingLocation.RuntimeOptions,     SettingLocation.None,               false)]
        [InlineData(SettingLocation.RuntimeOptions,     SettingLocation.FrameworkReference, SettingLocation.None,               false)]
        [InlineData(SettingLocation.RuntimeOptions,     SettingLocation.None,               SettingLocation.RuntimeOptions,     false)]
        [InlineData(SettingLocation.RuntimeOptions,     SettingLocation.None,               SettingLocation.FrameworkReference, false)]
        [InlineData(SettingLocation.RuntimeOptions,     SettingLocation.RuntimeOptions,     SettingLocation.RuntimeOptions,     false)]
        [InlineData(SettingLocation.RuntimeOptions,     SettingLocation.FrameworkReference, SettingLocation.FrameworkReference, false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.None,               SettingLocation.None,               true)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.RuntimeOptions,     SettingLocation.None,               false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.FrameworkReference, SettingLocation.None,               false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.None,               SettingLocation.RuntimeOptions,     false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.None,               SettingLocation.FrameworkReference, false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.RuntimeOptions,     SettingLocation.RuntimeOptions,     false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.FrameworkReference, SettingLocation.FrameworkReference, false)]
        public void CollisionsInRuntimeConfig(
            SettingLocation rollForwardLocation,
            SettingLocation rollForwardOnNoCandidateFxLocation,
            SettingLocation applyPatchesLocation,
            bool passes)
        {
            CommandResult result = RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "5.0.0"))
                    .With(RollForwardSetting(rollForwardLocation, Constants.RollForwardSetting.Minor))
                    .With(RollForwardOnNoCandidateFxSetting(rollForwardOnNoCandidateFxLocation, 1))
                    .With(ApplyPatchesSetting(applyPatchesLocation, false)));

            if (passes)
            {
                result.ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
            }
            else
            {
                result.Should().Fail()
                      .And.HaveStdErrContaining(
                        $"It's invalid to use both `{Constants.RollForwardSetting.RuntimeConfigPropertyName}` and one of " +
                        $"`{Constants.RollForwardOnNoCandidateFxSetting.RuntimeConfigPropertyName}` or " +
                        $"`{Constants.ApplyPatchesSetting.RuntimeConfigPropertyName}` in the same runtime config.");
            }
        }

        // Verifies that rollForward and rollForwardOnNoCandidateFx can't be used both on a command line
        [Fact]
        public void CollisionsOnCommandLine_RollForwardOnNoCandidateFx()
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .WithCommandLine(Constants.RollForwardSetting.CommandLineArgument, Constants.RollForwardSetting.LatestPatch)
                    .WithCommandLine(Constants.RollForwardOnNoCandidateFxSetting.CommandLineArgument, "2"))
                .Should().Fail()
                .And.HaveStdErrContaining(
                    $"It's invalid to use both '{Constants.RollForwardSetting.CommandLineArgument}' and " +
                    $"'{Constants.RollForwardOnNoCandidateFxSetting.CommandLineArgument}' command line options.");
        }

        // Verifies the precedence of rollFoward and rollForwardOnNoCandidateFx from various sources
        // Only checks valid cases - the precedence order should be:
        //   1. Command line
        //   2. DOTNET_ROLL_FORWARD env. variable
        //   3. .runtimeconfig.json  (only one allowed, both is invalid)
        //   4. DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX env. variable
        [Theory] // rollForwardLocation Major           rollForwardOnNoCandidateFxLocation 0   passes
        [InlineData(SettingLocation.CommandLine,        SettingLocation.FrameworkReference,    true)]
        [InlineData(SettingLocation.CommandLine,        SettingLocation.RuntimeOptions,        true)]
        [InlineData(SettingLocation.CommandLine,        SettingLocation.Environment,           true)]
        [InlineData(SettingLocation.Environment,        SettingLocation.CommandLine,           false)]
        [InlineData(SettingLocation.Environment,        SettingLocation.FrameworkReference,    true)]
        [InlineData(SettingLocation.Environment,        SettingLocation.RuntimeOptions,        true)]
        [InlineData(SettingLocation.Environment,        SettingLocation.Environment,           true)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.CommandLine,           false)]
        [InlineData(SettingLocation.FrameworkReference, SettingLocation.Environment,           true)]
        [InlineData(SettingLocation.RuntimeOptions,     SettingLocation.CommandLine,           false)]
        [InlineData(SettingLocation.RuntimeOptions,     SettingLocation.Environment,           true)]
        public void Precedence(
            SettingLocation rollForwardLocation,
            SettingLocation rollForwardOnNoCandidateFxLocation,
            bool passes)
        {
            string requestedVersion = "5.0.0";
            CommandResult result = RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, requestedVersion))
                    .With(RollForwardSetting(rollForwardLocation, Constants.RollForwardSetting.Major))
                    .With(RollForwardOnNoCandidateFxSetting(rollForwardOnNoCandidateFxLocation, 0)));

            if (passes)
            {
                result.ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
            }
            else
            {
                result.ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
            }
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
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
