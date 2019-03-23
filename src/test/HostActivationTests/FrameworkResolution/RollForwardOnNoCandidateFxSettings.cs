// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Collections.Generic;
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

        [Fact]
        public void Default()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                result => result.Should().Fail()
                    .And.DidNotFindCompatibleFrameworkVersion());

            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Fact]
        public void RuntimeConfigOnly()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(2)
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Fact]
        public void FrameworkReferenceOnly()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, "4.0.0")
                        .WithRollForwardOnNoCandidateFx(2)),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Fact]
        public void EnvironmentVariableOnly()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"),
                environment: new Dictionary<string, string>() { { "DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "2" } });
        }

        [Fact]
        public void CommandLineOnly()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"),
                commandLine: new string[] { Constants.RollForwardOnNoCandidateFxSetting.CommandLineArgument, "2" });
        }

        [Theory]  // CLI wins over everything
        [InlineData(SettingLocation.Environment, "5.1.3")]
        [InlineData(SettingLocation.RuntimeOptions, "5.1.3")]
        [InlineData(SettingLocation.FrameworkReference, "5.1.3")]
        public void CommandLinePriority(SettingLocation settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 0))
                    .WithCommandLine(Constants.RollForwardOnNoCandidateFxSetting.CommandLineArgument, "2"),
                resolvedFramework: resolvedFramework);
        }

        [Theory]  // Framework loses only to CLI
        [InlineData(SettingLocation.CommandLine, null)]
        [InlineData(SettingLocation.Environment, "5.1.3")]
        [InlineData(SettingLocation.RuntimeOptions, "5.1.3")]
        public void FrameworkPriority(SettingLocation settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, "4.0.0")
                            .WithRollForwardOnNoCandidateFx(2)))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 0)),
                resolvedFramework: resolvedFramework);
        }

        [Theory]  // Runtime config only wins over env
        [InlineData(SettingLocation.CommandLine, null)]
        [InlineData(SettingLocation.Environment, "5.1.3")]
        [InlineData(SettingLocation.FrameworkReference, null)]
        public void RuntimeConfigPriority(SettingLocation settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithRollForwardOnNoCandidateFx(2)
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 0)),
                resolvedFramework: resolvedFramework);
        }

        [Theory]  // Env loses to everything else
        [InlineData(SettingLocation.CommandLine, null)]
        [InlineData(SettingLocation.RuntimeOptions, null)]
        [InlineData(SettingLocation.FrameworkReference, null)]
        public void EnvironmentPriority(SettingLocation settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 0))
                    .WithEnvironment(Constants.RollForwardOnNoCandidateFxSetting.EnvironmentVariable, "2"),
                resolvedFramework: resolvedFramework);
        }

        [Theory]
        [InlineData(SettingLocation.CommandLine, null)]   // Command line overrides everything - even inner framework references
        [InlineData(SettingLocation.RuntimeOptions, "5.1.3")]
        [InlineData(SettingLocation.FrameworkReference, "5.1.3")]
        [InlineData(SettingLocation.Environment, "5.1.3")]
        public void InnerFrameworkReference(SettingLocation settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(new RuntimeConfig.Framework(MiddleWare, "2.1.0")))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 1, MiddleWare)),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig
                        .WithRollForwardOnNoCandidateFx(2)
                        .GetFramework(MicrosoftNETCoreApp).Version = "4.0.0"),
                resolvedFramework);
        }

        [Theory]
        [InlineData(SettingLocation.CommandLine, "5.1.3")]     // Command line overrides everything - even inner framework references
        [InlineData(SettingLocation.RuntimeOptions, null)]     // RuntimeOptions and FrameworkReference settings are not inherited to inner reference
        [InlineData(SettingLocation.FrameworkReference, null)] // RuntimeOptions and FrameworkReference settings are not inherited to inner reference
        [InlineData(SettingLocation.Environment, "5.1.3")]     // Since none is specified for the inner reference, environment is used
        public void NoInheritance_MoreRelaxed(SettingLocation settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MiddleWare, "1.0.0"))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 2, MiddleWare)),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig
                        .GetFramework(MicrosoftNETCoreApp).Version = "4.0.0"),
                resolvedFramework);
        }

        [Theory]
        [InlineData(SettingLocation.CommandLine, null)]           // Command line overrides everything - even inner framework references
        [InlineData(SettingLocation.RuntimeOptions, "5.1.3")]     // RuntimeOptions and FrameworkReference settings are not inherited to inner reference
        [InlineData(SettingLocation.FrameworkReference, "5.1.3")] // RuntimeOptions and FrameworkReference settings are not inherited to inner reference
        [InlineData(SettingLocation.Environment, null)]           // Since none is specified for the inner reference, environment is used
        public void NoInheritance_MoreRestrictive(SettingLocation settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(new RuntimeConfig.Framework(MiddleWare, "2.1.2")))
                    .With(RollForwardOnNoCandidateFxSetting(settingLocation, 0, MiddleWare)),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig
                        .GetFramework(MicrosoftNETCoreApp).Version = "5.0.0"),
                resolvedFramework);
        }

        private void RunTestWithRollForwardOnNoCandidateFxSetting(
            TestSettings testSettings,
            Action<DotNetCliExtensions.DotNetCliCustomizer> customizeDotNet = null,
            string resolvedFramework = null)
        {
            using (DotNetCliExtensions.DotNetCliCustomizer dotnetCustomizer = SharedState.DotNetWithFrameworks.Customize())
            {
                customizeDotNet?.Invoke(dotnetCustomizer);

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
        }

        private void RunTest(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction,
            IDictionary<string, string> environment = null,
            string[] commandLine = null)
        {
            RunTest(
                SharedState.DotNetWithFrameworks,
                SharedState.FrameworkReferenceApp,
                runtimeConfig,
                resultAction,
                environment,
                commandLine);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithFrameworks { get; }

            public SharedTestState()
            {
                DotNetWithFrameworks = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFramework("5.1.3")
                    .AddFramework(
                        MiddleWare, "2.1.2", 
                        runtimeConfig => runtimeConfig.WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
