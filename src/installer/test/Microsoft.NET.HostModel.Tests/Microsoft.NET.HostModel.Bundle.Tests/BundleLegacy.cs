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
    public class BuncleLegacy : IClassFixture<BuncleLegacy.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BuncleLegacy(BuncleLegacy.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void TestNetCoreApp30Apps()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            // Targetting netcoreap3.0 implies BundleOption.BundleAllContent
            var singleFile = BundleHelper.BundleApp(fixture, BundleOptions.None, "netcoreapp3.0");

            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World!");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider(microsoftNETCoreAppVersion: "3.0.0");

                TestFixture = new TestProjectFixture("StandaloneApp30", RepoDirectories,
                                                     framework: "netcoreapp3.0", assemblyName: "StandaloneApp");

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
