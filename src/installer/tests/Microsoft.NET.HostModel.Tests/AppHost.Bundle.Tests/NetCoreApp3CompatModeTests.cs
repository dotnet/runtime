// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.IO;
using System.Linq;
using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class NetCoreApp3CompatModeTests : IClassFixture<NetCoreApp3CompatModeTests.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public NetCoreApp3CompatModeTests(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Bundle_Is_Extracted()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            string singleFile;
            Bundler bundler = BundleHelper.BundleApp(fixture, out singleFile, BundleOptions.BundleAllContent);
            var extractionBaseDir = BundleHelper.GetExtractionRootDir(fixture);

            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractionBaseDir.FullName)
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            var extractionDir = BundleHelper.GetExtractionDir(fixture, bundler);
            var bundleFiles = BundleHelper.GetBundleDir(fixture).GetFiles().Select(file => file.Name).ToArray();
            var publishedFiles = Directory.GetFiles(BundleHelper.GetPublishPath(fixture), searchPattern: "*", searchOption: SearchOption.AllDirectories)
                .Select(file => Path.GetFileName(file))
                .Except(bundleFiles)
                .ToArray();
            var bundlerFiles = BundleHelper.GetBundleDir(fixture).GetFiles();
            extractionDir.Should().HaveFiles(publishedFiles);
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();
                TestFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid, outputDirectory: BundleHelper.GetPublishPath(TestFixture));
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
