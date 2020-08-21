// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using BundleTests.Helpers;
using System.Threading;

namespace AppHost.Bundle.Tests
{
    public class SingleFileApiTests : IClassFixture<SingleFileApiTests.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public SingleFileApiTests(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void CodeBaseThrows()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture);

            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("CodeBase InvalidOperation");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("SingleFileApis", RepoDirectories);
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