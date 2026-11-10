// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

using static Microsoft.DotNet.CoreSetup.Test.Constants;

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
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.1.0-preview.2")]
        public void RollForwardOnPatch_FromReleaseToPreRelease(string rollForward, string resolvedFramework)
        {
            RunTest(
                "5.1.0",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // release version 5.0.0 and rolling forward to pre-release versions only with available
        // versions starting with 5.2.1-*. So roll over minor version.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.1.0-preview.2")]
        public void RollForwardOnMinor_FromReleaseToPreRelease(string rollForward, string resolvedFramework)
        {
            RunTest(
                "5.0.0",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // release version 4.0.0 and rolling forward to pre-release versions only with available
        // versions starting with 5.2.1-*. So roll over major version.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.1.0-preview.2")]
        public void RollForwardOnMajor_FromReleaseToPreRelease(string rollForward, string resolvedFramework)
        {
            RunTest(
                "4.0.0",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings won't roll back (on pre-release).
        // Starting from 5.1.2-preview.3 which is higher than any available 5.1.2 version.
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        public void NeverRollBackOnPreRelease_PreReleaseOnly(string rollForward)
        {
            string requestedVersion = "5.1.2-preview.3";
            RunTest(
                requestedVersion,
                rollForward)
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        // Verifies that rollForward settings won't roll back (on patch).
        // Starting from 5.1.3-preview.1 which is higher than any available 5.1.* version.
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        public void NeverRollBackOnPatch_PreReleaseOnly(string rollForward)
        {
            string requestedVersion = "5.1.3-preview.1";
            RunTest(
                requestedVersion,
                rollForward)
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        // Verifies that rollForward settings won't roll back (on minor).
        // Starting from 5.3.0-preview.1 which is higher than any available 5.*.* version.
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        public void NeverRollBackOnMinor_PreReleaseOnly(string rollForward)
        {
            string requestedVersion = "5.3.0-preview.1";
            RunTest(
                requestedVersion,
                rollForward)
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        // Verifies that rollForward settings won't roll back (on major).
        // Starting from 7.1.0-preview.1 which is higher than any available version.
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        public void NeverRollBackOnMajor_PreReleaseOnly(string rollForward)
        {
            string requestedVersion = "7.1.0-preview.1";
            RunTest(
                requestedVersion,
                rollForward)
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // pre-release version 5.1.1-preview.1 which is available and rolling forward to pre-release versions only with available
        // versions starting with 5.1.1-preview.1. Since the first match is pre-release no roll to latest patch is applied.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.1.0-preview.2")]
        public void RollFromExisting_PreReleaseOnly(string rollForward, string resolvedFramework)
        {
            RunTest(
                "5.1.1-preview.1",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // pre-release version 5.1.2-preview.0 and rolling forward to pre-release versions only with available
        // versions starting with 5.1.2-preview.1. Since the first match is pre-release no roll to latest patch is applied.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, "5.1.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       "5.1.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.1.0-preview.2")]
        public void RollForwardOnPreRelease_PreReleaseOnly(string rollForward, string resolvedFramework)
        {
            RunTest(
                "5.1.2-preview.0",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // pre-release version 5.1.0-preview.1 and rolling forward to pre-release versions only with available
        // versions starting with 5.1.2-preview.1. Since the first match is pre-release no roll to latest patch is applied.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.1.0-preview.2")]
        public void RollForwardOnPatch_PreReleaseOnly(string rollForward, string resolvedFramework)
        {
            RunTest(
                "5.1.0-preview.1",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // pre-release version 5.0.0-preview.5 and rolling forward to pre-release versions only with available
        // versions starting with 5.1.1-preview.1. Since the first match is pre-release no roll to latest patch is applied.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "5.2.1-preview.2")]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.1.0-preview.2")]
        public void RollForwardOnMinor_PreReleaseOnly(string rollForward, string resolvedFramework)
        {
            RunTest(
                "5.0.0-preview.5",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected starting with framework reference
        // pre-release version 4.1.0-preview.6 and rolling forward to pre-release versions only with available
        // versions starting with 5.1.1-preview.1. Since the first match is pre-release no roll to latest patch is applied.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.1-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.1.0-preview.2")]
        public void RollForwardOnMajor_PreReleaseOnly(string rollForward, string resolvedFramework)
        {
            RunTest(
                "4.1.0-preview.6",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        private CommandResult RunTest(
            string frameworkReferenceVersion,
            string rollForward,
            [CallerMemberName] string caller = "")
        {
            return RunTest(
                SharedState.DotNetWithNETCoreAppPreRelease,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, frameworkReferenceVersion))
                    .With(RollForwardSetting(SettingLocation.CommandLine, rollForward)),
                caller: caller);
        }
    }
}
