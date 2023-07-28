// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    /// <summary>
    /// Tests for rollForward option behavior considering only release versions
    /// so only release versions are available and only release versions are asked for
    /// in framework references.
    /// </summary>
    public class RollForwardReleaseOnly :
        FrameworkResolutionBase,
        IClassFixture<RollForwardReleaseOnly.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public RollForwardReleaseOnly(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithNETCoreAppRelease { get; }

            public SharedTestState()
            {
                DotNetWithNETCoreAppRelease = DotNet("DotNetWithNETCoreAppRelease")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("2.1.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("2.1.3")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("2.4.0")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("2.4.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("3.1.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("3.1.2")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }

        // Verifies that exact version match is resolved by default
        [Fact]
        public void ExactMatchOnRelease()
        {
            RunTest(
                frameworkReferenceVersion: "2.1.3",
                rollForward: null,
                applyPatches: null)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "2.1.3");
        }

        // Verifies that rollForward settings behave as expected when starting from 2.1.2 which does exist
        // to other available 2.1.* versions. So roll forward on patch version.
        [Theory] // rollForward                               applyPatches resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     null,        "2.1.2")]
        // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Disable,     false,       "2.1.2")]
        // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Disable,     true,        "2.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        "2.1.3")]
        // Backward compat, equivalent to rollForwardOnNoCandidateFx=0, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestPatch, false,       "2.1.2")]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        "2.1.3")]
        // Backward compat, equivalent to rollForwardOnNoCandidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Minor,       false,       "2.1.2")]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "2.1.3")]
        // Backward compat, equivalent to rollForwardOnNoCandidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Major,       false,       "2.1.2")]
        public void RollFromExisting_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "2.1.2",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 2.1.0 which doesn't exist
        // to other available 2.1.* versions. So roll forward on patch version.
        [Theory] // rollForward                               applyPatches resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     null,        ResolvedFramework.NotFound)]
        // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Disable,     false,       ResolvedFramework.NotFound)]
        // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.Disable,     true,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        "2.1.3")]
        // Backward compat, equivalent to rollForwardOnNoCandidateFx=0, applyPatches=false
        [InlineData(Constants.RollForwardSetting.LatestPatch, false,       ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        "2.1.3")]
        // Backward compat, equivalent to rollForwardOnNoCandidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Minor,       false,       "2.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        "2.4.1")]
        // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.LatestMinor, false,       "2.4.1")]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "2.1.3")]
        // Backward compat, equivalent to rollForwardOnNoCandidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Major,       false,       "2.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "3.1.2")]
        public void RollForwardOnPatch_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "2.1.0",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 2.0.0
        // to other available 2.*.* and higher versions. So roll forward on minor version.
        [Theory] // rollForward                               applyPatches resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        "2.1.3")]
        // Backward compat, equivalent to rollForwardOnNoCandidateFx=1, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Minor,       false,       "2.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        "2.4.1")]
        // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.LatestMinor, false,       "2.4.1")]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "2.1.3")]
        // Backward compat, equivalent to rollForwardOnNoCandidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Major,       false,       "2.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "3.1.2")]
        public void RollForwardOnMinor_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "2.0.0",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 1.0.0
        // to other available 2.*.* and higher versions. So roll forward on major version.
        [Theory] // rollForward                               applyPatches resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "2.1.3")]
        // Backward compat, equivalent to rollForwardOnNoCandidateFx=2, applyPatches=false
        [InlineData(Constants.RollForwardSetting.Major,       false,       "2.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "3.1.2")]
        // applyPatches is ignored for new rollForward settings
        [InlineData(Constants.RollForwardSetting.LatestMajor, false,       "3.1.2")]
        public void RollForwardOnMajor_ReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "1.1.0",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verify that rollForward settings will never roll back to lower patch version.
        [Theory] // rollForward                               applyPatches
        [InlineData(Constants.RollForwardSetting.Disable,     null)]
        [InlineData(Constants.RollForwardSetting.Disable,     false)]
        [InlineData(Constants.RollForwardSetting.Disable,     true)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false)]
        public void NeverRollBackOnPatch_ReleaseOnly(string rollForward, bool? applyPatches)
        {
            string requestedVersion = "2.1.4";
            RunTest(
                requestedVersion,
                rollForward,
                applyPatches)
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        // Verify that rollForward settings will never roll back to lower minor version.
        [Theory] // rollForward                               applyPatches
        [InlineData(Constants.RollForwardSetting.Disable,     null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData(Constants.RollForwardSetting.Minor,       null)]
        [InlineData(Constants.RollForwardSetting.Minor,       false)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false)]
        public void NeverRollBackOnMinor_ReleaseOnly(string rollForward, bool? applyPatches)
        {
            string requestedVersion = "2.5.0";
            RunTest(
                requestedVersion,
                rollForward,
                applyPatches)
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        // Verify that rollForward settings will never roll back to lower major version.
        [Theory] // rollForward                               applyPatches
        [InlineData(Constants.RollForwardSetting.Disable,     null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData(Constants.RollForwardSetting.Minor,       null)]
        [InlineData(Constants.RollForwardSetting.Minor,       false)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false)]
        public void NeverRollBackOnMajor_ReleaseOnly(string rollForward, bool? applyPatches)
        {
            string requestedVersion = "4.1.0";
            RunTest(
                requestedVersion,
                rollForward,
                applyPatches)
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        private CommandResult RunTest(
            string frameworkReferenceVersion,
            string rollForward,
            bool? applyPatches)
        {
            return RunTest(
                SharedState.DotNetWithNETCoreAppRelease,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithApplyPatches(applyPatches)
                        .WithFramework(MicrosoftNETCoreApp, frameworkReferenceVersion))
                   // Using command line, so that it's possible to mix rollForward and applyPatches
                   .With(RollForwardSetting(SettingLocation.CommandLine, rollForward)));
        }
    }
}
