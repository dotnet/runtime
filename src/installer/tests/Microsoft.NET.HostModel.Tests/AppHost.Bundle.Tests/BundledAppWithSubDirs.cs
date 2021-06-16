// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundledAppWithSubDirs : BundleTestBase, IClassFixture<BundledAppWithSubDirs.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundledAppWithSubDirs(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private void RunTheApp(string path, TestProjectFixture fixture)
        {
            Command.Create(path)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("DOTNET_ROOT", fixture.BuiltDotnet.BinPath)
                .EnvironmentVariable("DOTNET_ROOT(x86)", fixture.BuiltDotnet.BinPath)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Wow! We now say hello to the big world and you.");
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Framework_dependent_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestFrameworkDependentFixture.Copy();
            UseFrameworkDependentHost(fixture);
            var singleFile = BundleHelper.BundleApp(fixture, options);

            // Run the bundled app (extract files)
            RunTheApp(singleFile, fixture);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, fixture);
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Self_Contained_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture, options);

            // Run the bundled app (extract files)
            RunTheApp(singleFile, fixture);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, fixture);
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Self_Contained_NoCompression_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture, options, disableCompression: true);

            // Run the bundled app (extract files)
            RunTheApp(singleFile, fixture);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, fixture);
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Self_Contained_Targeting50_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture, options, new Version(5, 0));

            // Run the bundled app (extract files)
            RunTheApp(singleFile, fixture);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, fixture);
        }

        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Framework_dependent_Targeting50_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            UseFrameworkDependentHost(fixture);
            var singleFile = BundleHelper.BundleApp(fixture, options, new Version(5, 0));

            // Run the bundled app (extract files)
            RunTheApp(singleFile, fixture);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, fixture);
        }

        [Fact]
        public void Bundled_Self_Contained_Targeting50_WithCompression_Throws()
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            UseSingleFileSelfContainedHost(fixture);
            // compression must be off when targeting 5.0
            var options = BundleOptions.EnableCompression;

            Assert.Throws<ArgumentException>(()=>BundleHelper.BundleApp(fixture, options, new Version(5, 0)));
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_With_Empty_File_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestAppWithEmptyFileFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture, options);

            // Run the app
            RunTheApp(singleFile, fixture);
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFrameworkDependentFixture { get; set; }
            public TestProjectFixture TestSelfContainedFixture { get; set; }
            public TestProjectFixture TestAppWithEmptyFileFixture { get; set; }

            public SharedTestState()
            {
                TestFrameworkDependentFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestFrameworkDependentFixture);
                TestFrameworkDependentFixture
                    .EnsureRestoredForRid(TestFrameworkDependentFixture.CurrentRid)
                    .PublishProject(runtime: TestFrameworkDependentFixture.CurrentRid,
                                    selfContained: false,
                                    outputDirectory: BundleHelper.GetPublishPath(TestFrameworkDependentFixture));

                TestSelfContainedFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestSelfContainedFixture);
                TestSelfContainedFixture
                    .EnsureRestoredForRid(TestSelfContainedFixture.CurrentRid)
                    .PublishProject(runtime: TestSelfContainedFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestSelfContainedFixture));

                TestAppWithEmptyFileFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestAppWithEmptyFileFixture);
                BundleHelper.AddEmptyContentToApp(TestAppWithEmptyFileFixture);
                TestAppWithEmptyFileFixture
                    .EnsureRestoredForRid(TestAppWithEmptyFileFixture.CurrentRid)
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
