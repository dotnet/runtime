// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class MultipleFrameworks :
        FrameworkResolutionBase,
        IClassFixture<MultipleFrameworks.SharedTestState>
    {
        private const string MiddleWare = "MiddleWare";
        private const string AnotherMiddleWare = "AnotherMiddleWare";
        private const string HighWare = "HighWare";

        private SharedTestState SharedState { get; }

        public MultipleFrameworks(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        // Soft roll forward from app's 5.1.1 (defaults) to inner framework reference with [specified version]
        [Theory]
        [InlineData("5.0.0", 0,    null,  null)]
        [InlineData("5.1.0", 0,    null,  "5.1.3")]
        [InlineData("5.1.0", 0,    false, null)]
        [InlineData("5.1.1", 0,    false, "5.1.1")]
        [InlineData("5.0.0", null, null,  "5.1.3")]
        [InlineData("5.0.0", 1,    null,  "5.1.3")]
        [InlineData("1.0.0", 1,    null,  null)]
        [InlineData("1.0.0", 2,    null,  "5.1.3")]
        public void SoftRollForward_InnerFrameworkReference_ToLower(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)
                        .Version = versionReference),
                resolvedFramework: resolvedFramework,
                resultValidator: resolvedFramework != null ? null : new Action<CommandResult>(
                    commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, versionReference, "5.1.1")));
        }

        // Soft roll forward from app's 5.1.1 (defaults) to inner framework reference with [specified version]
        // In this case the direct reference from app is first, so the framework reference from app
        // is actually resolved against the disk - and the resolved framework is than compared to
        // the inner framework reference .
        [Theory]
        [InlineData("5.0.0", 0, null, null)]
        [InlineData("5.1.0", 0, null, "5.1.3")]
        [InlineData("5.1.0", 0, false, null)]
        [InlineData("5.1.3", 0, false, "5.1.3")]
        [InlineData("5.0.0", null, null, "5.1.3")]
        [InlineData("5.0.0", 1, null, "5.1.3")]
        [InlineData("1.0.0", 1, null, null)]
        [InlineData("1.0.0", 2, null, "5.1.3")]
        public void SoftRollForward_InnerFrameworkReference_ToLower_HardResolve(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)
                        .Version = versionReference),
                resolvedFramework: resolvedFramework,
                resultValidator: resolvedFramework != null ? null : new Action<CommandResult>(
                    commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, versionReference, "5.1.3")));
        }

        // Soft roll forward from app's 5.1.1 (defaults) to inner framework reference with [specified version]
        [Theory]
        [InlineData("5.4.0", null, null, "5.4.1")]
        [InlineData("6.0.0", null, null, null)]
        public void SoftRollForward_InnerFrameworkReference_ToHigher(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)
                        .Version = versionReference),
                resolvedFramework: resolvedFramework,
                resultValidator: resolvedFramework != null ? null : new Action<CommandResult>(
                    commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, "5.1.1", versionReference)));
        }

        // Soft roll forward from app's 5.1.1 (defaults) to inner framework reference with [specified version]
        // In this case the app reference to core framework comes first, which means it's going to be hard resolved
        // and only then the soft roll forward to the inner reference is performed. So the hard resolved version
        // is use in the soft roll forward.
        [Theory]
        [InlineData("5.4.0", null, null, "5.4.1")]
        [InlineData("6.0.0", null, null, null)]
        public void SoftRollForward_InnerFrameworkReference_ToHigher_HardResolve(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)
                        .Version = versionReference),
                resolvedFramework: resolvedFramework,
                resultValidator: resolvedFramework != null ? null : new Action<CommandResult>(
                    commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, "5.1.3", versionReference)));
        }

        // Soft roll forward from app's 5.1.1 (defaults) to inner framework reference with [specified version]
        [Theory]
        [InlineData("6.0.0", null, null)]    // Can't roll forward from release to pre-release
        [InlineData("6.0.1-preview.0", null, "6.0.1-preview.1")]
        public void SoftRollForward_InnerFrameworkReference_PreRelease(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "6.0.1-preview.0")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .Version = versionReference),
                resolvedFramework: resolvedFramework,
                resultValidator: resolvedFramework != null ? null : new Action<CommandResult>(
                    commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, versionReference, "6.0.1-preview.1")));
        }

        // Soft roll forward from app [specified version] to inner framework reference 5.1.1
        [Theory]
        [InlineData("5.0.0", 0, null, null)]
        [InlineData("5.1.0", 0, null, "5.1.3")]
        [InlineData("5.1.0", 0, false, null)]
        [InlineData("5.1.1", 0, false, "5.1.1")]
        [InlineData("5.0.0", null, null, "5.1.3")]
        [InlineData("5.0.0", 1, null, "5.1.3")]
        [InlineData("1.0.0", 1, null, null)]
        [InlineData("1.0.0", 2, null, "5.1.3")]
        public void SoftRollForward_AppFrameworkReference_ToLower(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"),
                resolvedFramework: resolvedFramework,
                resultValidator: resolvedFramework != null ? null : new Action<CommandResult>(
                    commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, versionReference, "5.1.1")));
        }

        // Soft roll forward from app [specified version] to inner framework reference 5.1.1
        [Theory]
        [InlineData("5.4.0", null, null, "5.4.1")]
        [InlineData("6.0.0", null, null, null)]
        public void SoftRollForward_AppFrameworkReference_ToHigher(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.1.0")
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"),
                resolvedFramework: resolvedFramework,
                resultValidator: resolvedFramework != null ? null : new Action<CommandResult>(
                    commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, "5.1.1", versionReference)));
        }

        // Soft roll forward inner framework reference (defaults) to inner framework reference with [specified version]
        [Theory]
        [InlineData("5.0.0", 0,    null,  null)]
        [InlineData("5.1.0", 0,    null,  "5.1.3")]
        [InlineData("5.1.0", 0,    false, null)]
        [InlineData("5.0.0", null, null,  "5.1.3")]
        [InlineData("5.0.0", 1,    null,  "5.1.3")]
        [InlineData("1.0.0", 1,    null,  null)]
        [InlineData("1.0.0", 2,    null,  "5.1.3")]
        public void SoftRollForward_InnerToInnerFrameworkReference_ToLower(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(HighWare, "7.0.0"),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(HighWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.1.1");
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                            .WithApplyPatches(applyPatches)
                            .Version = versionReference);
                },
                resolvedFramework: resolvedFramework,
                resultValidator: resolvedFramework != null ? null : new Action<CommandResult>(
                    commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, versionReference, "5.1.3")));
        }

        // Soft roll forward inner framework reference (defaults) to inner framework reference with [specified version]
        [Theory]
        [InlineData("5.4.0", null, null, "5.4.1")]
        [InlineData("6.0.0", null, null, null)]
        public void SoftRollForward_InnerToInnerFrameworkReference_ToHigher(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(HighWare, "7.0.0"),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(HighWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.1.1");
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                            .WithApplyPatches(applyPatches)
                            .Version = versionReference);
                },
                resolvedFramework: resolvedFramework,
                resultValidator: resolvedFramework != null ? null : new Action<CommandResult>(
                    commandResult => commandResult.Should().Fail().And.FailedToSoftRollForward(MicrosoftNETCoreApp, "5.1.3", versionReference)));
        }

        // This test does:
        //  - Forces hard resolve of 5.1.1 -> 5.1.3 (direct reference from app)
        //  - Loads HighWare which has 5.4.1 
        //    - This forces a retry since 5.1.3 was hard resolved, so we have reload with 5.4.1 instead
        //  - Loads MiddleWare which has 5.6.0
        //    - This forces a retry since by this time 5.4.1 was hard resolved, so we have to reload with 5.6.0 instead
        [Fact]
        public void FrameworkResolutionRetry_FrameworkChain()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(2)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1")
                    .WithFramework(HighWare, "7.3.1"),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(HighWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.4.1");
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.6.0");
                },
                resultValidator: commandResult =>
                    commandResult.Should().Pass()
                        .And.RestartedFrameworkResolution("5.1.3", "5.4.1")
                        .And.RestartedFrameworkResolution("5.4.1", "5.6.0")
                        .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.6.0"));
        }

        // This test does:
        //  - Forces hard resolve of 5.1.1 -> 5.1.3 (direct reference from app)
        //  - Loads MiddleWare which has 5.4.1 
        //    - This forces a retry since 5.1.3 was hard resolved, so we have reload with 5.4.1 instead
        //  - Loads AnotherMiddleWare which has 5.6.0
        //    - This forces a retry since by this time 5.4.1 was hard resolved, so we have to reload with 5.6.0 instead
        [Fact]
        public void FrameworkResolutionRetry_FrameworkTree()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(2)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1")
                    .WithFramework(MiddleWare, "2.1.2")
                    .WithFramework(AnotherMiddleWare, "3.0.0"),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.4.1");
                    dotnetCustomizer.Framework(AnotherMiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.6.0");
                },
                resultValidator: commandResult =>
                    commandResult.Should().Pass()
                        .And.RestartedFrameworkResolution("5.1.3", "5.4.1")
                        .And.RestartedFrameworkResolution("5.4.1", "5.6.0")
                        .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.6.0"));
        }

        private void RunTest(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<DotNetCliExtensions.DotNetCliCustomizer> customizeDotNet = null,
            string resolvedFramework = null,
            Action<CommandResult> resultValidator = null)
        {
            using (DotNetCliExtensions.DotNetCliCustomizer dotnetCustomizer = SharedState.DotNetWithMultipleFrameworks.Customize())
            {
                customizeDotNet?.Invoke(dotnetCustomizer);

                RunTest(
                    SharedState.DotNetWithMultipleFrameworks,
                    SharedState.FrameworkReferenceApp,
                    runtimeConfig,
                    commandResult =>
                    {
                        if (resolvedFramework != null)
                        {
                            commandResult.Should().Pass()
                                .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
                        }
                        else
                        {
                            resultValidator?.Invoke(commandResult);
                        }
                    });
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithMultipleFrameworks { get; }

            public SharedTestState()
            {
                DotNetWithMultipleFrameworks = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFramework("5.1.1")
                    .AddMicrosoftNETCoreAppFramework("5.1.3")
                    .AddMicrosoftNETCoreAppFramework("5.4.1")
                    .AddMicrosoftNETCoreAppFramework("5.6.0")
                    .AddMicrosoftNETCoreAppFramework("6.0.1-preview.1")
                    .AddFramework(MiddleWare, "2.1.2", runtimeConfig =>
                        runtimeConfig.WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                    .AddFramework(AnotherMiddleWare, "3.0.0", runtimeConfig =>
                        runtimeConfig.WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                    .AddFramework(HighWare, "7.3.1", runtimeConfig =>
                        runtimeConfig
                            .WithFramework(MicrosoftNETCoreApp, "5.1.3")
                            .WithFramework(MiddleWare, "2.1.2"))
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
