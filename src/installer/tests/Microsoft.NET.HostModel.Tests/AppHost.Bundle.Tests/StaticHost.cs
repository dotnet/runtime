// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class StaticHost : BundleTestBase, IClassFixture<StaticHost.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public StaticHost(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        private void Can_Run_App_With_StaticHost()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var appExe = UseSingleFileSelfContainedHost(fixture);

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
        private void Can_Run_SingleFile_App_With_StaticHost()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            string singleFile = BundleSelfContainedApp(fixture);

            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            if (OperatingSystem.IsWindows())
            {
                // StandaloneApp sets FileVersion to NETCoreApp version. On Windows, this should be copied to singlefilehost resources.
                Assert.Equal(System.Diagnostics.FileVersionInfo.GetVersionInfo(singleFile).FileVersion, sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);
            }
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                TestFixture = PreparePublishedSelfContainedTestProject("StandaloneApp");
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
