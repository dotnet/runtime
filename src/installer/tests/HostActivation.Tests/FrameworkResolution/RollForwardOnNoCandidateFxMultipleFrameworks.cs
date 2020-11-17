// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardOnNoCandidateFxMultipleFrameworks :
        FrameworkResolutionBase,
        IClassFixture<RollForwardOnNoCandidateFxMultipleFrameworks.SharedTestState>
    {
        private const string MiddleWare = "MiddleWare";
        private const string AnotherMiddleWare = "AnotherMiddleWare";
        private const string HighWare = "HighWare";

        private SharedTestState SharedState { get; }

        public RollForwardOnNoCandidateFxMultipleFrameworks(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithMultipleFrameworks { get; }

            public SharedTestState()
            {
                DotNetWithMultipleFrameworks = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.4.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.6.0")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.0.0")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.1-preview.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.2.1")
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

        // Verify that inner framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>)
        // is correctly reconciled with app's framework reference 5.1.1 (defaults = RollForward:Minor). App fx reference is higher.
        [Theory] // fxRefVersion  rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("5.0.0",      0,                          null,         ResolvedFramework.FailedToReconcile)]
        [InlineData("5.1.0",      0,                          null,         "5.1.3")]
        [InlineData("5.1.0",      0,                          false,        ResolvedFramework.FailedToReconcile)]
        [InlineData("5.1.1",      0,                          false,        "5.1.1")]
        [InlineData("5.0.0",      null,                       null,         "5.1.3")]
        [InlineData("5.0.0",      1,                          null,         "5.1.3")]
        [InlineData("5.1.0",      1,                          false,        "5.1.1")]
        [InlineData("1.0.0",      1,                          null,         ResolvedFramework.FailedToReconcile)]
        [InlineData("1.0.0",      2,                          null,         "5.1.3")]
        public void ReconcileFxReferences_InnerFrameworkReference_ToHigher(
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
                        .Version = versionReference))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "5.1.1");
        }

        // Verify that inner framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>)
        // is correctly reconciled with app's framework reference 5.1.1 (defaults = RollForward:Minor). App fx reference is higher.
        // In this case the direct reference from app is first, so the framework reference from app
        // is actually resolved against the disk - and the resolved framework is than compared to
        // the inner framework reference (potentially causing re-resolution).
        [Theory] // fxRefVersion  rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("5.0.0",      0,                          null,         ResolvedFramework.FailedToReconcile)]
        [InlineData("5.1.0",      0,                          null,         "5.1.3")]
        [InlineData("5.1.0",      0,                          false,        ResolvedFramework.FailedToReconcile)]
        [InlineData("5.1.3",      0,                          false,        "5.1.3")]
        [InlineData("5.0.0",      null,                       null,         "5.1.3")]
        [InlineData("5.0.0",      1,                          null,         "5.1.3")]
        [InlineData("5.0.0",      1,                          false,        "5.1.1")]
        [InlineData("5.1.0",      1,                          false,        "5.1.1")]
        [InlineData("1.0.0",      1,                          null,         ResolvedFramework.FailedToReconcile)]
        [InlineData("1.0.0",      2,                          null,         "5.1.3")]
        public void ReconcileFrameworkReferences_InnerFrameworkReference_ToHigher_HardResolve(
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
                        .Version = versionReference))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "5.1.1");
        }

        // Verify that inner framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>)
        // is correctly reconciled with app's framework reference 5.1.1 (defaults = RollForward:Minor). App fx reference is lower.
        [Theory] // fxRefVersion  rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("5.4.0",      null,                       null,         "5.4.1")]
        [InlineData("6.0.0",      null,                       null,         ResolvedFramework.FailedToReconcile)]
        public void ReconcileFrameworkReferences_InnerFrameworkReference_ToLower(
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
                        .Version = versionReference))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, "5.1.1", versionReference);
        }

        // Verify that inner framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>)
        // is correctly reconciled with app's framework reference 5.1.1 (defaults = RollForward:Minor). App fx reference is lower.
        // In this case the direct reference from app is first, so the framework reference from app
        // is actually resolved against the disk - and the resolved framework is than compared to
        // the inner framework reference (potentially causing re-resolution).
        [Theory] // fxRefVersion  rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("5.4.0",      null,                       null,         "5.4.1")]
        [InlineData("6.0.0",      null,                       null,         ResolvedFramework.FailedToReconcile)]
        public void ReconcileFrameworkReferences_InnerFrameworkReference_ToLower_HardResolve(
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
                        .Version = versionReference))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, "5.1.1", versionReference);
        }

        // Verify that inner framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>)
        // is correctly reconciled with app's framework reference 5.1.1 (defaults = RollForward:Minor). App fx reference is higher.
        // 3.0 change:
        // 2.* - release would never roll forward to pre-release
        // 3.* - release rolls forward to pre-release if there is no available release match
        [Theory] // fxRefVersion       rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("6.0.0",           null,                       null,         "6.2.1")]   // Starting from release version should prefer release version
        [InlineData("6.0.1-preview.0", null,                       null,         "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.1", null,                       null,         "6.1.1-preview.2")]
        [InlineData("6.0.1-preview.0", 0,                          null,         ResolvedFramework.FailedToReconcile)]
        [InlineData("6.1.0-preview.0", 0,                          false,        ResolvedFramework.FailedToReconcile)]
        [InlineData("6.1.0-preview.0", 0,                          null,         "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.0", 0,                          false,        "6.1.1-preview.2")] // applyPatches=false is ignored for pre-release roll
        [InlineData("6.1.1-preview.1", 0,                          null,         "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.1", 0,                          false,        "6.1.1-preview.2")]
        [InlineData("6.1.1-preview.2", 0,                          null,         "6.1.1-preview.2")]
        public void ReconcileFrameworkReferences_InnerFrameworkReference_PreRelease(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "6.1.1-preview.1")
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches)
                        .Version = versionReference))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "6.1.1-preview.1");
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with app's framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>).
        // App fx reference is lower.
        [Theory] // fxRefVersion  rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("5.0.0",      0,                          null,         ResolvedFramework.FailedToReconcile)]
        [InlineData("5.1.0",      0,                          null,         "5.1.3")]
        [InlineData("5.1.0",      0,                          false,        ResolvedFramework.FailedToReconcile)]
        [InlineData("5.1.1",      0,                          false,        "5.1.1")]
        [InlineData("5.0.0",      null,                       null,         "5.1.3")]
        [InlineData("5.1.0",      1,                          null,         "5.1.3")]
        [InlineData("5.1.0",      1,                          false,        "5.1.1")]
        [InlineData("5.0.0",      1,                          null,         "5.1.3")]
        [InlineData("1.0.0",      1,                          null,         ResolvedFramework.FailedToReconcile)]
        [InlineData("1.0.0",      2,                          null,         "5.1.3")]
        public void ReconcileFrameworkReferences_AppFrameworkReference_ToLower(
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
                        .Version = "5.1.1"))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "5.1.1");
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with app's framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>).
        // App fx reference is lower.
        // In this case the direct reference from app is first, so the framework reference from app
        // is actually resolved against the disk - and the resolved framework is than compared to
        // the inner framework reference (potentially causing re-resolution).
        [Theory] // fxRefVersion  rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("5.0.0",      0,                          null,         ResolvedFramework.NotFound)]
        [InlineData("5.1.0",      0,                          null,         "5.1.3")]
        [InlineData("5.1.0",      0,                          false,        ResolvedFramework.NotFound)]
        [InlineData("5.1.1",      0,                          false,        "5.1.1")]
        [InlineData("5.0.0",      null,                       null,         "5.1.3")]
        [InlineData("5.1.0",      1,                          null,         "5.1.3")]
        [InlineData("5.1.0",      1,                          false,        "5.1.1")]
        [InlineData("5.0.0",      1,                          null,         "5.1.3")]
        [InlineData("1.0.0",      1,                          null,         ResolvedFramework.NotFound)]
        [InlineData("1.0.0",      2,                          null,         "5.1.3")]
        public void ReconcileFrameworkReferences_AppFrameworkReference_ToLower_HardResolve(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches))
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"))
                // Note that in this case (since the app reference is first) if the app's framework reference
                // can't be resolved against the available frameworks, the error is actually a regular
                // "can't find framework" and not a framework reconcile event.
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with app's framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>).
        // App fx reference is higher.
        [Theory] // fxRefVersion  rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("5.4.0",      null,                       null,         "5.4.1")]
        [InlineData("6.0.0",      null,                       null,         ResolvedFramework.FailedToReconcile)]
        public void ReconcileFrameworkReferences_AppFrameworkReference_ToHigher(
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
                        .Version = "5.1.1"))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, "5.1.1", versionReference);
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with app's framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>).
        // App fx reference is higher.
        // In this case the direct reference from app is first, so the framework reference from app
        // is actually resolved against the disk - and the resolved framework is than compared to
        // the inner framework reference (potentially causing re-resolution).
        [Theory] // fxRefVersion  rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("5.4.0",      null,                       null,         "5.4.1")]
        [InlineData("6.0.0",      null,                       null,         null)]
        public void ReconcileFrameworkReferences_AppFrameworkReference_ToHigher_HardResolve(
            string versionReference,
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(new RuntimeConfig.Framework(MicrosoftNETCoreApp, versionReference)
                        .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                        .WithApplyPatches(applyPatches))
                    .WithFramework(MiddleWare, "2.1.0"),
                dotnetCustomizer => dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                    runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                        .Version = "5.1.1"))
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, "5.1.1", versionReference);
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with another's framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>).
        // The higher framework has fx reference with higher version.
        [Theory] // fxRefVersion  rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("5.0.0",      0,                          null,         ResolvedFramework.FailedToReconcile)]
        [InlineData("5.1.0",      0,                          null,         "5.1.3")]
        [InlineData("5.1.0",      0,                          false,        ResolvedFramework.FailedToReconcile)]
        [InlineData("5.0.0",      null,                       null,         "5.1.3")]
        [InlineData("5.0.0",      1,                          null,         "5.1.3")]
        [InlineData("1.0.0",      1,                          null,         ResolvedFramework.FailedToReconcile)]
        [InlineData("1.0.0",      2,                          null,         "5.1.3")]
        public void ReconcileFrameworkReferences_InnerToInnerFrameworkReference_ToLower(
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
                })
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, versionReference, "5.1.1");
        }

        // Verify that inner framework reference 5.1.1 (defaults = RollForward:Minor)
        // is correctly reconciled with another's framework reference (<fxRefVersion>, <rollForwardOnNoCandidateFx>, <applyPatches>).
        // The higher framework has fx reference with lower version.
        [Theory] // fxRefVersion  rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData("5.4.0",      null,                       null,         "5.4.1")]
        [InlineData("6.0.0",      null,                       null,         ResolvedFramework.FailedToReconcile)]
        public void ReconcileFrameworkReferences_InnerToInnerFrameworkReference_ToHigher(
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
                })
                .ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
                    MicrosoftNETCoreApp, resolvedFramework, "5.1.1", versionReference);
        }

        // This test:
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
                })
                .Should().Pass()
                .And.RestartedFrameworkResolution("5.1.1", "5.4.1")
                .And.RestartedFrameworkResolution("5.4.1", "5.6.0")
                .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.6.0");
        }

        // This test:
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
                })
                .Should().Pass()
                .And.RestartedFrameworkResolution("5.1.1", "5.4.1")
                .And.RestartedFrameworkResolution("5.4.1", "5.6.0")
                .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.6.0");
        }

        // Verifies that roll forward acts on all framework references (3 frameworks in chain)
        [Fact]
        public void RollForwardOnAllFrameworks()
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(MiddleWare, "2.0.0")
                    .WithFramework(HighWare, "7.0.0")
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                dotnetCustomizer =>
                {
                    dotnetCustomizer.Framework(MiddleWare).RuntimeConfig(runtimeConfig =>
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.0.0");
                    dotnetCustomizer.Framework(HighWare).RuntimeConfig(runtimeConfig =>
                    {
                        runtimeConfig.GetFramework(MiddleWare)
                            .Version = "2.0.0";
                        runtimeConfig.GetFramework(MicrosoftNETCoreApp)
                            .Version = "5.0.0";
                    });
                })
                .Should().Pass()
                .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3")
                .And.HaveResolvedFramework(MiddleWare, "2.1.2")
                .And.HaveResolvedFramework(HighWare, "7.3.1");
        }

        private CommandResult RunTest(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<DotNetCliExtensions.DotNetCliCustomizer> customizeDotNet = null)
        {
            return RunTest(
                SharedState.DotNetWithMultipleFrameworks,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig)
                    .WithDotnetCustomizer(customizeDotNet));
        }
    }
}
