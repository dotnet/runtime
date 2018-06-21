// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.CoreSetup.Test.HostActivation.StandaloneApp;
using Microsoft.DotNet.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.VersionCompatibility
{
    public class GivenThatICareAboutHostVersionCompatibility
    {
        private static TestProjectFixture Fixture20 { get; set; }
        private static TestProjectFixture Fixture21 { get; set; }
        private static TestProjectFixture FixtureLatest { get; set; }

        static GivenThatICareAboutHostVersionCompatibility()
        {
            // If these versions are changed, also change the corresponding .csproj
            Fixture20 = CreateTestFixture("StandaloneApp20", "netcoreapp2.0", "2.0.7");
            Fixture21 = CreateTestFixture("StandaloneApp21", "netcoreapp2.1", "2.1.0");
            FixtureLatest = GivenThatICareAboutStandaloneAppActivation.PreviouslyPublishedAndRestoredStandaloneTestProjectFixture;
        }

        public static IEnumerable<object[]> PreviousVersions
        {
            get
            {
                yield return new object[] { Fixture20 };
                yield return new object[] { Fixture21 };
            }
        }

        [Theory]
        [MemberData(nameof(PreviousVersions))]
        public void Latest_Host_Is_Backwards_Compatible_With_Older_Runtime(TestProjectFixture previousVersionFixture)
        {
            if (!IsRidSupported())
            {
                return;
            }

            TestProjectFixture fixture = previousVersionFixture.Copy();
            string appExe = fixture.TestProject.AppExe;

            Assert.NotEqual(fixture.Framework, FixtureLatest.Framework);
            Assert.NotEqual(fixture.RepoDirProvider.MicrosoftNETCoreAppVersion, FixtureLatest.RepoDirProvider.MicrosoftNETCoreAppVersion);

            // Baseline (no changes)
            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World")
                .And
                .HaveStdErrContaining($"--- Invoked apphost [version: {fixture.RepoDirProvider.MicrosoftNETCoreAppVersion}");

            // Use the newer apphost
            // This emulates the case when:
            //  1) Newer runtime installed
            //  2) Newer runtime uninstalled (installer preserves newer apphost)
            File.Copy(FixtureLatest.TestProject.AppExe, fixture.TestProject.AppExe, true);
            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World")
                .And
                .HaveStdErrContaining($"--- Invoked apphost [version: {FixtureLatest.RepoDirProvider.MicrosoftNETCoreAppVersion}");

            // Use the newer apphost and hostFxr
            // This emulates the case when:
            //  1) Newer runtime installed
            //  2) A roll-forward to the newer runtime did not occur
            File.Copy(FixtureLatest.TestProject.HostFxrDll, fixture.TestProject.HostFxrDll, true);
            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World")
                .And
                .HaveStdErrContaining($"--- Invoked apphost [version: {FixtureLatest.RepoDirProvider.MicrosoftNETCoreAppVersion}");
        }

        [Theory]
        [MemberData(nameof(PreviousVersions))]
        public void Old_Host_Is_Forward_Compatible_With_Latest_Runtime(TestProjectFixture previousVersionFixture)
        {
            if (!IsRidSupported())
            {
                return;
            }

            TestProjectFixture fixture = FixtureLatest.Copy();
            string appExe = fixture.TestProject.AppExe;

            Assert.NotEqual(fixture.Framework, previousVersionFixture.Framework);
            Assert.NotEqual(fixture.RepoDirProvider.MicrosoftNETCoreAppVersion, previousVersionFixture.RepoDirProvider.MicrosoftNETCoreAppVersion);

            // Baseline (no changes)
            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World")
                .And
                .HaveStdErrContaining($"--- Invoked apphost [version: {fixture.RepoDirProvider.MicrosoftNETCoreAppVersion}");

            // Use the older apphost and hostfxr
            // This emulates the case when:
            //  1) One-off deployment of older runtime (not in global location)
            //  2) Older apphost executed, but found newer runtime because of multi-level lookup
            //     Note that we currently don't have multi-level on hostfxr so we will always find the older\one-off hostfxr
            File.Copy(previousVersionFixture.TestProject.AppExe, fixture.TestProject.AppExe, true);
            File.Copy(previousVersionFixture.TestProject.HostFxrDll, fixture.TestProject.HostFxrDll, true);
            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World")
                .And
                .HaveStdErrContaining($"--- Invoked apphost [version: {previousVersionFixture.RepoDirProvider.MicrosoftNETCoreAppVersion}");
        }

        private static TestProjectFixture CreateTestFixture(string testName, string netCoreAppFramework, string mnaVersion)
        {
            var repoDirectories = new RepoDirectoriesProvider(microsoftNETCoreAppVersion: mnaVersion);

            // Use standalone instead of framework-dependent for ease of deployment.
            var publishFixture = new TestProjectFixture(testName, repoDirectories, framework: netCoreAppFramework, assemblyName: "StandaloneApp");

            if (IsRidSupported())
            {
                publishFixture
                    .EnsureRestoredForRid(publishFixture.CurrentRid, repoDirectories.CorehostPackages)
                    .PublishProject(runtime: publishFixture.CurrentRid);
            }

            return publishFixture;
        }

        private static bool IsRidSupported()
        {
            Platform platform = RuntimeEnvironment.OperatingSystemPlatform;

            // Some current Linux RIDs are not supported in 2.0\2.1; just test for Ubuntu 16.
            return (
                platform == Platform.Windows ||
                platform == Platform.Darwin ||
                (platform == Platform.Linux && RuntimeEnvironment.GetRuntimeIdentifier() == "ubuntu.16.04-x64")
            );
        }
    }
}
