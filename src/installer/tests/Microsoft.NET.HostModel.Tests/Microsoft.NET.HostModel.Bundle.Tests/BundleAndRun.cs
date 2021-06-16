// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using BundleTests.Helpers;
using System.Runtime.InteropServices;

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

        private void CheckFileNotarizable(string path)
        {
            // attempt to remove signature data.
            // no-op if the file is not signed (it should not be)
            // fail if the file structure is malformed
            // i: input, o: output, r: remove
            Command.Create("codesign_allocate", $"-i {path} -o {path} -r")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass();
        }

        private string MakeUniversalBinary(string path, string rid)
        {
            string fatApp = path + ".fat";
            string arch = BundleHelper.GetTargetArch(rid) == Architecture.Arm64 ? "arm64" : "x86_64";

            // We will create a universal binary with just one arch slice and run it.
            // It is enough for testing purposes. The code that finds the releavant slice
            // would work the same regardless if there is 1, 2, 3 or more slices.
            Command.Create("lipo", $"-create -arch {arch} {path} -output {fatApp}")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass();

            return fatApp;
        }

        private void BundleRun(TestProjectFixture fixture, string publishPath)
        {
            var hostName = BundleHelper.GetHostName(fixture);

            // Run the App normally
            RunTheApp(Path.Combine(publishPath, hostName));

            // Bundle to a single-file
            string singleFile = BundleHelper.BundleApp(fixture);

            // check that the file structure is understood by codesign
            var targetOS = BundleHelper.GetTargetOS(fixture.CurrentRid);
            if (targetOS == OSPlatform.OSX)
            {
                CheckFileNotarizable(singleFile);
            }

            // Run the extracted app
            RunTheApp(singleFile);

            if (targetOS == OSPlatform.OSX)
            {
                string fatApp = MakeUniversalBinary(singleFile, fixture.CurrentRid);

                // Run the fat app
                RunTheApp(fatApp);
            }
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
                    .EnsureRestoredForRid(TestFixture.CurrentRid)
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
