// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundleProbe : BundleTestBase, IClassFixture<BundleProbe.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleProbe(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        private void Bundle_Probe_Not_Passed_For_Non_Single_File_App()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            string appExe = BundleHelper.GetHostPath(fixture);

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("No BUNDLE_PROBE");
        }

        [Fact]
        private void Bundle_Probe_Passed_For_Single_File_App()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            string singleFile = BundleSelfContainedApp(fixture);

            Command.Create(singleFile, "SingleFile")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("BUNDLE_PROBE OK");
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                TestFixture = PreparePublishedSelfContainedTestProject("BundleProbeTester");
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
