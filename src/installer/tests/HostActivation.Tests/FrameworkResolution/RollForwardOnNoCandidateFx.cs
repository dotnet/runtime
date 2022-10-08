// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public class RollForwardOnNoCandidateFx :
        FrameworkResolutionBase,
        IClassFixture<RollForwardOnNoCandidateFx.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public RollForwardOnNoCandidateFx(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithOneFramework { get; }

            public DotNetCli DotNetWithPreReleaseFramework { get; }

            public DotNetCli DotNetWithManyVersions { get; }

            public SharedTestState()
            {
                DotNetWithOneFramework = DotNet("WithOneFramework")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3")
                    .Build();

                DotNetWithPreReleaseFramework = DotNet("WithPreReleaseFramework")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3-preview.2")
                    .Build();

                DotNetWithManyVersions = DotNet("WithManyVersions")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("2.3.1-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("2.3.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.1.3-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.2.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.5.1-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("4.5.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.3-preview.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.1.4-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.2.3-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("5.2.3-preview.2")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("6.1.2-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("7.1.1-preview.1")
                    .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("7.1.2-preview.1")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }

        #region With one release framework
        // RunTestWithOneFramework
        //   dotnet with
        //     - Microsoft.NETCore.App 5.1.3

        // Verifies that exact match for release version picks that version by default.
        [Fact]
        public void ExactMatchOnRelease_NoSettings()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches work as expected.
        // Rolling from 5.1.0 to 5.1.3 version. So roll on patch version.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches
        [InlineData(null,                       null)]
        [InlineData(0,                          null)]
        [InlineData(1,                          null)]
        // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        [InlineData(1,                          false)]
        [InlineData(2,                          null)]
        // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        [InlineData(2,                          false)]
        public void RollForwardToLatestPatch_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches work as expected.
        // Rolling from 5.0.0 to 5.1.3 version. So roll on minor version.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  passes
        [InlineData(null,                       null,         true)]
        [InlineData(null,                       false,        true)]
        [InlineData(0,                          null,         false)]
        [InlineData(1,                          null,         true)]
        // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        [InlineData(1,                          false,        true)]
        [InlineData(2,                          null,         true)]
        // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        [InlineData(2,                          false,        true)]
        public void RollForwardOnMinor_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            string requestedVersion = "5.0.0";
            CommandResult result = RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, requestedVersion));
            if (passes)
            {
                result.ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
            }
            else
            {
                result.ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
            }
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches work as expected.
        // Rolling from 4.1.0 to 5.1.3 version. So roll on major version.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  passes
        [InlineData(null,                       null,         false)]
        [InlineData(0,                          null,         false)]
        [InlineData(1,                          null,         false)]
        [InlineData(1,                          false,        false)]
        [InlineData(2,                          null,         true)]
        // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0, but only to the lowest higher
        [InlineData(2,                          false,        true)]
        public void RollForwardOnMajor_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            string requestedVersion = "4.1.0";
            CommandResult result = RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, requestedVersion));
            if (passes)
            {
                result.ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
            }
            else
            {
                result.ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
            }
        }

        // Verifies that no matter what setting the version will never roll back.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches
        [InlineData(0,                          null)]
        [InlineData(0,                          false)]
        [InlineData(1,                          null)]
        [InlineData(1,                          false)]
        [InlineData(2,                          null)]
        [InlineData(2,                          false)]
        public void NeverRollBackOnRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            string requestedVersion = "5.1.4";
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, requestedVersion))
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        // Verifies that if both rollForwardOnNoCandidateFx=0 and applyPatches=0 there will be no rolling forward.
        [Fact]
        public void RollForwardDisabledOnCandidateFxAndDisabledApplyPatches_FailsToRollPatches()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(0)
                    .WithApplyPatches(false)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0"))
                .Should().Fail()
                .And.HaveStdErrContaining("Did not roll forward because apply_patches=0, version_compatibility_range=patch chose [5.1.0]");
        }

        // Verifies that if both rollForwardOnNoCandidateFx=0 and applyPatches=0 there can still resolve exact match
        [Fact]
        public void RollForwardDisabledOnCandidateFxAndDisabledApplyPatches_MatchesExact()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(0)
                    .WithApplyPatches(false)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
        }

        // Verifies that if rollForwardOnNoCandidateFx=0 (and applyPatches=<default> that is true)
        // the product will fail to roll on minor, but will try.
        [Fact]
        public void RollForwardOnMinorDisabledOnNoCandidateFx_FailsToRoll()
        {
            string requestedVersion = "5.0.0";
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(0)
                    .WithFramework(MicrosoftNETCoreApp, requestedVersion))
                // Will still attempt roll forward to latest patch
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion)
                .And.HaveStdErrContaining("Attempting FX roll forward");
        }

        // 3.0 change: In 2.* pre-release never rolled to release. In 3.* it will follow normal roll-forward rules.
        [Fact]
        public void PreReleaseReference_CanRollToRelease()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0-preview.1"))
                .Should().Pass()
                .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
        }

        private CommandResult RunTestWithOneFramework(Func<RuntimeConfig, RuntimeConfig> runtimeConfig)
        {
            return RunTest(
                SharedState.DotNetWithOneFramework,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig));
        }
        #endregion

        #region With one pre-release framework
        // RunTestWithPreReleaseFramework
        //   dotnet with
        //     - Microsoft.NETCore.App 5.1.3-preview.2

        // Verifies that exact match for pre-release version picks that version by default.
        [Fact]
        public void ExactMatchOnPreRelease_NoSettings()
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.2"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
        }

        // 3.0 change:
        // 2.* - Pre-Release only rolls on the exact same major.minor.patch (it only rolls over the pre-release portion of the version)
        // 3.* - Pre-Release follows normal roll-forward rules, including rolling over patches
        [Fact]
        public void RollForwardToPreRelease_CanRollOnPatch()
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.2-preview.2"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches work as expected.
        // Rolling from 5.1.3-preview.1 to 5.1.3-preview.2 version. So roll on pre-release version.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches
        [InlineData(null,                       null)]
        // 3.0 change:
        // 2.* - Pre-Release ignores rollForwardOnNoCandidateFX and applyPatches settings
        // 3.* - Pre-Release follows normal roll-forward rules, including all the roll-forward settings
        //   with the exception of applyPatches=false for pre-release roll.
        [InlineData(0,                          false)]
        [InlineData(2,                          true)]
        public void RollForwardToPreRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.1"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches work as expected.
        // Rolling from release 5.1.0 to pre-release 5.1.3-preview.2 version. So roll on patch version.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches
        [InlineData(null,                       null)]
        // This is a different behavior in 3.0. In 2.* the app would fail in this case as it was explicitly disallowed
        // to roll forward from release to pre-release when rollForwardOnNoCandidateFx=0 (and only then).
        [InlineData(0,                          null)]
        [InlineData(1,                          null)]
        [InlineData(1,                          false)]
        [InlineData(2,                          null)]
        [InlineData(2,                          false)]
        public void RollForwardToPreReleaseLatestPatch_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches work as expected.
        // Rolling from release 5.0.0 to pre-release 5.1.3-preview.2 version. So roll on minor version.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  passes
        [InlineData(null,                       null,         true)]
        [InlineData(null,                       false,        true)]
        [InlineData(0,                          null,         false)]
        [InlineData(1,                          null,         true)]
        [InlineData(1,                          false,        true)]
        [InlineData(2,                          null,         true)]
        [InlineData(2,                          false,        true)]
        public void RollForwardToPreReleaseOnMinor_RollForwardOnNoCandidateFx(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            bool passes)
        {
            string requestedVersion = "5.0.0";
            CommandResult result = RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, requestedVersion));
            if (passes)
            {
                result.ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
            }
            else
            {
                result.ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
            }
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches work as expected.
        // Rolling from release 4.1.0 to pre-release 5.1.3-preview.2 version. So roll on major version.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  passes
        [InlineData(null,                       null,         false)]
        [InlineData(0,                          null,         false)]
        [InlineData(1,                          null,         false)]
        [InlineData(1,                          false,        false)]
        [InlineData(2,                          null,         true)]
        [InlineData(2,                          false,        true)]
        public void RollForwardToPreReleaseOnMajor_RollForwardOnNoCandidateFx(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            bool passes)
        {
            string requestedVersion = "4.1.0";
            CommandResult result = RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, requestedVersion));
            if (passes)
            {
                result.ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
            }
            else
            {
                result.ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
            }
        }

        // Verifies that the produce never rolls back even on pre-release versions
        [Theory] // rollForwardOnNoCandidateFx  applyPatches
        [InlineData(0,                          null)]
        [InlineData(0,                          false)]
        [InlineData(1,                          null)]
        [InlineData(1,                          false)]
        [InlineData(2,                          null)]
        [InlineData(2,                          false)]
        public void NeverRollBackOnPreRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            string requestedVersion = "5.1.3-preview.9";
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, requestedVersion))
                .ShouldFailToFindCompatibleFrameworkVersion(MicrosoftNETCoreApp, requestedVersion);
        }

        private CommandResult RunTestWithPreReleaseFramework(Func<RuntimeConfig, RuntimeConfig> runtimeConfig)
        {
            return RunTest(
                SharedState.DotNetWithPreReleaseFramework,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig));
        }
        #endregion

        #region With many versions
        // RunWithManyVersions has these frameworks
        //  - Microsoft.NETCore.App 2.3.1-preview.1
        //  - Microsoft.NETCore.App 2.3.2
        //  - Microsoft.NETCore.App 4.1.1
        //  - Microsoft.NETCore.App 4.1.2
        //  - Microsoft.NETCore.App 4.1.3-preview.1
        //  - Microsoft.NETCore.App 4.2.1
        //  - Microsoft.NETCore.App 4.5.1-preview.1
        //  - Microsoft.NETCore.App 4.5.2
        //  - Microsoft.NETCore.App 5.1.3-preview.1
        //  - Microsoft.NETCore.App 5.1.3-preview.2
        //  - Microsoft.NETCore.App 5.1.4-preview.1
        //  - Microsoft.NETCore.App 5.2.3-preview.1
        //  - Microsoft.NETCore.App 5.2.3-preview.2
        //  - Microsoft.NETCore.App 6.1.1
        //  - Microsoft.NETCore.App 6.1.2-preview.1
        //  - Microsoft.NETCore.App 7.1.1-preview.1
        //  - Microsoft.NETCore.App 7.1.2-preview.1

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a release version 4.1.1 to the latest patch if allowed.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         "4.1.2")]
        [InlineData(null,                       false,        "4.1.1")]
        [InlineData(0,                          null,         "4.1.2")]
        [InlineData(0,                          false,        "4.1.1")]  // No roll forward
        [InlineData(1,                          null,         "4.1.2")]
        [InlineData(1,                          false,        "4.1.1")]  // Doesn't roll to latest patch
        [InlineData(2,                          null,         "4.1.2")]
        [InlineData(2,                          false,        "4.1.1")]  // Doesn't roll to latest patch
        public void RollForwardToLatestPatch_PicksLatestReleasePatch(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.1.1"))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a release version 4.0.0 the closest minor version with the latest patch.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         "4.1.2")]
        [InlineData(null,                       false,        "4.1.1")]
        [InlineData(0,                          null,         ResolvedFramework.NotFound)]
        [InlineData(0,                          false,        ResolvedFramework.NotFound)]
        [InlineData(1,                          null,         "4.1.2")]
        [InlineData(1,                          false,        "4.1.1")]  // Rolls to nearest higher even on patches, but not to latest patch.
        [InlineData(2,                          null,         "4.1.2")]
        [InlineData(2,                          false,        "4.1.1")]  // Rolls to nearest higher even on patches, but not to latest patch.
        public void RollForwardOnMinor_PicksLatestReleasePatch(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a release version 4.4.0 over the pre-release 4.5.1-preview.1 to the closest release minor version with the latest patch.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         "4.5.2")]
        [InlineData(null,                       false,        "4.5.2")]
        [InlineData(0,                          null,         ResolvedFramework.NotFound)]
        [InlineData(0,                          false,        ResolvedFramework.NotFound)]
        [InlineData(1,                          null,         "4.5.2")]
        [InlineData(1,                          false,        "4.5.2")]
        [InlineData(2,                          null,         "4.5.2")]
        [InlineData(2,                          false,        "4.5.2")]
        public void RollForwardOnMinor_RollOverPreRelease(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.4.0"))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a release version 3.0.0 to the closest release major version with the latest patch.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         ResolvedFramework.NotFound)]
        [InlineData(null,                       false,        ResolvedFramework.NotFound)]
        [InlineData(0,                          null,         ResolvedFramework.NotFound)]
        [InlineData(0,                          false,        ResolvedFramework.NotFound)]
        [InlineData(1,                          null,         ResolvedFramework.NotFound)]
        [InlineData(1,                          false,        ResolvedFramework.NotFound)]
        [InlineData(2,                          null,         "4.1.2")]
        [InlineData(2,                          false,        "4.1.1")]  // Rolls to nearest higher even on patches, but not to latest patch.
        public void RollForwardOnMajor_PicksLatestReleasePatch(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "3.0.0"))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a release version 5.1.2 to the closest patch pre-release version (since there's no release available)
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         "5.1.3-preview.1")]
        [InlineData(null,                       false,        "5.1.3-preview.1")]
        // This is a different behavior in 3.0. In 2.* the app would fail in this case as it was explicitly disallowed
        // to roll forward from release to pre-release when rollForwardOnNoCandidateFx=0 (and only then).
        [InlineData(0,                          null,         "5.1.3-preview.1")]
        [InlineData(0,                          false,        ResolvedFramework.NotFound)]
        [InlineData(1,                          null,         "5.1.3-preview.1")]
        [InlineData(1,                          false,        "5.1.3-preview.1")]
        [InlineData(2,                          null,         "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        [InlineData(2,                          false,        "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        public void RollForwardToPreReleaseToLatestPatch_FromRelease(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.2"))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a release version 5.0.0 to the closest minor.patch pre-release version (since there's no release available)
        // No roll to latest patch is applied since this is pre-release version only.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         "5.1.3-preview.1")]
        [InlineData(null,                       false,        "5.1.3-preview.1")]
        [InlineData(0,                          null,         ResolvedFramework.NotFound)]
        [InlineData(0,                          false,        ResolvedFramework.NotFound)]
        [InlineData(1,                          null,         "5.1.3-preview.1")]
        [InlineData(1,                          false,        "5.1.3-preview.1")]
        [InlineData(2,                          null,         "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        [InlineData(2,                          false,        "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        public void RollForwardToPreReleaseOnMinor_FromRelease(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"))
                .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a release version 6.2.0 to the closest major.minor.patch pre-release version (since there's no release available)
        // No roll to latest patch is applied since this is pre-release version only.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         ResolvedFramework.NotFound)]
        [InlineData(null,                       false,        ResolvedFramework.NotFound)]
        [InlineData(0,                          null,         ResolvedFramework.NotFound)]
        [InlineData(0,                          false,        ResolvedFramework.NotFound)]
        [InlineData(1,                          null,         ResolvedFramework.NotFound)]
        [InlineData(1,                          false,        ResolvedFramework.NotFound)]
        [InlineData(2,                          null,         "7.1.1-preview.1")]
        [InlineData(2,                          false,        "7.1.1-preview.1")]
        public void RollForwardToPreReleaseOnMajor_FromRelease(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "6.2.0"))
               .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a release version 5.2.2 to the closest patch pre-release version (since there's no release available)
        // No roll to latest patch is applied since this is pre-release version only.
        // Both 5.2.3-preview.1 and 5.2.3-preview.2 are available
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         "5.2.3-preview.1")]
        [InlineData(null,                       false,        "5.2.3-preview.1")]
        // This is a different behavior in 3.0. In 2.* the app would fail in this case as it was explicitly disallowed
        // to roll forward from release to pre-release when rollForwardOnNoCandidateFx=0 (and only then).
        [InlineData(0,                          null,         "5.2.3-preview.1")]
        [InlineData(0,                          false,        ResolvedFramework.NotFound)]
        [InlineData(1,                          null,         "5.2.3-preview.1")]
        [InlineData(1,                          false,        "5.2.3-preview.1")]
        [InlineData(2,                          null,         "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        [InlineData(2,                          false,        "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        public void RollForwardToPreReleaseToClosestPreRelease_FromRelease(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.2.2"))
               .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a release version 2.3.0 to the closest release (and latest patch) over pre-release versions
        // Both 2.3.1-preview.1 and 2.3.2 are available
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         "2.3.2")]
        [InlineData(null,                       false,        "2.3.2")]
        [InlineData(0,                          null,         "2.3.2")]  // Pre-release is ignored, roll forward to latest release patch
        [InlineData(0,                          false,        ResolvedFramework.NotFound)] // No exact match available
        [InlineData(1,                          null,         "2.3.2")]
        [InlineData(1,                          false,        "2.3.2")] // Pre-release is ignored, roll forward to closest release available
        [InlineData(2,                          null,         "2.3.2")]
        [InlineData(2,                          false,        "2.3.2")] // Pre-release is ignored, roll forward to closest release available
        public void RollForwardToClosestReleaseWithPreReleaseAvailable_FromRelease(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "2.3.0"))
               .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a pre-release version 5.1.1-preview.1 to another pre-release - latest patch.
        // 3.0 change:
        // 2.* - Pre-release will only match the exact x.y.z version, regardless of settings
        // 3.* - Pre-release uses normal roll forward rules, including rolling over minor/patches and obeying settings but does not roll to latest patch.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         "5.1.3-preview.1")]
        [InlineData(0,                          false,        ResolvedFramework.NotFound)]  // Roll-forward fully disabled
        [InlineData(1,                          null,         "5.1.3-preview.1")]
        [InlineData(2,                          null,         "5.1.3-preview.1")]
        public void RollForwardToPreRelease_FromDifferentPreRelease(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1-preview.1"))
               .ShouldHaveResolvedFrameworkOrFailToFind(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a pre-release version 5.1.3-preview.1 which exists - this no roll-forward should occur.
        // 3.0 change:
        // 2.* - Pre-release with exact match will not try to roll forward at all
        // 3.* - Pre-release uses normal roll forward rules, it will roll forward on patches even on exact match.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         "5.1.3-preview.1")]
        [InlineData(null,                       false,        "5.1.3-preview.1")]
        [InlineData(0,                          false,        "5.1.3-preview.1")]
        [InlineData(1,                          null,         "5.1.3-preview.1")]
        [InlineData(2,                          null,         "5.1.3-preview.1")]
        public void RollForwardToPreRelease_ExactPreReleaseMatch(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.1"))
               .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a pre-release version 5.1.3-preview.0 which doesn't exists to another pre-release, but closest.
        [Theory] // rollForwardOnNoCandidateFx  applyPatches  resolvedFramework
        [InlineData(null,                       null,         "5.1.3-preview.1")]
        [InlineData(null,                       false,        "5.1.3-preview.1")]
        [InlineData(0,                          false,        "5.1.3-preview.1")]
        [InlineData(1,                          null,         "5.1.3-preview.1")]
        [InlineData(2,                          null,         "5.1.3-preview.1")]
        public void RollForwardToPreRelease_FromSamePreRelease(
            int? rollForwardOnNoCandidateFx,
            bool? applyPatches,
            string resolvedFramework)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.0"))
               .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, resolvedFramework);
        }

        // Verifies that rollForwardOnNoCandidateFx and applyPatches settings correctly roll
        // from a release version 6.1.0 to another release version.
        // When rolling from release, pre-release is ignored if any release which matches can be found
        // 6.1.1 and 6.1.2-preview.1 is available so pure latest patch should pick the 6.1.2-preview.1
        // but release is preferred if available.
        [Theory] // rollForwardOnNoCandidateFx
        [InlineData(null)]
        [InlineData(1)]
        [InlineData(2)]
        public void RollForwardToLatestPatch_WithHigherPreReleasePresent(int? rollForwardOnNoCandidateFx)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithFramework(MicrosoftNETCoreApp, "6.1.0"))
                .ShouldHaveResolvedFramework(MicrosoftNETCoreApp, "6.1.1");
        }

        private CommandResult RunTestWithManyVersions(Func<RuntimeConfig, RuntimeConfig> runtimeConfig)
        {
            return RunTest(
                SharedState.DotNetWithManyVersions,
                SharedState.FrameworkReferenceApp,
                new TestSettings()
                    .WithRuntimeConfigCustomizer(runtimeConfig));
        }
        #endregion
    }
}
