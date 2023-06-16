// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.Bundle;
using Microsoft.DotNet.CoreSetup.Test;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundleLocalizedApp : BundleTestBase, IClassFixture<BundleLocalizedApp.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleLocalizedApp(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Bundled_Localized_App_Run_Succeeds()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture);

            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("[kn-IN]! [ta-IN]! [default]!");
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                TestFixture = PreparePublishedSelfContainedTestProject("LocalizedApp");
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
