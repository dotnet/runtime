// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        #region With one release framework
        // RunTestWithOneFramework
        //   dotnet with
        //     - Microsoft.NETCore.App 5.1.3

        [Fact]
        public void ExactMatchOnRelease_NoSettings()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(0, null)]
        [InlineData(1, null)]
        [InlineData(1, false)]  // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0
        [InlineData(2, null)]
        [InlineData(2, false)]  // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0
        public void RollForwardToLatestPatch_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(null, false, true)]
        [InlineData(0, null, false)]
        [InlineData(1, null, true)]
        [InlineData(1, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0
        [InlineData(2, null, true)]
        [InlineData(2, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0
        public void RollForwardOnMinor_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                commandResult =>
                {
                    if (passes)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData(0, null, false)]
        [InlineData(1, null, false)]
        [InlineData(1, false, false)]
        [InlineData(2, null, true)]
        [InlineData(2, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0
        public void RollForwardOnMajor_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.1.0"),
                commandResult =>
                {
                    if (passes)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3");
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Fact]
        public void RollForwardDisabledOnCandidateFxAndDisabledApplyPatches_FailsToRollPatches()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(0)
                    .WithApplyPatches(false)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0"),
                commandResult => commandResult.Should().Fail()
                    .And.HaveStdErrContaining("Did not roll forward because patch_roll_fwd=0, roll_fwd_on_no_candidate_fx=0, use_exact_version=0 chose [5.1.0]"));
        }

        [Fact]
        public void RollForwardDisabledOnCandidateFxAndDisabledApplyPatches_MatchesExact()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(0)
                    .WithApplyPatches(false)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3"));
        }

        [Fact]
        public void RollForwardOnMinorDisabledOnNoCandidateFx_FailsToRoll()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(0)
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                // Will still attempt roll forward to latest patch
                commandResult => commandResult.Should().Fail()
                    .And.HaveStdErrContaining("Attempting FX roll forward")
                    .And.DidNotFindCompatibleFrameworkVersion());
        }

        [Fact]
        public void PreReleaseReference_FailsToRollToRelease()
        {
            RunTestWithOneFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0-preview.1"),
                commandResult => commandResult.Should().Fail()
                    .And.DidNotFindCompatibleFrameworkVersion());
        }

        private void RunTestWithOneFramework(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction)
        {
            RunTest(SharedState.DotNetWithOneFramework, runtimeConfig, resultAction);
        }
        #endregion

        #region With one pre-release framework
        // RunTestWithPreReleaseFramework
        //   dotnet with
        //     - Microsoft.NETCore.App 5.1.3-preview.2

        [Fact]
        public void ExactMatchOnPreRelease_NoSettings()
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.2"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2"));
        }

        [Fact]
        public void RollForwardToPreRelease_FailsOnVersionMismatch()
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithFramework(MicrosoftNETCoreApp, "5.1.2-preview.2"),
                commandResult => commandResult.Should().Fail()
                    .And.DidNotFindCompatibleFrameworkVersion());
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(0, false)] // Pre-Release ignores roll forward on no candidate FX and apply patches settings
        [InlineData(2, true)]
        public void RollForwardToPreRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.1"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2"));
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(0, null, false)]  // Roll forward to pre-release on patch from release is blocked
        [InlineData(1, null, true)]
        [InlineData(1, false, true)]  // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0
        [InlineData(2, null, true)]
        [InlineData(2, false, true)]  // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0
        public void RollForwardToPreReleaseLatestPatch_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.0"),
                commandResult =>
                {
                    if (passes)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(null, false, true)]
        [InlineData(0, null, false)]
        [InlineData(1, null, true)]
        [InlineData(1, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0
        [InlineData(2, null, true)]
        [InlineData(2, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0
        public void RollForwardToPreReleaseOnMinor_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                commandResult =>
                {
                    if (passes)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData(0, null, false)]
        [InlineData(1, null, false)]
        [InlineData(1, false, false)]
        [InlineData(2, null, true)]
        [InlineData(2, false, true)] // Rolls on patches even when applyPatches = false if rollForwardOnNoCandidateFx != 0
        public void RollForwardToPreReleaseOnMajor_RollForwardOnNoCandidateFx(int? rollForwardOnNoCandidateFx, bool? applyPatches, bool passes)
        {
            RunTestWithPreReleaseFramework(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.1.0"),
                commandResult =>
                {
                    if (passes)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, "5.1.3-preview.2");
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        private void RunTestWithPreReleaseFramework(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction)
        {
            RunTest(SharedState.DotNetWithPreReleaseFramework, runtimeConfig, resultAction);
        }
        #endregion

        #region With many versions
        // RunWithManyVersions has these frameworks
        //  - Microsoft.NETCore.App 4.1.1
        //  - Microsoft.NETCore.App 4.1.2
        //  - Microsoft.NETCore.App 4.1.3-preview.1
        //  - Microsoft.NETCore.App 4.2.1
        //  - Microsoft.NETCore.App 5.1.3-preview.1
        //  - Microsoft.NETCore.App 5.1.3-preview.2
        //  - Microsoft.NETCore.App 5.1.4-preview.1
        //  - Microsoft.NETCore.App 5.2.3-preview.1
        //  - Microsoft.NETCore.App 6.1.1
        //  - Microsoft.NETCore.App 6.1.2-preview.1
        //  - Microsoft.NETCore.App 7.1.1-preview.1
        //  - Microsoft.NETCore.App 7.1.2-preview.1

        [Theory]
        [InlineData(null, null, "4.1.2")]
        [InlineData(null, false, "4.1.1")]
        [InlineData(0, null, "4.1.2")]
        [InlineData(0, false, "4.1.1")]
        [InlineData(1, null, "4.1.2")]
        [InlineData(1, false, "4.1.1")]
        [InlineData(2, null, "4.1.2")]
        [InlineData(2, false, "4.1.1")]
        public void RollForwardToLatestPatch_PicksLatestReleasePatch(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.1.1"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion));
        }

        [Theory]
        [InlineData(null, null, "4.1.2")]
        [InlineData(null, false, "4.1.1")]
        [InlineData(0, null, null)]
        [InlineData(0, false, null)]
        [InlineData(1, null, "4.1.2")]
        [InlineData(1, false, "4.1.1")]
        [InlineData(2, null, "4.1.2")]
        [InlineData(2, false, "4.1.1")]
        public void RollForwardOnMinor_PicksLatestReleasePatch(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, false, null)]
        [InlineData(0, null, null)]
        [InlineData(0, false, null)]
        [InlineData(1, null, null)]
        [InlineData(1, false, null)]
        [InlineData(2, null, "4.1.2")]
        [InlineData(2, false, "4.1.1")]
        public void RollForwardOnMajor_PicksLatestReleasePatch(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "3.0.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, "5.1.4-preview.1")]
        [InlineData(null, false, "5.1.3-preview.1")]
        [InlineData(0, null, null)]   // This is interesting - we prevent roll forward from release to preview on patch alone
        [InlineData(0, false, null)]
        [InlineData(1, null, "5.1.4-preview.1")]
        [InlineData(1, false, "5.1.3-preview.1")]
        [InlineData(2, null, "6.1.1")]   // Not really testing the pre-release roll forward, but valid test anyway
        [InlineData(2, false, "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        public void RollForwardToPreReleaseToLatestPatch_FromRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.2"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, "5.1.4-preview.1")]
        [InlineData(null, false, "5.1.3-preview.1")]
        [InlineData(0, null, null)]
        [InlineData(0, false, null)]
        [InlineData(1, null, "5.1.4-preview.1")]
        [InlineData(1, false, "5.1.3-preview.1")]
        [InlineData(2, null, "6.1.1")]   // Not really testing the pre-release roll forward, but valid test anyway
        [InlineData(2, false, "6.1.1")]  // Not really testing the pre-release roll forward, but valid test anyway
        public void RollForwardToPreReleaseOnMinor_FromRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.0.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, false, null)]
        [InlineData(0, null, null)]
        [InlineData(0, false, null)]
        [InlineData(1, null, null)]
        [InlineData(1, false, null)]
        [InlineData(2, null, "7.1.2-preview.1")]
        [InlineData(2, false, "7.1.1-preview.1")]
        public void RollForwardToPreReleaseOnMajor_FromRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "6.2.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, null)]   // Pre-release will only match the extact x.y.z version, regardless of settings
        [InlineData(0, false, null)]
        [InlineData(1, null, null)]
        [InlineData(2, null, null)]
        public void RollForwardToPreRelease_FromDifferentPreRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.1-preview.1"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null, null, "5.1.3-preview.1")]   // Pre-release will select the closest higher version
        [InlineData(null, false, "5.1.3-preview.1")]
        [InlineData(0, false, "5.1.3-preview.1")]
        [InlineData(1, null, "5.1.3-preview.1")]
        [InlineData(2, null, "5.1.3-preview.1")]
        public void RollForwardToPreRelease_FromSamePreRelease(int? rollForwardOnNoCandidateFx, bool? applyPatches, string resolvedVersion)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithApplyPatches(applyPatches)
                    .WithFramework(MicrosoftNETCoreApp, "5.1.3-preview.0"),
                commandResult =>
                {
                    if (resolvedVersion != null)
                    {
                        commandResult.Should().Pass()
                            .And.HaveResolvedFramework(MicrosoftNETCoreApp, resolvedVersion);
                    }
                    else
                    {
                        commandResult.Should().Fail()
                            .And.DidNotFindCompatibleFrameworkVersion();
                    }
                });
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        [InlineData(2)]
        public void RollForwardToLatestPatch_WithHigherPreReleasePresent(int? rollForwardOnNoCandidateFx)
        {
            RunTestWithManyVersions(
                runtimeConfig => runtimeConfig
                    .WithRollForwardOnNoCandidateFx(rollForwardOnNoCandidateFx)
                    .WithFramework(MicrosoftNETCoreApp, "6.1.0"),
                commandResult => commandResult.Should().Pass()
                    .And.HaveResolvedFramework(MicrosoftNETCoreApp, "6.1.1"));
        }



        private void RunTestWithManyVersions(
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction)
        {
            RunTest(SharedState.DotNetWithManyVersions, runtimeConfig, resultAction);
        }
        #endregion

        private void RunTest(
            DotNetCli dotNet,
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction)
        {
            RunTest(
                dotNet,
                SharedState.FrameworkReferenceApp,
                runtimeConfig,
                resultAction);
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
                    .AddMicrosoftNETCoreAppFramework("5.1.3")
                    .Build();

                DotNetWithPreReleaseFramework = DotNet("WithPreReleaseFramework")
                    .AddMicrosoftNETCoreAppFramework("5.1.3-preview.2")
                    .Build();

                DotNetWithManyVersions = DotNet("WithManyVersions")
                    .AddMicrosoftNETCoreAppFramework("4.1.1")
                    .AddMicrosoftNETCoreAppFramework("4.1.2")
                    .AddMicrosoftNETCoreAppFramework("4.1.3-preview.1")
                    .AddMicrosoftNETCoreAppFramework("4.2.1")
                    .AddMicrosoftNETCoreAppFramework("5.1.3-preview.1")
                    .AddMicrosoftNETCoreAppFramework("5.1.3-preview.2")
                    .AddMicrosoftNETCoreAppFramework("5.1.4-preview.1")
                    .AddMicrosoftNETCoreAppFramework("5.2.3-preview.1")
                    .AddMicrosoftNETCoreAppFramework("6.1.1")
                    .AddMicrosoftNETCoreAppFramework("6.1.2-preview.1")
                    .AddMicrosoftNETCoreAppFramework("7.1.1-preview.1")
                    .AddMicrosoftNETCoreAppFramework("7.1.2-preview.1")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp();
            }
        }
    }
}
