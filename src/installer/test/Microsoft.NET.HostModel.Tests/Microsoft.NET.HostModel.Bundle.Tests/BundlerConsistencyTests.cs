// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.Bundle;
using BundleTests.Helpers;
using FluentAssertions;

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
            var bundleDir = BundleHelper.GetBundleDir(fixture);

            // Generate a file specification without the apphost
            var fileSpecs = new List<FileSpec>();
            string[] files = { $"{appName}.dll", $"{appName}.deps.json", $"{appName}.runtimeconfig.json" };
            Array.ForEach(files, x => fileSpecs.Add(new FileSpec(x, x)));

            Bundler bundler = new Bundler(hostName, bundleDir.FullName);

            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(fileSpecs));
        }

        [Fact]
        public void TestWithDuplicateEntriesFails()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);

            // Generate a file specification with duplicate entries
            var fileSpecs = new List<FileSpec>();
            fileSpecs.Add(new FileSpec(BundleHelper.GetHostPath(fixture), BundleHelper.GetHostName(fixture)));
            fileSpecs.Add(new FileSpec(BundleHelper.GetAppPath(fixture), "app.repeat"));
            fileSpecs.Add(new FileSpec(BundleHelper.GetAppPath(fixture), "app.repeat"));

            Bundler bundler = new Bundler(hostName, bundleDir.FullName);
            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(fileSpecs));
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleOtherFiles)]
        [InlineData(BundleOptions.BundleAllContent)]
        [InlineData(BundleOptions.BundleSymbolFiles)]
        [Theory]
        public void TestFilesAlwaysBundled(BundleOptions options)
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var bundler = BundleHelper.Bundle(fixture, options);
            var bundledFiles = BundleHelper.GetBundledFiles(fixture);

            Array.ForEach(bundledFiles, file => bundler.BundleManifest.Contains(file).Should().BeTrue());
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleOtherFiles)]
        [InlineData(BundleOptions.BundleAllContent)]
        [InlineData(BundleOptions.BundleSymbolFiles)]
        [Theory]
        public void TestFilesNeverBundled(BundleOptions options)
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var appBaseName =  BundleHelper.GetAppBaseName(fixture);
            string publishPath = BundleHelper.GetPublishPath(fixture);
            
            // Make up a app.runtimeconfig.dev.json file in the publish directory.
            File.Copy(Path.Combine(publishPath, $"{appBaseName}.runtimeconfig.json"), 
                      Path.Combine(publishPath, $"{appBaseName}.runtimeconfig.dev.json"));

            var bundler = BundleHelper.Bundle(fixture, options);

            bundler.BundleManifest.Contains($"{appBaseName}.runtimeconfig.dev.json").Should().BeFalse();
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleSymbolFiles)]
        [Theory]
        public void TestBundlingSymbols(BundleOptions options)
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var appBaseName = BundleHelper.GetAppBaseName(fixture);
            var bundler = BundleHelper.Bundle(fixture, options);

            bundler.BundleManifest.Contains($"{appBaseName}.pdb").Should().Be(options.HasFlag(BundleOptions.BundleSymbolFiles));
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [Theory]
        public void TestBundlingNativeBinaries(BundleOptions options)
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var coreclr = Path.GetFileName(fixture.TestProject.CoreClrDll);
            var bundler = BundleHelper.Bundle(fixture, options);

            bundler.BundleManifest.Contains($"{coreclr}").Should().Be(options.HasFlag(BundleOptions.BundleNativeBinaries));
        }

        [Fact]
        public void TestAssemblyAlignment()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var bundler = BundleHelper.Bundle(fixture);

            bundler.BundleManifest.Files.ForEach(file => 
                Assert.True((file.Type != FileType.Assembly) || (file.Offset % Bundler.AssemblyAlignment == 0)));
        }

        [Fact]
        public void TestWithAdditionalContentAfterBundleMetadata()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            string singleFile = BundleHelper.BundleApp(fixture);

            using (var file = File.OpenWrite(singleFile))
            {
                file.Position = file.Length;
                var blob = Encoding.UTF8.GetBytes("Mock signature at the end of the bundle");
                file.Write(blob, 0, blob.Length);
            }

            Command.Create(singleFile)
                   .CaptureStdErr()
                   .CaptureStdOut()
                   .Execute()
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
