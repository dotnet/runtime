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
    /// Tests for rollForward option behavior considering combinations of release and pre-release versions.
    /// so only release versions are available and only release versions are asked for
    /// in framework references.
    /// </summary>
    public class RollForwardReleaseAndPreRelease :
        FrameworkResolutionBase,
        IClassFixture<RollForwardReleaseAndPreRelease.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public RollForwardReleaseAndPreRelease(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithNETCoreAppReleaseAndPreRelease { get; }

            public SharedTestState()
            {
                DotNetWithNETCoreAppReleaseAndPreRelease = DotNet("DotNetWithNETCoreAppReleaseAndPreRelease")

                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.0-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.0-preview.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.2-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.3-preview.1")

                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.5.1-preview.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.5.2-preview.1")

                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.0-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.2-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.2")

                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.5.1-preview.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.5.2")

                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.0.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.0.2-preview.1")

                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }

        // -----------------------------------
        // Tests where the starting reference is a release version
        //
        // Available (relevant) framework versions (full list see above):
        // 4.1.0-preview.1
        // 4.1.0-preview.2
        // 4.1.1
        // 4.1.2-preview.1
        // 4.1.2
        // 4.1.3-preview.1

        // Verifies that rollForward settings behave as expected when starting from 4.1.1 which does exit
        // to other available 4.1.* versions (both release and pre-release). So roll forward on patch version.
        // Also verifying behavior when DOTNET_ROLL_FORWARD_TO_PRERELEASE is set.
        [Theory] // rollForward                               rollForwardToPreRelease resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     false,                  "4.1.1")]
        [InlineData(Constants.RollForwardSetting.Disable,     true,                   "4.1.1")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false,                  "4.1.2")] // Prefers release over pre-release
        [InlineData(Constants.RollForwardSetting.LatestPatch, true,                   "4.1.3-preview.1")] // Pre-release is considered equally to release
        [InlineData(Constants.RollForwardSetting.Minor,       false,                  "4.1.2")] // Prefers release over pre-release
        [InlineData(Constants.RollForwardSetting.Minor,       true,                   "4.1.3-preview.1")] // Pre-release is considered equally to release
        [InlineData(Constants.RollForwardSetting.Major,       false,                  "4.1.2")] // Prefers release over pre-release
        [InlineData(Constants.RollForwardSetting.Major,       true,                   "4.1.3-preview.1")] // Pre-release is considered equally to release
        public void RollFromExisting_FromReleaseToPreRelease(
            string rollForward,
            bool rollForwardToPreRelease,
            string resolvedFramework)
        {
            RunTest(
                "4.1.1",
                rollForward,
                rollForwardToPreRelease)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 4.1.0 which doesn't exist
        // to other available 4.1.* versions (both release and pre-release). So roll forward on patch version.
        // Also verifying behavior when DOTNET_ROLL_FORWARD_TO_PRERELEASE is set.
        [Theory] // rollForward                               rollForwardToPreRelease resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     false,                  ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Disable,     true,                   ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, false,                  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, true,                   "4.1.3-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       false,                  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.Minor,       true,                   "4.1.3-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false,                  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, true,                   "4.5.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.Major,       false,                  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.Major,       true,                   "4.1.3-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, false,                  "6.0.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, true,                   "6.0.2-preview.1")]
        public void RollForwardOnPatch_FromReleaseToPreRelease(
            string rollForward,
            bool rollForwardToPreRelease,
            string resolvedFramework)
        {
            RunTest(
                "4.1.0",
                rollForward,
                rollForwardToPreRelease)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 4.0.0 which doesn't exit
        // to other available 4.1.* versions (both release and pre-release). So roll forward on minor version.
        // Specifically targeting the behavior that starting from release should by default prefer release versions.
        // Also verifying behavior when DOTNET_ROLL_FORWARD_TO_PRERELEASE is set.
        [Theory] // rollForward                               rollForwardToPreRelease resolvedFramework
        [InlineData(Constants.RollForwardSetting.Minor,       false,                  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.Minor,       true,                   "4.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, false,                  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, true,                   "4.5.2-preview.1")]
        [InlineData(Constants.RollForwardSetting.Major,       false,                  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.Major,       true,                   "4.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, false,                  "6.0.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, true,                   "6.0.2-preview.1")]
        public void RollForwardOnMinor_FromReleaseIgnoresPreReleaseIfReleaseAvailable(
            string rollForward,
            bool rollForwardToPreRelease,
            string resolvedFramework)
        {
            RunTest(
                "4.0.0",
                rollForward,
                rollForwardToPreRelease)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 3.0.0 which does exit
        // to other available 4.1.* versions (both release and pre-release). So roll forward on major version.
        // Specifically targeting the behavior that starting from release should by default prefer release versions.
        // Also verifying behavior when DOTNET_ROLL_FORWARD_TO_PRERELEASE is set.
        [Theory] // rollForward                               rollForwardToPreRelease resolvedFramework
        [InlineData(Constants.RollForwardSetting.Major,       false,                  "4.1.2")]
        [InlineData(Constants.RollForwardSetting.Major,       true,                   "4.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, false,                  "6.0.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, true,                   "6.0.2-preview.1")]
        public void RollForwardOnMajor_FromReleaseIgnoresPreReleaseIfReleaseAvailable(
            string rollForward,
            bool rollForwardToPreRelease,
            string resolvedFramework)
        {
            RunTest(
                "3.0.0",
                rollForward,
                rollForwardToPreRelease)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
        }


        // -----------------------------------
        // Tests where the starting reference is a pre-release version
        //
        // Available (relevant) framework versions (full list see above):
        // 5.1.0-preview.1
        // 5.1.1
        // 5.1.2-preview.1
        // 5.1.2

        // Verifies that rollForward settings behave as expected when starting from 5.1.0-preview.1 which does exist
        // to other available 5.1.* versions (both release and pre-release).
        // Starting from pre-release means that exact match is always taken unless LatestMinor/LatestMajor is used.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "5.5.2")]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.0.2-preview.1")]
        public void RollFromExisting_FromPreReleaseToRelease(string rollForward, string resolvedFramework)
        {
            RunTest(
                "5.1.0-preview.1",
                rollForward)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 5.1.0-preview.0 which doesn't exist
        // to other available 5.1.* versions (both release and pre-release). So roll forward on patch version.
        // Starting from pre-release means that all versions are always considered (both release and pre-release).
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.Minor,       "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "5.5.2")]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.0.2-preview.1")]
        public void RollForwardOnPatch_FromPreReleaseToRelease(string rollForward, string resolvedFramework)
        {
            RunTest(
                "5.1.0-preview.0",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 5.0.0-preview.5
        // to other available 5.*.* versions (both release and pre-release). So roll forward on minor version.
        // Starting from pre-release means that all versions are always considered (both release and pre-release).
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Minor,       "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "5.5.2")]
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.0.2-preview.1")]
        public void RollForwardOnMinor_FromPreReleaseToRelease(string rollForward, string resolvedFramework)
        {
            RunTest(
                "5.0.0-preview.5",
                rollForward)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 4.9.0-preview.6
        // to other available 5.*.* versions (both release and pre-release). So roll forward on major version.
        // Starting from pre-release means that all versions are always considered (both release and pre-release).
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Major,       "5.1.0-preview.1")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "6.0.2-preview.1")]
        public void RollForwardOnMajor_FromPreReleaseToRelease(string rollForward, string resolvedFramework)
        {
            RunTest(
                "4.9.0-preview.6",
                rollForward)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Just a sanity check, DOTNET_ROLL_FORWARD_TO_PRERELEASE should have no effect if the framework reference is pre-release
        [Theory] // rollForwardToPreRelease
        [InlineData(false)]
        [InlineData(true)]
        public void RollForwardOnPatch_FromPreReleaseToRelease_RollForwardToPreRelease(bool rollForwardToPreRelease)
        {
            // Defaults
            RunTest(
                "5.1.0-preview.0",
                rollForward: null,
                rollForwardToPreRelease: rollForwardToPreRelease)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.0-preview.1");
        }

        private CommandResult RunTest(
            string frameworkReferenceVersion,
            string rollForward,
            bool rollForwardToPreRelease = false,
            [CallerMemberName] string caller = "")
        {
            return RunTest(
                SharedState.DotNetWithNETCoreAppReleaseAndPreRelease,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                        .WithFramework(MicrosoftNETCoreApp, frameworkReferenceVersion))
                    .With(RollForwardSetting(SettingLocation.CommandLine, rollForward))
                    .WithEnvironment(Constants.RollForwardToPreRelease.EnvironmentVariable, rollForwardToPreRelease ? "1" : "0"),
                caller: caller);
        }
    }
}
