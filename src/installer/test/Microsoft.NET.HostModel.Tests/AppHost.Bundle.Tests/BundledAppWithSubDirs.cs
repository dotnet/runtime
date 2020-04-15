﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.Bundle;
using Microsoft.DotNet.CoreSetup.Test;
using System;
using Xunit;

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

        // BundleOptions.BundleNativeBinaries: Test when the payload data files are unbundled, and beside the single-file app.
        // BundleOptions.BundleAllContent: Test when the payload data files are bundled and extracted to temporary directory. 
        // Once the runtime can load assemblies from the bundle, BundleOptions.None can be used in place of BundleOptions.BundleNativeBinaries.
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Framework_dependent_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestFrameworkDependentFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture, options);

            // Run the bundled app (extract files)
            RunTheApp(singleFile);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile);
        }

        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Self_Contained_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture, options);

            // Run the bundled app (extract files)
            RunTheApp(singleFile);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile);
        }

        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_With_Empty_File_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestAppWithEmptyFileFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture, options);

            // Run the app
            RunTheApp(singleFile);
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFrameworkDependentFixture { get; set; }
            public TestProjectFixture TestSelfContainedFixture { get; set; }
            public TestProjectFixture TestAppWithEmptyFileFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFrameworkDependentFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestFrameworkDependentFixture);
                TestFrameworkDependentFixture
                    .EnsureRestoredForRid(TestFrameworkDependentFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFrameworkDependentFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestFrameworkDependentFixture));

                TestSelfContainedFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestSelfContainedFixture);
                TestSelfContainedFixture
                    .EnsureRestoredForRid(TestSelfContainedFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestSelfContainedFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestSelfContainedFixture));

                TestAppWithEmptyFileFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestAppWithEmptyFileFixture);
                BundleHelper.AddEmptyContentToApp(TestAppWithEmptyFileFixture);
                TestAppWithEmptyFileFixture
                    .EnsureRestoredForRid(TestAppWithEmptyFileFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestAppWithEmptyFileFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestAppWithEmptyFileFixture));
            }

            public void Dispose()
            {
                TestFrameworkDependentFixture.Dispose();
                TestSelfContainedFixture.Dispose();
                TestAppWithEmptyFileFixture.Dispose();
            }
        }
    }
}
