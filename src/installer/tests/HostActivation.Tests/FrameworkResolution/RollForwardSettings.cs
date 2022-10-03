// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardSettings :
        FrameworkResolutionBase,
        IClassFixture<RollForwardSettings.SharedTestState>
    {
        private const string MiddleWare = "MiddleWare";

        private SharedTestState SharedState { get; }

        public RollForwardSettings(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        // Verifies that default behavior is Minor
        [Fact]
        public void Default()
        {
            string requestedVersion = "4.0.0";
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0")))
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);

            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "5.0.0")))
                .Should().Pass()
                .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
        }

        // Verifies that invalid values is checked in all settings locations
        [Theory]
        [InlineData(SettingLocation.CommandLine)]
        [InlineData(SettingLocation.Environment)]
        [InlineData(SettingLocation.RuntimeOptions)]
        [InlineData(SettingLocation.FrameworkReference)]
        public void InvalidValue(SettingLocation settingLocation)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .With(RollForwardSetting(settingLocation, "InvalidValue")))
                .Should().Fail()
                .And.DidNotRecognizeRollForwardValue("InvalidValue");
        }

        // Verifies that the value ignores casing on command line
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        [InlineData(Constants.RollForwardSetting.Major)]
        [InlineData(Constants.RollForwardSetting.LatestMajor)]
        public void ValueIgnoresCase_CommandLine(string rollForward)
        {
            ValidateValueIgnoresCase(SettingLocation.CommandLine, rollForward);
        }

        // Verifies that the value ignores casing in env. variable
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        [InlineData(Constants.RollForwardSetting.Major)]
        [InlineData(Constants.RollForwardSetting.LatestMajor)]
        public void ValueIgnoresCase_Environment(string rollForward)
        {
            ValidateValueIgnoresCase(SettingLocation.Environment, rollForward);
        }

        // Verifies that the value ignores casing in the runtime options
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        [InlineData(Constants.RollForwardSetting.Major)]
        [InlineData(Constants.RollForwardSetting.LatestMajor)]
        public void ValueIgnoresCase_RuntimeOptions(string rollForward)
        {
            ValidateValueIgnoresCase(SettingLocation.RuntimeOptions, rollForward);
        }

        // Verifies that the value ignores casing in the framework reference
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        [InlineData(Constants.RollForwardSetting.Major)]
        [InlineData(Constants.RollForwardSetting.LatestMajor)]
        public void ValueIgnoresCase_FrameworkReference(string rollForward)
        {
            ValidateValueIgnoresCase(SettingLocation.FrameworkReference, rollForward);
        }

        private void ValidateValueIgnoresCase(SettingLocation settingLocation, string rollForward)
        {
            string[] values = new string[]
            {
                rollForward,
                rollForward.ToLowerInvariant(),
                rollForward.ToUpperInvariant()
            };

            foreach (string value in values)
            {
                RunTest(
                    new TestSettings()
                        .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                            .WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                        .With(RollForwardSetting(settingLocation, value)))
                    .Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
            }
        }

        // Verifies that there's no inheritance between app and framework when applying more relaxed setting in the app
        [Theory] // settingLocation                     appWins
        // Command line overrides everything - even inner framework references
        [InlineData(SettingLocation.CommandLine,        true)]
        // RuntimeOptions and FrameworkReference settings are not inherited to inner reference
        [InlineData(SettingLocation.RuntimeOptions,     false)]
        // RuntimeOptions and FrameworkReference settings are not inherited to inner reference
        [InlineData(SettingLocation.FrameworkReference, false)]
        // Since none is specified for the inner reference, environment is used
        [InlineData(SettingLocation.Environment,        true)]
        public void NoInheritance_MoreRelaxed(SettingLocation settingLocation, bool appWins)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MiddleWare, "1.0.0"))
                    .With(RollForwardSetting(settingLocation, Constants.RollForwardSetting.Major, MiddleWare))
                    .WithDotnetCustomizer(dotnetCustomizer => dotnetCustomizer
                        .Framework(MiddleWare).RuntimeConfig(runtimeConfig => runtimeConfig
                            .GetFramework(MicrosoftNETCoreApp).Version = "4.0.0")))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, appWins ? "5.1.3" : null);
        }

        // Verifies that there's no inheritance between app and framework when applying more strict setting in the app
        [Theory] // settingLocation                     appWins
        // Command line overrides everything - even inner framework references
        [InlineData(SettingLocation.CommandLine,        true)]
        // RuntimeOptions and FrameworkReference settings are not inherited to inner reference
        [InlineData(SettingLocation.RuntimeOptions,     false)]
        // RuntimeOptions and FrameworkReference settings are not inherited to inner reference
        [InlineData(SettingLocation.FrameworkReference, false)]
        // Since none is specified for the inner reference, environment is used
        [InlineData(SettingLocation.Environment,        true)]
        public void NoInheritance_MoreRestrictive(SettingLocation settingLocation, bool appWins)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(new RuntimeConfig.Framework(MiddleWare, "2.1.2")))
                    .With(RollForwardSetting(settingLocation, Constants.RollForwardSetting.LatestPatch, MiddleWare))
                    .WithDotnetCustomizer(dotnetCustomizer => dotnetCustomizer
                        .Framework(MiddleWare).RuntimeConfig(runtimeConfig => runtimeConfig
                            .GetFramework(MicrosoftNETCoreApp).Version = "5.0.0")))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, appWins ? null : "5.1.3");
        }

        // Verifies that the setting works in all supported locations
        [Theory]
        [InlineData(SettingLocation.CommandLine)]
        [InlineData(SettingLocation.Environment)]
        [InlineData(SettingLocation.RuntimeOptions)]
        [InlineData(SettingLocation.FrameworkReference)]
        public void AllLocations(SettingLocation location)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .With(RollForwardSetting(location, Constants.RollForwardSetting.Major)))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
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
                    .AddFramework(
                        MiddleWare, "2.1.2",
                        runtimeConfig => runtimeConfig.WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
