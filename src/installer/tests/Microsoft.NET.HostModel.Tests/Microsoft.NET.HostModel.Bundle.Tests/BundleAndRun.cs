// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using BundleTests.Helpers;

namespace Microsoft.NET.HostModel.Tests
{
    public class BundleAndRun : IClassFixture<BundleAndRun.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleAndRun(BundleAndRun.SharedTestState fixture)
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

        private void BundleRun(TestProjectFixture fixture, string publishPath)
        {
            var hostName = BundleHelper.GetHostName(fixture);

            // Run the App normally
            RunTheApp(Path.Combine(publishPath, hostName));

            // Bundle to a single-file
            string singleFile = BundleHelper.BundleApp(fixture);

            // Run the extracted app
            RunTheApp(singleFile);
        }

        private string RelativePath(string path)
        {
            return Path.GetRelativePath(Directory.GetCurrentDirectory(), path)
                       .TrimEnd(Path.DirectorySeparatorChar);
        }

        [Fact]
        public void TestWithAbsolutePaths()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            string publishDir = BundleHelper.GetPublishPath(fixture);
            BundleRun(fixture, publishDir);
        }

        [Fact]
        public void TestWithRelativePaths()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            string publishDir = RelativePath(BundleHelper.GetPublishPath(fixture));
            BundleRun(fixture, publishDir);
        }

        [Fact]
        public void TestWithRelativePathsDirSeparator()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            string publishDir = RelativePath(BundleHelper.GetPublishPath(fixture)) + Path.DirectorySeparatorChar;
            BundleRun(fixture, publishDir);
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public TestProjectFixture LegacyFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestFixture);
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
