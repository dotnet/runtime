// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
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
                environment: new string[] { "DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2" });
        }

        [Fact]
        public void CommandLineOnly()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"),
                commandLine: new string[] { Constants.RollFowardOnNoCandidateFxSetting.CommandLineArgument, "2" });
        }

        [Theory]  // CLI wins over everything
        [InlineData("Environment", "5.1.3")]
        [InlineData("RuntimeConfig", "5.1.3")]
        [InlineData("Framework", "5.1.3")]
        public void CommandLinePriority(string settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                commandLine: new string[] { Constants.RollFowardOnNoCandidateFxSetting.CommandLineArgument, "2" },
                settingLocation: settingLocation,
                settingValue: 0,
                resolvedFramework: resolvedFramework);
        }

        [Theory]  // Framework loses only to CLI
        [InlineData("CommandLine", null)]
        [InlineData("Environment", "5.1.3")]
        [InlineData("RuntimeConfig", "5.1.3")]
        public void FrameworkPriority(string settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, "4.0.0")
                        .WithRollForwardOnNoCandidateFx(2)),
                settingLocation: settingLocation,
                settingValue: 0,
                resolvedFramework: resolvedFramework);
        }

        [Theory]  // Runtime config only wins over env
        [InlineData("CommandLine", null)]
        [InlineData("Environment", "5.1.3")]
        [InlineData("Framework", null)]
        public void RuntimeConfigPriority(string settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(2)
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                settingLocation: settingLocation,
                settingValue: 0,
                resolvedFramework: resolvedFramework);
        }

        [Theory]  // Env loses to everything else
        [InlineData("CommandLine", null)]
        [InlineData("RuntimeConfig", null)]
        [InlineData("Framework", null)]
        public void EnvironmentPriority(string settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                environment: new string[] { Constants.RollFowardOnNoCandidateFxSetting.EnvironmentVariable + "=2" },
                settingLocation: settingLocation,
                settingValue: 0,
                resolvedFramework: resolvedFramework);
        }

        [Theory]
        [InlineData("CommandLine", null)]   // Command line overrides everything - even inner framework references
        [InlineData("RuntimeConfig", "5.1.3")]
        [InlineData("Framework", "5.1.3")]
        [InlineData("Environment", "5.1.3")]
        public void InnerFrameworkReference(string settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MiddleWare, "2.1.0")),
                customizeDotNet: dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig
                        .WithRollForwardOnNoCandidateFx(2)
                        .GetFramework(MicrosoftNETCoreApp).Version = "4.0.0"),
                settingLocation: settingLocation,
                settingValue: 1,
                frameworkReferenceName: MiddleWare,
                resolvedFramework: resolvedFramework);
        }

        [Theory]
        [InlineData("CommandLine", "5.1.3")]   // Command line overrides everything - even inner framework references
        [InlineData("RuntimeConfig", null)]    // RuntimeConfig and Framework settings are not inherited to inner reference
        [InlineData("Framework", null)]        // RuntimeConfig and Framework settings are not inherited to inner reference
        [InlineData("Environment", "5.1.3")]   // Since none is specified for the inner reference, environment is used
        public void NoInheritance_MoreRelaxed(string settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MiddleWare, "1.0.0")),
                customizeDotNet: dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig
                        .GetFramework(MicrosoftNETCoreApp).Version = "4.0.0"),
                settingLocation: settingLocation,
                settingValue: 2,
                frameworkReferenceName: MiddleWare,
                resolvedFramework: resolvedFramework);
        }

        [Theory]
        [InlineData("CommandLine", null)]      // Command line overrides everything - even inner framework references
        [InlineData("RuntimeConfig", "5.1.3")] // RuntimeConfig and Framework settings are not inherited to inner reference
        [InlineData("Framework", "5.1.3")]     // RuntimeConfig and Framework settings are not inherited to inner reference
        [InlineData("Environment", null)]      // Since none is specified for the inner reference, environment is used
        public void NoInheritance_MoreRestrictive(string settingLocation, string resolvedFramework)
        {
            RunTestWithRollForwardOnNoCandidateFxSetting(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MiddleWare, "2.1.2")),
                customizeDotNet: dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig
                        .GetFramework(MicrosoftNETCoreApp).Version = "5.0.0"),
                settingLocation: settingLocation,
                settingValue: 0,
                frameworkReferenceName: MiddleWare,
                resolvedFramework: resolvedFramework);
        }


        private void RunTestWithRollForwardOnNoCandidateFxSetting(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<DotNetCliExtensions.DotNetCliCustomizer> customizeDotNet = null,
            string[] environment = null,
            string[] commandLine = null,
            string settingLocation = null,
            int settingValue = 1,
            string frameworkReferenceName = MicrosoftNETCoreApp,
            string resolvedFramework = null)
        {
            using (DotNetCliExtensions.DotNetCliCustomizer dotnetCustomizer = SharedState.DotNetWithFrameworks.Customize())
            {
                customizeDotNet?.Invoke(dotnetCustomizer);

                Func<RuntimeConfig, RuntimeConfig> runtimeConfigCustomization = runtimeConfig;
                switch (settingLocation)
                {
                    case "Environment":
                        environment = new string[] { $"{Constants.RollFowardOnNoCandidateFxSetting.EnvironmentVariable}={settingValue}" };
                        break;
                    case "CommandLine":
                        commandLine = new string[] { Constants.RollFowardOnNoCandidateFxSetting.CommandLineArgument, settingValue.ToString() };
                        break;
                    case "RuntimeConfig":
                        runtimeConfigCustomization = rc => runtimeConfig(rc).WithRollForwardOnNoCandidateFx(settingValue);
                        break;
                    case "Framework":
                        runtimeConfigCustomization = rc =>
                        {
                            runtimeConfig(rc).GetFramework(frameworkReferenceName).WithRollForwardOnNoCandidateFx(settingValue);
                            return rc;
                        };
                        break;
                }

                RunTest(
                    runtimeConfigCustomization,
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
                    environment,
                    commandLine);
            }
        }

        private void RunTest(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction,
            string[] environment = null,
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
