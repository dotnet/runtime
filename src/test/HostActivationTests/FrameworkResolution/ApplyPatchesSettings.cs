// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
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

        [Fact]
        public void Default()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.2"),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Fact]
        public void RuntimeConfigOnly()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithApplyPatches(false)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.2"),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2"));
        }

        [Fact]
        public void FrameworkReferenceOnly()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, "5.1.2")
                        .WithApplyPatches(false)),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2"));
        }

        [Fact]
        public void Priority()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithApplyPatches(true)
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, "5.1.2")
                        .WithApplyPatches(false)),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2"));

            RunTest(
                runtimeConfig => runtimeConfig
                    .WithApplyPatches(false)
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, "5.1.2")
                        .WithApplyPatches(true)),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Fact]
        public void InnerFrameworkReference_RuntimeConfig()
        {
            using (var dotnetCustomizer = SharedState.DotNetWithFrameworks.Customize())
            {
                dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.WithApplyPatches(false));

                RunTest(
                    runtimeConfig => runtimeConfig
                        .WithFramework(MiddleWare, "2.1.0"),
                    result => result.Should().Pass()
                        .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2")
                        .And.HaveResolvedFramework(MiddleWare, "2.1.2"));
            }
        }

        [Fact]
        public void InnerFrameworkReference_Framework()
        {
            using (var dotnetCustomizer = SharedState.DotNetWithFrameworks.Customize())
            {
                dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp).WithApplyPatches(false));

                RunTest(
                    runtimeConfig => runtimeConfig
                        .WithFramework(MiddleWare, "2.1.0"),
                    result => result.Should().Pass()
                        .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.2")
                        .And.HaveResolvedFramework(MiddleWare, "2.1.2"));
            }
        }

        [Fact]
        public void NoInheritance()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithApplyPatches(false)
                    .WithFramework(MiddleWare, "2.1.2"),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));

            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MiddleWare, "2.1.2")
                        .WithApplyPatches(false)),
                result => result.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
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
                    .AddMicrosoftNETCoreAppFramework("5.1.2")
                    .AddMicrosoftNETCoreAppFramework("5.1.3")
                    .AddFramework(MiddleWare, "2.1.2", runtimeConfig =>
                        runtimeConfig.WithFramework(MicrosoftNETCoreApp, "5.1.2"))
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
