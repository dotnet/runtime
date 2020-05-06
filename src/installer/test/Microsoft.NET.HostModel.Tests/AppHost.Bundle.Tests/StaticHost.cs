// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.Bundle;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class StaticHost : IClassFixture<StaticHost.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public StaticHost(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        // This helper is used in lieu of SDK support for publishing apps using the singlefilehost.
        // It replaces the apphost with singlefilehost, and along with appropriate app.dll updates in the host.
        // For now, we leave behind the hostpolicy and hostfxr DLLs in the publish directory, because
        // removing them requires deps.json update.
        void ReplaceApphostWithStaticHost(TestProjectFixture fixture)
        {
            var staticHost = Path.Combine(fixture.RepoDirProvider.HostArtifacts,
                                          RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("singlefilehost"));
            HostWriter.CreateAppHost(staticHost,
                                     BundleHelper.GetHostPath(fixture),
                                     BundleHelper.GetAppPath(fixture));

        }

        [Fact]
        private void Can_Run_App_With_StatiHost()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var appExe = BundleHelper.GetHostPath(fixture);

            ReplaceApphostWithStaticHost(fixture);

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }

        [Fact]
        private void Can_Run_SingleFile_App_With_StatiHost()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            ReplaceApphostWithStaticHost(fixture);

            string singleFile = BundleHelper.BundleApp(fixture);

            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();
                TestFixture = new TestProjectFixture("StaticHostApp", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid, 
                                    outputDirectory: BundleHelper.GetPublishPath(TestFixture));
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
