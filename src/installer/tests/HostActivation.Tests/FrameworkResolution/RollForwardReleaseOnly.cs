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
                rollForward: null)
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "2.1.3");
        }

        // Verifies that rollForward settings behave as expected when starting from 2.1.2 which does exist
        // to other available 2.1.* versions. So roll forward on patch version.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     "2.1.2")]
        [InlineData(Constants.RollForwardSetting.LatestPatch, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Minor,       "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Major,       "2.1.3")]
        public void RollFromExisting_ReleaseOnly(string rollForward, string resolvedFramework)
        {
            RunTest(
                "2.1.2",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 2.1.0 which doesn't exist
        // to other available 2.1.* versions. So roll forward on patch version.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, "2.1.3")]
        [InlineData(Constants.RollForwardSetting.Minor,       "2.1.3")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "2.4.1")]
        [InlineData(Constants.RollForwardSetting.Major,       "2.1.3")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "3.1.2")]
        public void RollForwardOnPatch_ReleaseOnly(string rollForward, string resolvedFramework)
        {
            RunTest(
                "2.1.0",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 2.0.0
        // to other available 2.*.* and higher versions. So roll forward on minor version.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       "2.1.3")]
        [InlineData(Constants.RollForwardSetting.LatestMinor, "2.4.1")]
        [InlineData(Constants.RollForwardSetting.Major,       "2.1.3")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "3.1.2")]
        public void RollForwardOnMinor_ReleaseOnly(string rollForward, string resolvedFramework)
        {
            RunTest(
                "2.0.0",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForward settings behave as expected when starting from 1.0.0
        // to other available 2.*.* and higher versions. So roll forward on major version.
        [Theory] // rollForward                               resolvedFramework
        [InlineData(Constants.RollForwardSetting.Disable,     ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestPatch, ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Minor,       ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.LatestMinor, ResolvedFramework.NotFound)]
        [InlineData(Constants.RollForwardSetting.Major,       "2.1.3")]
        [InlineData(Constants.RollForwardSetting.LatestMajor, "3.1.2")]
        public void RollForwardOnMajor_ReleaseOnly(string rollForward, string resolvedFramework)
        {
            RunTest(
                "1.1.0",
                rollForward)
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verify that rollForward settings will never roll back to lower patch version.
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        public void NeverRollBackOnPatch_ReleaseOnly(string rollForward)
        {
            string requestedVersion = "2.1.4";
            RunTest(
                requestedVersion,
                rollForward)
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        // Verify that rollForward settings will never roll back to lower minor version.
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        public void NeverRollBackOnMinor_ReleaseOnly(string rollForward)
        {
            string requestedVersion = "2.5.0";
            RunTest(
                requestedVersion,
                rollForward)
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        // Verify that rollForward settings will never roll back to lower major version.
        [Theory]
        [InlineData(Constants.RollForwardSetting.Disable)]
        [InlineData(Constants.RollForwardSetting.LatestPatch)]
        [InlineData(Constants.RollForwardSetting.Minor)]
        [InlineData(Constants.RollForwardSetting.LatestMinor)]
        public void NeverRollBackOnMajor_ReleaseOnly(string rollForward)
        {
            string requestedVersion = "4.1.0";
            RunTest(
                requestedVersion,
                rollForward)
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        private CommandResult RunTest(
            string frameworkReferenceVersion,
            string rollForward,
            [CallerMemberName] string caller = "")
        {
            return RunTest(
                SharedState.DotNetWithNETCoreAppRelease,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                   .WithRuntimeConfigCustomizer(runtimeConfig => runtimeConfig
                       .WithFramework(MicrosoftNETCoreApp, frameworkReferenceVersion))
                   .With(RollForwardSetting(SettingLocation.CommandLine, rollForward)),
                caller: caller);
        }
    }
}
