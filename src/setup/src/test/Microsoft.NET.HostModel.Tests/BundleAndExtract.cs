﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.Bundle;

namespace Microsoft.DotNet.CoreSetup.Test.BundleTests.BundleExtract
{
    public class BundleAndExtract : IClassFixture<BundleAndExtract.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleAndExtract(BundleAndExtract.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private void Run(TestProjectFixture fixture, string publishDir, string singleFileDir)
        {
            var dotnet = fixture.SdkDotnet;
            var hostName = Path.GetFileName(fixture.TestProject.AppExe);

            // Run the App normally
            Command.Create(Path.Combine(publishDir, hostName))
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Wow! We now say hello to the big world and you.");

            // Bundle to a single-file
            Bundler bundler = new Bundler(hostName, singleFileDir);
            string singleFile = bundler.GenerateBundle(publishDir);

            // Extract the file
            Extractor extractor = new Extractor(singleFile, singleFileDir);
            extractor.ExtractFiles();

            // Run the extracted app
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Wow! We now say hello to the big world and you.");
        }

        private string GetSingleFileDir(TestProjectFixture fixture)
        {
            // Create a directory for bundle/extraction output.
            // This directory shouldn't be within TestProject.OutputDirectory, since the bundler
            // will (attempt to) embed all files below the TestProject.OutputDirectory tree into one file.

            string singleFileDir = Path.Combine(fixture.TestProject.ProjectDirectory, "oneExe");
            Directory.CreateDirectory(singleFileDir);

            return singleFileDir;
        }

        private string RelativePath(string path)
        {
            return Path.GetRelativePath(Directory.GetCurrentDirectory(), path)
                       .TrimEnd(Path.DirectorySeparatorChar);
        }

        [Fact]
        public void TestWithAbsolutePaths()
        {
            var fixture = sharedTestState.TestFixture
                .Copy();

            string publishDir = fixture.TestProject.OutputDirectory;
            string singleFileDir = GetSingleFileDir(fixture);

            Run(fixture, publishDir, singleFileDir);
        }

        [Fact]
        public void TestWithRelativePaths()
        {
            var fixture = sharedTestState.TestFixture
                .Copy();

            string publishDir = RelativePath(fixture.TestProject.OutputDirectory);
            string singleFileDir = RelativePath(GetSingleFileDir(fixture));

            Run(fixture, publishDir, singleFileDir);
        }

        [Fact]
        public void TestWithRelativePathsDirSeparator()
        {
            var fixture = sharedTestState.TestFixture
                .Copy();

            string publishDir = RelativePath(fixture.TestProject.OutputDirectory) + Path.DirectorySeparatorChar;
            string singleFileDir = RelativePath(GetSingleFileDir(fixture)) + Path.DirectorySeparatorChar;

            Run(fixture, publishDir, singleFileDir);
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("StandaloneAppWithSubDirs", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid);
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
