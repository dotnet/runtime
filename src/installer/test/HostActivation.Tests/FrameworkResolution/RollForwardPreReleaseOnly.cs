// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    /// <summary>
    /// Tests for rollForward option behavior considering only pre-release versions
    /// so only pre-release versions are available and only pre-release versions are asked for
    /// in framework references.
    /// </summary>
    public class RollForwardPreReleaseOnly :
        FrameworkResolutionBase,
        IClassFixture<RollForwardPreReleaseOnly.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public RollForwardPreReleaseOnly(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithNETCoreAppPreRelease { get; }

            public SharedTestState()
            {
                DotNetWithNETCoreAppPreRelease = DotNet("DotNetWithNETCoreAppPreRelease")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.1-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.2-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.2-preview.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.2.0-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.2.1-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.2.1-preview.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.0-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.0-preview.2")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // release version 5.1.0 and rolling forward to pre-release versions only with available
        // versions starting with 5.2.1-*. So roll over patch version.
        [Theory] // rollForward                               applyPatches resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false,       ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false,       "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Major,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "6.1.0-preview.2")]
        public void RollForwardOnPatch_FromReleaseToPreRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.0",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // release version 5.0.0 and rolling forward to pre-release versions only with available
        // versions starting with 5.2.1-*. So roll over minor version.
        [Theory] // rollForward                               applyPatches resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false,       "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Major,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "6.1.0-preview.2")]
        public void RollForwardOnMinor_FromReleaseToPreRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.0.0",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // release version 4.0.0 and rolling forward to pre-release versions only with available
        // versions starting with 5.2.1-*. So roll over major version.
        [Theory] // rollForward                               applyPatches resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Major,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "6.1.0-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, false,       "6.1.0-preview.2")]
        public void RollForwardOnMajor_FromReleaseToPreRelease(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "4.0.0",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings won't roll back (on pre-release).
        // Starting from 5.1.2-preview.3 which is higher than any available 5.1.2 version.
        [Theory] // rollForward                               applyPatches
        [InlineData(Constants.RollForwardSetting.Disable,     null)]
        [InlineData(Constants.RollForwardSetting.Disable,     false)]
        [InlineData(Constants.RollForwardSetting.Disable,     true)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false)]
        public void NeverRollBackOnPreRelease_PreReleaseOnly(string rollForward, bool? applyPatches)
        {
            RunTest(
                "5.1.2-preview.3",
                rollForward,
                applyPatches)
                .ShouldFailToFindCompatibleFrameworkVersion();
        }

        // Verifies that rollForward settings won't roll back (on patch).
        // Starting from 5.1.3-preview.1 which is higher than any available 5.1.* version.
        [Theory] // rollForward                               applyPatches
        [InlineData(Constants.RollForwardSetting.Disable,     null)]
        [InlineData(Constants.RollForwardSetting.Disable,     false)]
        [InlineData(Constants.RollForwardSetting.Disable,     true)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false)]
        public void NeverRollBackOnPatch_PreReleaseOnly(string rollForward, bool? applyPatches)
        {
            RunTest(
                "5.1.3-preview.1",
                rollForward,
                applyPatches)
                .ShouldFailToFindCompatibleFrameworkVersion();
        }

        // Verifies that rollForward settings won't roll back (on minor).
        // Starting from 5.3.0-preview.1 which is higher than any available 5.*.* version.
        [Theory] // rollForward                               applyPatches
        [InlineData(Constants.RollForwardSetting.Disable,     null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData(Constants.RollForwardSetting.Minor,       null)]
        [InlineData(Constants.RollForwardSetting.Minor,       false)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false)]
        public void NeverRollBackOnMinor_PreReleaseOnly(string rollForward, bool? applyPatches)
        {
            RunTest(
                "5.3.0-preview.1",
                rollForward,
                applyPatches)
                .ShouldFailToFindCompatibleFrameworkVersion();
        }

        // Verifies that rollForward settings won't roll back (on major).
        // Starting from 7.1.0-preview.1 which is higher than any available version.
        [Theory] // rollForward                               applyPatches
        [InlineData(Constants.RollForwardSetting.Disable,     null)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null)]
        [InlineData(Constants.RollForwardSetting.Minor,       null)]
        [InlineData(Constants.RollForwardSetting.Minor,       false)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false)]
        public void NeverRollBackOnMajor_PreReleaseOnly(string rollForward, bool? applyPatches)
        {
            RunTest(
                "7.1.0-preview.1",
                rollForward,
                applyPatches)
                .ShouldFailToFindCompatibleFrameworkVersion();
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // pre-release version 5.1.1-preview.1 which is available and rolling forward to pre-release versions only with available
        // versions starting with 5.1.1-preview.1. Since the first match is pre-release no roll to latest patch is applied.
        [Theory] // rollForward                               applyPatches rollForward
        [InlineData(Constants.RollForwardSetting.Disable,     null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false,       "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Major,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "6.1.0-preview.2")]
        public void RollFromExisting_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.1-preview.1",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // pre-release version 5.1.2-preview.0 and rolling forward to pre-release versions only with available
        // versions starting with 5.1.2-preview.1. Since the first match is pre-release no roll to latest patch is applied.
        [Theory] // rollForward                               applyPatches rollForward
        [InlineData(Constants.RollForwardSetting.Disable,     null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        "5.1.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false,       "5.1.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        "5.1.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       false,       "5.1.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false,       "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "5.1.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.Major,       false,       "5.1.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "6.1.0-preview.2")]
        public void RollForwardOnPreRelease_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.2-preview.0",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // pre-release version 5.1.0-preview.1 and rolling forward to pre-release versions only with available
        // versions starting with 5.1.2-preview.1. Since the first match is pre-release no roll to latest patch is applied.
        [Theory] // rollForward                               applyPatches rollForward
        [InlineData(Constants.RollForwardSetting.Disable,     null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false,       ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false,       "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Major,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "6.1.0-preview.2")]
        public void RollForwardOnPatch_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.1.0-preview.1",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // pre-release version 5.0.0-preview.5 and rolling forward to pre-release versions only with available
        // versions starting with 5.1.1-preview.1. Since the first match is pre-release no roll to latest patch is applied.
        [Theory] // rollForward                               applyPatches rollForward
        [InlineData(Constants.RollForwardSetting.Disable,     null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false,       "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Major,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "6.1.0-preview.2")]
        public void RollForwardOnMinor_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "5.0.0-preview.5",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // pre-release version 4.1.0-preview.6 and rolling forward to pre-release versions only with available
        // versions starting with 5.1.1-preview.1. Since the first match is pre-release no roll to latest patch is applied.
        [Theory] // rollForward                               applyPatches rollForward
        [InlineData(Constants.RollForwardSetting.Disable,     null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, null,        ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Major,       null,        "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Major,       false,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, null,        "6.1.0-preview.2")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, false,       "6.1.0-preview.2")]
        public void RollForwardOnMajor_PreReleaseOnly(string rollForward, bool? applyPatches, string resolvedFramework)
        {
            RunTest(
                "4.1.0-preview.6",
                rollForward,
                applyPatches)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        private CommandResult RunTest(
            string frameworkReferenceVersion,
            string rollForward,
            bool? applyPatches)
        {
            return RunTest(
                SharedState.DotNetWithNETCoreAppPreRelease,
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