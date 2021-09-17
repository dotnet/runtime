// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class ComplexHierarchies :
        FrameworkResolutionBase,
        IClassFixture<ComplexHierarchies.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public ComplexHierarchies(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithMultipleFrameworks { get; }

            public SharedTestState()
            {
                DotNetWithMultipleFrameworks = DotNet("DotNetWithMultipleFrameworks")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.1")
                    .AddFramework("MiddleWare", "2.1.2", runtimeConfig =>
                        runtimeConfig.WithFramework(MicrosoftNETCoreApp, "5.1.1"))
                    .AddFramework("SerializerWare", "3.0.1", runtimeConfig =>
                        runtimeConfig
                            .WithFramework(MicrosoftNETCoreApp, "5.1.0")
                            .WithFramework("MiddleWare", "2.1.0"))
                    .AddFramework("OMWare", "7.3.1", runtimeConfig =>
                        runtimeConfig
                            .WithFramework(MicrosoftNETCoreApp, "5.1.0")
                            .WithFramework("MiddleWare", "2.1.0"))
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }

        [Fact]
        public void TwoAppFrameworksOnTopOfMiddleWare()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0")
                    .WithFramework("MiddleWare", "2.1.0")
                    .WithFramework("SerializerWare", "3.0.1")
                    .WithFramework("OMWare", "7.3.1"))
                // https://github.com/dotnet/runtime/issues/71027
                // This should pass just fine and resolve all frameworks correctly
                // Currently it fails because it does resolve frameworks, but incorrectly
                // looks for hostpolicy in MiddleWare, instead of Microsoft.NETCore.App.
                .Should().Fail().And.HaveStdErrContaining("hostpolicy");
                //.ShouldHaveResolvedFramework(
                //    MicrosoftNETCoreApp, "5.1.1")
                //.And.HaveResolvedFramework("MiddleWare", "2.1.")
                //.And.HaveResolvedFramework("SerializerWare", "3.0.1")
                //.And.HaveResolvedFramework("OMWare", "7.3.1");
        }

        private CommandResult RunTest(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<DotNetCliExtensions.DotNetCliCustomizer> customizeDotNet = null,
            bool rollForwardToPreRelease = false)
        {
            return RunTest(
                SharedState.DotNetWithMultipleFrameworks,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig)
                    .WithDotnetCustomizer(customizeDotNet)
                    .WithEnvironment(Constants.RollForwardToPreRelease.EnvironmentVariable, rollForwardToPreRelease ? "1" : "0"));
        }
    }
}
