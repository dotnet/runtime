// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
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

        public BundleLegacy(BundleLegacy.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [InlineData(3.0)]
        [InlineData(3.1)]
        [Theory]
        public void TestNetCoreApp3xApp(float targetFrameworkVersion)
        {
            var fixture = (targetFrameworkVersion == 3.0) ? sharedTestState.TestFixture30.Copy() : sharedTestState.TestFixture31.Copy();

            // Targetting netcoreap3.0 implies BundleOption.BundleAllContent
            var singleFile = BundleHelper.BundleApp(fixture, BundleOptions.None, targetFrameworkVersion);

            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World!");
        }

        private static TestProjectFixture CreateTestFixture(string netCoreAppFramework, string mnaVersion)
        {
            var repoDirectories = new RepoDirectoriesProvider(microsoftNETCoreAppVersion: mnaVersion);
            var fixture = new TestProjectFixture("StandaloneApp3x", repoDirectories, framework: netCoreAppFramework, assemblyName: "StandaloneApp");

            fixture
                .EnsureRestoredForRid(fixture.CurrentRid, repoDirectories.CorehostPackages)
                .PublishProject(runtime: fixture.CurrentRid, outputDirectory: BundleHelper.GetPublishPath(fixture));

            return fixture;
        }


        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture30 { get; set; }
            public TestProjectFixture TestFixture31 { get; set; }
            

            public SharedTestState()
            {
                TestFixture30 = CreateTestFixture("netcoreapp3.0", "3.0.0");
                TestFixture31 = CreateTestFixture("netcoreapp3.1", "3.1.0");
            }

            public void Dispose()
            {
                TestFixture30.Dispose();
                TestFixture31.Dispose();
            }
        }
    }
}
