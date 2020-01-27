// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using BundleTests.Helpers;
using System.Threading;

namespace AppHost.Bundle.Tests
{
    public class BundleRename : IClassFixture<BundleRename.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleRename(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Theory]
        [InlineData(true)]  // Test renaming the single-exe during the initial run, when contents are extracted
        [InlineData(false)] // Test renaming the single-exe during subsequent runs, when contents are reused
        private void Bundle_can_be_renamed_while_running(bool renameFirstRun)
        {
            var fixture = sharedTestState.TestFixture.Copy();
            string singleFile = BundleHelper.GetPublishedSingleFilePath(fixture);
            string renameFile = Path.Combine(BundleHelper.GetPublishPath(fixture), Path.GetRandomFileName());
            string writeFile = Path.Combine(BundleHelper.GetPublishPath(fixture), "lock");

            if (!renameFirstRun)
            {
                Command.Create(singleFile)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute()
                    .Should()
                    .Pass()
                    .And
                    .HaveStdOutContaining("Hello World!");
            }

            var singleExe = Command.Create(singleFile, writeFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Start();

            // Once the App starts running, it creates the writeFile, and waits until the file is deleted.
            while (!File.Exists(writeFile))
            {
                Thread.Sleep(100);
            }

            File.Move(singleFile, renameFile);
            File.Delete(writeFile);

            var result = singleExe.WaitForExit(fExpectedToFail: false);

            result
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
                RepoDirectories = new RepoDirectoriesProvider();
                TestFixture = new TestProjectFixture("AppWithWait", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid,
                                    singleFile: true,
                                    outputDirectory: BundleHelper.GetPublishPath(TestFixture));
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
