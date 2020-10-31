// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using BundleTests.Helpers;

namespace Microsoft.NET.HostModel.Tests
{
    public class BundleLegacy : IClassFixture<BundleLegacy.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleLegacy(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [InlineData(0)]
        [InlineData(1)]
        [Theory]
        public void TestNetCoreApp3xApp(int minorVersion)
        {
            var fixture = (minorVersion == 0) ? sharedTestState.TestFixture30.Copy() : sharedTestState.TestFixture31.Copy();

            var singleFile = BundleHelper.BundleApp(fixture, targetFrameworkVersion: new Version(3, minorVersion));

            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World!");
        }

        private static TestProjectFixture CreatePublishedFixture(string netCoreAppFramework, string mnaVersion)
        {
            var repoDirectories = new RepoDirectoriesProvider(microsoftNETCoreAppVersion: mnaVersion);
            var fixture = new TestProjectFixture("StandaloneApp3x", repoDirectories, framework: netCoreAppFramework, assemblyName: "StandaloneApp");

            fixture.PublishProject(runtime: fixture.CurrentRid, outputDirectory: BundleHelper.GetPublishPath(fixture), restore: true);

            return fixture;
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture30 { get; set; }
            public TestProjectFixture TestFixture31 { get; set; }
            

            public SharedTestState()
            {
                TestFixture30 = CreatePublishedFixture("netcoreapp3.0", "3.0.0");
                TestFixture31 = CreatePublishedFixture("netcoreapp3.1", "3.1.0");
            }

            public void Dispose()
            {
                TestFixture30.Dispose();
                TestFixture31.Dispose();
            }
        }
    }
}
