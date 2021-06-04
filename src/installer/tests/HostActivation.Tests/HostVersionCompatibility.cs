// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class HostVersionCompatibility : IClassFixture<HostVersionCompatibility.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public HostVersionCompatibility(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Latest_Host_Is_Backwards_Compatible_With_Older_Runtime_20()
        {
            Latest_Host_Is_Backwards_Compatible_With_Older_Runtime(sharedTestState.Fixture20);
        }

        [Fact]
        public void Latest_Host_Is_Backwards_Compatible_With_Older_Runtime_21()
        {
            Latest_Host_Is_Backwards_Compatible_With_Older_Runtime(sharedTestState.Fixture21);
        }

        private void Latest_Host_Is_Backwards_Compatible_With_Older_Runtime(TestProjectFixture previousVersionFixture)
        {
            if (!IsRidSupported())
            {
                return;
            }

            TestProjectFixture fixture = previousVersionFixture.Copy();
            string appExe = fixture.TestProject.AppExe;

            Assert.NotEqual(fixture.Framework, sharedTestState.FixtureLatest.Framework);
            Assert.NotEqual(fixture.RepoDirProvider.MicrosoftNETCoreAppVersion, sharedTestState.FixtureLatest.RepoDirProvider.MicrosoftNETCoreAppVersion);

            // Baseline (no changes)
            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {fixture.RepoDirProvider.MicrosoftNETCoreAppVersion}");

            // Use the newer apphost
            // This emulates the case when:
            //  1) Newer runtime installed
            //  2) Newer runtime uninstalled (installer preserves newer apphost)
            File.Copy(sharedTestState.FixtureLatest.TestProject.AppExe, fixture.TestProject.AppExe, true);
            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {sharedTestState.FixtureLatest.RepoDirProvider.MicrosoftNETCoreAppVersion}");

            // Use the newer apphost and hostFxr
            // This emulates the case when:
            //  1) Newer runtime installed
            //  2) A roll-forward to the newer runtime did not occur
            File.Copy(sharedTestState.FixtureLatest.TestProject.HostFxrDll, fixture.TestProject.HostFxrDll, true);
            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {sharedTestState.FixtureLatest.RepoDirProvider.MicrosoftNETCoreAppVersion}");
        }

        [Fact]
        public void Old_Host_Is_Forward_Compatible_With_Latest_Runtime_20()
        {
            Old_Host_Is_Forward_Compatible_With_Latest_Runtime(sharedTestState.Fixture20);
        }

        [Fact]
        public void Old_Host_Is_Forward_Compatible_With_Latest_Runtime_21()
        {
            Old_Host_Is_Forward_Compatible_With_Latest_Runtime(sharedTestState.Fixture21);
        }

        private void Old_Host_Is_Forward_Compatible_With_Latest_Runtime(TestProjectFixture previousVersionFixture)
        {
            if (!IsRidSupported())
            {
                return;
            }

            TestProjectFixture fixture = sharedTestState.FixtureLatest.Copy();
            string appExe = fixture.TestProject.AppExe;

            Assert.NotEqual(fixture.Framework, previousVersionFixture.Framework);
            Assert.NotEqual(fixture.RepoDirProvider.MicrosoftNETCoreAppVersion, previousVersionFixture.RepoDirProvider.MicrosoftNETCoreAppVersion);

            // Baseline (no changes)
            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {fixture.RepoDirProvider.MicrosoftNETCoreAppVersion}");

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
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {previousVersionFixture.RepoDirProvider.MicrosoftNETCoreAppVersion}");
        }

        private static bool IsRidSupported()
        {
            // Some current Linux RIDs are not supported in 2.0\2.1; just test for Ubuntu 16.
            return (
                OperatingSystem.IsWindows() ||
                OperatingSystem.IsMacOS() ||
                (OperatingSystem.IsLinux() && RuntimeInformation.RuntimeIdentifier == "ubuntu.16.04-x64")
            );
        }

        public class SharedTestState : IDisposable
        {
            private static RepoDirectoriesProvider RepoDirectories { get; set; }

            public TestProjectFixture Fixture20 { get; }
            public TestProjectFixture Fixture21 { get; }
            public TestProjectFixture FixtureLatest { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                // If these versions are changed, also change the corresponding .csproj
                Fixture20 = CreateTestFixture("StandaloneApp20", "netcoreapp2.0", "2.0.7");
                Fixture21 = CreateTestFixture("StandaloneApp21", "netcoreapp2.1", "2.1.0");

                var fixtureLatest = new TestProjectFixture("StandaloneApp", RepoDirectories);
                fixtureLatest
                    .EnsureRestoredForRid(fixtureLatest.CurrentRid)
                    .PublishProject(runtime: fixtureLatest.CurrentRid);

                FixtureLatest = fixtureLatest;
            }

            public void Dispose()
            {
                Fixture20.Dispose();
                Fixture21.Dispose();
                FixtureLatest.Dispose();
            }

            private static TestProjectFixture CreateTestFixture(string testName, string netCoreAppFramework, string mnaVersion)
            {
                var repoDirectories = new RepoDirectoriesProvider(microsoftNETCoreAppVersion: mnaVersion);

                // Use standalone instead of framework-dependent for ease of deployment.
                var publishFixture = new TestProjectFixture(testName, repoDirectories, framework: netCoreAppFramework, assemblyName: "StandaloneApp");

                if (IsRidSupported())
                {
                    publishFixture
                        .EnsureRestoredForRid(publishFixture.CurrentRid)
                        .PublishProject(runtime: publishFixture.CurrentRid);
                }

                return publishFixture;
            }
        }
    }
}
