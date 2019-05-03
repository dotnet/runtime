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
    public class BundleExtractRun : IClassFixture<BundleExtractRun.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleExtractRun(BundleExtractRun.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        public void RunTheApp(string path)
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

        private void BundleExtractAndRun(TestProjectFixture fixture, string publishDir, string singleFileDir)
        {
            var hostName = BundleHelper.GetHostName(fixture);

            // Run the App normally
            RunTheApp(Path.Combine(publishDir, hostName));

            // Bundle to a single-file
            Bundler bundler = new Bundler(hostName, singleFileDir);
            string singleFile = bundler.GenerateBundle(publishDir);

            // Extract the file
            Extractor extractor = new Extractor(singleFile, singleFileDir);
            extractor.ExtractFiles();

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
            string outputDir = BundleHelper.GetBundleDir(fixture).FullName;

            BundleExtractAndRun(fixture, publishDir, outputDir);
        }

        [Fact]
        public void TestWithRelativePaths()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            string publishDir = RelativePath(BundleHelper.GetPublishPath(fixture));
            string outputDir = RelativePath(BundleHelper.GetBundleDir(fixture).FullName);

            BundleExtractAndRun(fixture, publishDir, outputDir);
        }

        [Fact]
        public void TestWithRelativePathsDirSeparator()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            string publishDir = RelativePath(BundleHelper.GetPublishPath(fixture)) + Path.DirectorySeparatorChar;
            string outputDir = RelativePath(BundleHelper.GetBundleDir(fixture).FullName) + Path.DirectorySeparatorChar;

            BundleExtractAndRun(fixture, publishDir, outputDir);
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
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
