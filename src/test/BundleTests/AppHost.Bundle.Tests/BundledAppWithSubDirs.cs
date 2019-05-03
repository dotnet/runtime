// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;
using BundleTests.Helpers;
using Microsoft.DotNet.CoreSetup.Test;

namespace AppHost.Bundle.Tests
{
    public class BundledAppWithSubDirs : IClassFixture<BundledAppWithSubDirs.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundledAppWithSubDirs(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private void RunTheApp(string path)
        {
            Command.Create(path)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Wow! We now say hello to the big world and you.");
        }

        [Fact]
        private void Bundled_Framework_dependent_App_Run_Succeeds()
        {
            var fixture = sharedTestState.TestFrameworkDependentFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture);

            // Run the bundled app (extract files)
            RunTheApp(singleFile);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile);
        }

        [Fact]
        private void Bundled_Self_Contained_App_Run_Succeeds()
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture);

            // Run the bundled app (extract files)
            RunTheApp(singleFile);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile);
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFrameworkDependentFixture { get; set; }
            public TestProjectFixture TestSelfContainedFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFrameworkDependentFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                TestFrameworkDependentFixture
                    .EnsureRestoredForRid(TestFrameworkDependentFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFrameworkDependentFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestFrameworkDependentFixture));

                TestSelfContainedFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                TestSelfContainedFixture
                    .EnsureRestoredForRid(TestSelfContainedFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestSelfContainedFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestSelfContainedFixture));
            }

            public void Dispose()
            {
                TestFrameworkDependentFixture.Dispose();
                TestSelfContainedFixture.Dispose();
            }
        }
    }
}
