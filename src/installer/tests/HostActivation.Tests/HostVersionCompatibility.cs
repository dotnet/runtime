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
        public void LatestHost_OldRuntime_BackwardsCompatible_60()
        {
            LatestHost_OldRuntime_BackwardsCompatible(sharedTestState.Fixture60);
        }

        private void LatestHost_OldRuntime_BackwardsCompatible(TestProjectFixture previousVersionFixture)
        {
            TestProjectFixture fixture = previousVersionFixture.Copy();
            string appExe = fixture.TestProject.AppExe;

            Assert.NotEqual(fixture.Framework, RepoDirectoriesProvider.Default.Tfm);
            Assert.NotEqual(fixture.RepoDirProvider.MicrosoftNETCoreAppVersion, RepoDirectoriesProvider.Default.MicrosoftNETCoreAppVersion);

            // Use the newer apphost
            // This emulates the case when:
            //  1) Newer runtime installed
            //  2) Newer runtime uninstalled (installer preserves newer apphost)
            fixture.TestProject.BuiltApp.CreateAppHost();
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {RepoDirectoriesProvider.Default.MicrosoftNETCoreAppVersion}");

            // Use the newer apphost and hostFxr
            // This emulates the case when:
            //  1) Newer runtime installed
            //  2) A roll-forward to the newer runtime did not occur
            File.Copy(Binaries.HostFxr.FilePath, fixture.TestProject.HostFxrDll, true);
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {RepoDirectoriesProvider.Default.MicrosoftNETCoreAppVersion}");
        }

        [Fact]
        public void OldHost_LatestRuntime_ForwardCompatible_60()
        {
            OldHost_LatestRuntime_ForwardCompatible(sharedTestState.Fixture60);
        }

        private void OldHost_LatestRuntime_ForwardCompatible(TestProjectFixture previousVersionFixture)
        {
            TestApp app = sharedTestState.AppLatest.Copy();
            string appExe = app.AppExe;

            Assert.NotEqual(RepoDirectoriesProvider.Default.Tfm, previousVersionFixture.Framework);
            Assert.NotEqual(RepoDirectoriesProvider.Default.MicrosoftNETCoreAppVersion, previousVersionFixture.RepoDirProvider.MicrosoftNETCoreAppVersion);

            // Use the older apphost
            // This emulates the case when:
            //  1) Newer runtime installed
            //  2) App rolls forward to newer runtime
            File.Copy(previousVersionFixture.TestProject.AppExe, appExe, true);
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {previousVersionFixture.RepoDirProvider.MicrosoftNETCoreAppVersion}");

            // Use the older apphost and hostfxr
            // This emulates the case when:
            //  1) One-off deployment of older runtime (not in global location)
            //  2) Older apphost executed, but found newer runtime because of multi-level lookup on Windows
            //     Note that we don't have multi-level on hostfxr so we will always find the older\one-off hostfxr
            if (OperatingSystem.IsWindows())
            {
                File.Copy(previousVersionFixture.TestProject.HostFxrDll, app.HostFxrDll, true);
                Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World")
                    .And.HaveStdErrContaining($"--- Invoked apphost [version: {previousVersionFixture.RepoDirProvider.MicrosoftNETCoreAppVersion}");
            }
        }

        public class SharedTestState : IDisposable
        {
            private static RepoDirectoriesProvider RepoDirectories { get; set; }

            public TestProjectFixture Fixture60 { get; }
            public TestApp AppLatest { get; }

            private const string AppName = "HelloWorld";

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                Fixture60 = CreateTestFixture("StandaloneApp6x", "net6.0", "6.0");

                AppLatest = TestApp.CreateFromBuiltAssets(AppName);
                AppLatest.PopulateSelfContained(TestApp.MockedComponent.None);
            }

            public void Dispose()
            {
                Fixture60.Dispose();
                AppLatest?.Dispose();
            }

            private static TestProjectFixture CreateTestFixture(string testName, string netCoreAppFramework, string mnaVersion)
            {
                var repoDirectories = new RepoDirectoriesProvider(microsoftNETCoreAppVersion: mnaVersion);

                // Use standalone instead of framework-dependent for ease of deployment.
                var publishFixture = new TestProjectFixture(testName, repoDirectories, framework: netCoreAppFramework, assemblyName: AppName);
                publishFixture
                    .EnsureRestoredForRid(publishFixture.CurrentRid)
                    .PublishProject(runtime: publishFixture.CurrentRid, selfContained: true, extraArgs: $"/p:AssemblyName={AppName}");

                return publishFixture;
            }
        }
    }
}
