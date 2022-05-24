// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundleRename : BundleTestBase, IClassFixture<BundleRename.SharedTestState>
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
            string singleFile = BundleSelfContainedApp(fixture, options);
            string outputDir = Path.GetDirectoryName(singleFile);
            string renameFile = Path.Combine(outputDir, Path.GetRandomFileName());
            string waitFile = Path.Combine(outputDir, "wait");
            string resumeFile = Path.Combine(outputDir, "resume");

            // Once the App starts running, it creates the waitFile, and waits until resumeFile file is created.
            var singleExe = Command.Create(singleFile, waitFile, resumeFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Start();

            const int twoMinutes = 120000 /*milliseconds*/;
            int waitTime = 0;
            while (!File.Exists(waitFile) && !singleExe.Process.HasExited && waitTime < twoMinutes)
            {
                Thread.Sleep(100);
                waitTime += 100;
            }

            Assert.True(File.Exists(waitFile));

            File.Move(singleFile, renameFile);
            File.Create(resumeFile).Close();

            singleExe.WaitForExit(expectedToFail: false, twoMinutes)
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World!");
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                TestFixture = PreparePublishedSelfContainedTestProject("AppWithWait");
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
