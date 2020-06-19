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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/38013")]
        [InlineData(true)]  // Test renaming the single-exe when contents are extracted
        [InlineData(false)] // Test renaming the single-exe when contents are not extracted 
        private void Bundle_can_be_renamed_while_running(bool testExtraction)
        {
            var fixture = sharedTestState.TestFixture.Copy();
            BundleOptions options = testExtraction ? BundleOptions.BundleAllContent : BundleOptions.None;
            string singleFile = BundleHelper.BundleApp(fixture, options);
            string outputDir = Path.GetDirectoryName(singleFile);
            string renameFile = Path.Combine(outputDir, Path.GetRandomFileName());
            string waitFile = Path.Combine(outputDir, "wait");
            string resumeFile = Path.Combine(outputDir, "resume");

            // Once the App starts running, it creates the waitFile, and waits until resumeFile file is created.
            var singleExe = Command.Create(singleFile, waitFile, resumeFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Start();

            const int twoMitutes = 120000 /*milliseconds*/;
            int waitTime = 0;
            while (!File.Exists(waitFile) && !singleExe.Process.HasExited && waitTime < twoMitutes)
            {
                Thread.Sleep(100);
                waitTime += 100;
            }

            Assert.True(File.Exists(waitFile));

            File.Move(singleFile, renameFile);
            File.Create(resumeFile).Close();

            var result = singleExe.WaitForExit(fExpectedToFail: false, twoMitutes);

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
                                    outputDirectory: BundleHelper.GetPublishPath(TestFixture));
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
