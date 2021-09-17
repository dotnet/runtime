// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class IncludedFrameworksSettings :
        FrameworkResolutionBase,
        IClassFixture<IncludedFrameworksSettings.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public IncludedFrameworksSettings(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Fact]
        public void FrameworkAndIncludedFrameworksIsInvalid()
        {
            RunFrameworkDependentTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "5.1.2")
                        .WithIncludedFramework(MicrosoftNETCoreApp, "5.1.2")))
                .Should().Fail()
                .And.HaveStdErrContaining("It's invalid to specify both `framework`/`frameworks` and `includedFrameworks` properties.");
        }

        [Fact]
        public void SelfContainedCanHaveIncludedFrameworks()
        {
            RunSelfContainedTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithIncludedFramework(MicrosoftNETCoreApp, "5.1.2")))
                .Should().Pass()
                .And.HaveStdOutContaining("mock is_framework_dependent: 0");
        }

        [Fact]
        public void IncludedFrameworkMustSpecifyName()
        {
            RunSelfContainedTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithIncludedFramework(null, "5.1.2")))
                .Should().Fail()
                .And.HaveStdErrContaining("No framework name specified.");
        }

        [Fact]
        public void OtherPropertiesAreIgnoredOnIncludedFramework()
        {
            RunSelfContainedTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithIncludedFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, "5.1.2")
                            .WithApplyPatches(false)    // Properties which are otherwise parsed on frameworks are ignored
                            .WithRollForward("invalid") // in case of included frameworks. (so invalid values will be accepted)
                            .WithRollForwardOnNoCandidateFx(42))))
                .Should().Pass()
                .And.HaveStdOutContaining("mock is_framework_dependent: 0");
        }

        private CommandResult RunFrameworkDependentTest(TestSettings testSettings) =>
            RunTest(SharedState.DotNetWithFrameworks, SharedState.FrameworkReferenceApp, testSettings);

        private CommandResult RunSelfContainedTest(TestSettings testSettings) =>
            RunSelfContainedTest(SharedState.SelfContainedApp, testSettings);

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public TestApp SelfContainedApp { get; }

            public DotNetCli DotNetWithFrameworks { get; }

            public SharedTestState()
            {
                DotNetWithFrameworks = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.2")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
                SelfContainedApp = CreateSelfContainedAppWithMockHostPolicy();
            }
        }
    }
}
