// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardOnNoCandidateFxSettings :
        FrameworkResolutionBase,
        IClassFixture<RollForwardOnNoCandidateFxSettings.SharedTestState>
    {
        private const string MiddleWare = "MiddleWare";

        private SharedTestState SharedState { get; }

        public RollForwardOnNoCandidateFxSettings(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        // Verifies the default behavior is 1 (Minor)
        [Fact]
        public void Default()
        {
            string requestedVersion = "4.0.0";
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, requestedVersion)))
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);

            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "5.0.0")))
                .Should().Pass()
                .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
        }

        // Verifies that it works in all supported locations
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
                    .With(RollForwardOnNoCandidateFxSetting(location, 2)))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
        }

        // Verifies that CLI setting wins over any other <settingLocation>
        [Theory] // settingLocation                     commandLineWins
        [InlineData(SettingLocation.Environment,        true)]
        [InlineData(SettingLocation.RuntimeOptions,     true)]
        [InlineData(SettingLocation.FrameworkReference, true)]
        public void CommandLinePriority(SettingLocation settingLocation, bool commandLineWins)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 0))
                    .WithCommandLine(Constants.RollForwardOnNoCandidateFxSetting.CommandLineArgument, "2"))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, commandLineWins ? "5.1.3" : null);
        }

        // Verifies that framework reference setting loses only to CLI <settingLocation>
        [Theory] // settingLocation                 frameworkReferenceWins
        [InlineData(SettingLocation.CommandLine,    false)]
        [InlineData(SettingLocation.Environment,    true)]
        [InlineData(SettingLocation.RuntimeOptions, true)]
        public void FrameworkReferencePriority(SettingLocation settingLocation, bool frameworkReferenceWins)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, "4.0.0")
                            .WithRollForwardOnNoCandidateFx(2)))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 0)))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, frameworkReferenceWins ? "5.1.3" : null);
        }

        // Verifies that runtime options setting only wins over env. variable <settingLocation>
        [Theory] // settingLocation                     runtimeOptionWins
        [InlineData(SettingLocation.CommandLine,        false)]
        [InlineData(SettingLocation.Environment,        true)]
        [InlineData(SettingLocation.FrameworkReference, false)]
        public void RuntimeOptionsPriority(SettingLocation settingLocation, bool runtimeOptionWins)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithRollForwardOnNoCandidateFx(2)
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 0)))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, runtimeOptionWins ? "5.1.3" : null);
        }

        // Verifies that env. variable loses to any other <settingLocation>
        [Theory] // settingLocation                     envVariableWins
        [InlineData(SettingLocation.CommandLine,        false)]
        [InlineData(SettingLocation.RuntimeOptions,     false)]
        [InlineData(SettingLocation.FrameworkReference, false)]
        public void EnvironmentPriority(SettingLocation settingLocation, bool envVariableWins)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 0))
                    .WithEnvironment(Constants.RollForwardOnNoCandidateFxSetting.EnvironmentVariable, "2"))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, envVariableWins ? "5.1.3" : null);
        }

        // Verifies interaction between variour <settingLocation> and inner framework reference setting
        [Theory] // settingLocation                     innerReferenceWins
        // Command line overrides everything - even inner framework references
        [InlineData(SettingLocation.CommandLine,        false)]
        [InlineData(SettingLocation.RuntimeOptions,     true)]
        [InlineData(SettingLocation.FrameworkReference, true)]
        [InlineData(SettingLocation.Environment,        true)]
        public void InnerFrameworkReference(SettingLocation settingLocation, bool innerReferenceWins)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(new RuntimeConfig.Framework(MiddleWare, "2.1.0")))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 1, MiddleWare))
                    .WithDotnetCustomizer(dotnetCustomizer => dotnetCustomizer
                        .Framework(MiddleWare).RuntimeConfig(runtimeConfig => runtimeConfig
                            .WithRollForwardOnNoCandidateFx(2)
                            .GetFramework(MicrosoftNETCoreApp).Version = "4.0.0")))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, innerReferenceWins ? "5.1.3" : null);
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
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 2, MiddleWare))
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
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 0, MiddleWare))
                    .WithDotnetCustomizer(dotnetCustomizer => dotnetCustomizer
                        .Framework(MiddleWare).RuntimeConfig(runtimeConfig => runtimeConfig
                            .GetFramework(MicrosoftNETCoreApp).Version = "5.0.0")))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, appWins ? null : "5.1.3");
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
