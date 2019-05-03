// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using BundleTests.Helpers;

namespace Microsoft.NET.HostModel.Tests
{
    public class BundlerConsistencyTests : IClassFixture<BundlerConsistencyTests.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundlerConsistencyTests(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void TestWithEmptySpecFails()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);
            Bundler bundler = new Bundler(hostName, bundleDir.FullName);

            FileSpec[][] invalidSpecs =
            {
                new FileSpec[] {new FileSpec(hostName, null) },
                new FileSpec[] {new FileSpec(hostName, "") },
                new FileSpec[] {new FileSpec(hostName, "    ") }
            };

            foreach (var invalidSpec in invalidSpecs)
            {
                Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(invalidSpec));
            }
        }

        [Fact]
        public void TestWithoutSpecifyingHostFails()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var appName = Path.GetFileNameWithoutExtension(hostName);
            string publishPath = BundleHelper.GetPublishPath(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);

            // Generate a file specification without the apphost
            var fileSpecs = new List<FileSpec>();
            string[] files = { $"{appName}.dll", $"{appName}.deps.json", $"{appName}.runtimeconfig.json" };
            Array.ForEach(files, x => fileSpecs.Add(new FileSpec(x, x)));

            Bundler bundler = new Bundler(hostName, bundleDir.FullName);

            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(fileSpecs));
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void TestFilesNotBundled(bool embedPDBs)
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var appName = Path.GetFileNameWithoutExtension(hostName);
            string publishPath = BundleHelper.GetPublishPath(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);
            
            // Make up a app.runtimeconfig.dev.json file in the publish directory.
            File.Copy(Path.Combine(publishPath, $"{appName}.runtimeconfig.json"), 
                      Path.Combine(publishPath, $"{appName}.runtimeconfig.dev.json"));

            var singleFile = new Bundler(hostName, bundleDir.FullName, embedPDBs).GenerateBundle(publishPath);

            bundleDir.Should().OnlyHaveFiles(new string[] { hostName });

            new Extractor(singleFile, bundleDir.FullName).ExtractFiles();

            bundleDir.Should().NotHaveFile($"{appName}.runtimeconfig.dev.json");
            if (!embedPDBs)
            {
                bundleDir.Should().NotHaveFile($"{appName}.pdb");
            }
        }

        [Fact]
        public void ExtractingANonBundleFails()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var hostExe = Path.Combine(BundleHelper.GetPublishPath(fixture), hostName);

            var bundleDir = BundleHelper.GetBundleDir(fixture);
            Extractor extractor = new Extractor(hostExe, "extract");
            Assert.Throws<BundleException>(() => extractor.ExtractFiles());
        }

        [Fact]
        public void AllBundledFilesAreExtracted()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);

            var bundler = new Bundler(hostName, bundleDir.FullName);
            string singleFile = bundler.GenerateBundle(BundleHelper.GetPublishPath(fixture));

            var expectedFiles = new List<string>(bundler.BundleManifest.Files.Count);
            expectedFiles.Add(hostName);
            bundler.BundleManifest.Files.ForEach(file => expectedFiles.Add(file.RelativePath));

            new Extractor(singleFile, bundleDir.FullName).ExtractFiles();
            bundleDir.Should().OnlyHaveFiles(expectedFiles);
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
