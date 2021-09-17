// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class ApplyPatchesSettings :
        FrameworkResolutionBase,
        IClassFixture<ApplyPatchesSettings.SharedTestState>
    {
        private const string MiddleWare = "MiddleWare";

        private SharedTestState SharedState { get; }

        public ApplyPatchesSettings(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        // Verifies that the default is true
        [Fact]
        public void Default()
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "5.1.2")))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
        }

        // Verifies that it works in all supported locations
        [Theory]
        [InlineData(SettingLocation.RuntimeOptions)]
        [InlineData(SettingLocation.FrameworkReference)]
        public void AllLocations(SettingLocation location)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, "5.1.2"))
                    .With(ApplyPatchesSetting(location, false)))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2");
        }

        // Verifies that framework reference setting wins over runtime options one
        [Fact]
        public void Priority()
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithApplyPatches(true)
                        .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, "5.1.2")
                            .WithApplyPatches(false))))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2");

            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithApplyPatches(false)
                        .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, "5.1.2")
                            .WithApplyPatches(true))))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
        }

        // Verifies that it works on inner framework references in runtime options
        [Fact]
        public void InnerFrameworkReference_RuntimeOptions()
        {
            using (var dotnetCustomizer = SharedState.DotNetWithFrameworks.Customize())
            {
                dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.WithApplyPatches(false));

                RunTest(
                    new TestSettings()
                        .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                            .WithFramework(MiddleWare, "2.1.0")))
                    .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2")
                    .And.HaveResolvedFramework(MiddleWare, "2.1.2");
            }
        }

        // Verifies that it works on inner framework references in framework reference
        [Fact]
        public void InnerFrameworkReference_Framework()
        {
            using (var dotnetCustomizer = SharedState.DotNetWithFrameworks.Customize())
            {
                dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp).WithApplyPatches(false));

                RunTest(
                    new TestSettings()
                        .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                            .WithFramework(MiddleWare, "2.1.0")))
                    .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2")
                    .And.HaveResolvedFramework(MiddleWare, "2.1.2");
            }
        }

        // Verifies that the setting is not inherited between frameworks
        [Theory]
        [InlineData(SettingLocation.RuntimeOptions)]
        [InlineData(SettingLocation.FrameworkReference)]
        public void NoInheritance(SettingLocation settingLocation)
        {
            RunTest(
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MiddleWare, "2.1.2"))
                    .With(ApplyPatchesSetting(settingLocation, false, MiddleWare)))
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
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3")
                    .AddFramework(MiddleWare, "2.1.2", runtimeConfig =>
                        runtimeConfig.WithFramework(MicrosoftNETCoreApp, "5.1.2"))
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
