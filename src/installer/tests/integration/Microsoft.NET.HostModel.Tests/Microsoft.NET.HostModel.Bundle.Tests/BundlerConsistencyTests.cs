// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BundleTests.Helpers;
using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

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
            var targetOS = BundleHelper.GetTargetOS(fixture.CurrentRid);
            var targetArch = BundleHelper.GetTargetArch(fixture.CurrentRid);
            Bundler bundler = new Bundler(hostName, bundleDir.FullName, targetOS: targetOS, targetArch: targetArch);

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
            var targetOS = BundleHelper.GetTargetOS(fixture.CurrentRid);
            var targetArch = BundleHelper.GetTargetArch(fixture.CurrentRid);

            // Generate a file specification without the apphost
            var fileSpecs = new List<FileSpec>();
            string[] files = { $"{appName}.dll", $"{appName}.deps.json", $"{appName}.runtimeconfig.json" };
            Array.ForEach(files, x => fileSpecs.Add(new FileSpec(x, x)));

            Bundler bundler = new Bundler(hostName, bundleDir.FullName, targetOS: targetOS, targetArch: targetArch);

            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(fileSpecs));
        }

        [Fact]
        public void TestWithExactDuplicateEntriesPasses()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);
            var targetOS = BundleHelper.GetTargetOS(fixture.CurrentRid);
            var targetArch = BundleHelper.GetTargetArch(fixture.CurrentRid);

            // Generate a file specification with duplicate entries
            var fileSpecs = new List<FileSpec>();
            fileSpecs.Add(new FileSpec(BundleHelper.GetHostPath(fixture), BundleHelper.GetHostName(fixture)));
            string appPath = BundleHelper.GetAppPath(fixture);
            fileSpecs.Add(new FileSpec(appPath, "rel/app.repeat.dll"));
            fileSpecs.Add(new FileSpec(appPath, "rel/app.repeat.dll"));
            string systemLibPath = Path.Join(BundleHelper.GetPublishPath(fixture), "System.dll");
            fileSpecs.Add(new FileSpec(systemLibPath, "rel/system.repeat.dll"));
            fileSpecs.Add(new FileSpec(systemLibPath, "rel/system.repeat.dll"));

            Bundler bundler = new Bundler(hostName, bundleDir.FullName, targetOS: targetOS, targetArch: targetArch);
            bundler.GenerateBundle(fileSpecs);

            // Exact duplicates are not duplicated in the bundle
            bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals("rel/app.repeat.dll")).Single().Type.Should().Be(FileType.Assembly);
            bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals("rel/system.repeat.dll")).Single().Type.Should().Be(FileType.Assembly);
        }

        [Fact]
        public void TestWithDuplicateEntriesFails()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);
            var targetOS = BundleHelper.GetTargetOS(fixture.CurrentRid);
            var targetArch = BundleHelper.GetTargetArch(fixture.CurrentRid);

            // Generate a file specification with duplicate entries
            var fileSpecs = new List<FileSpec>();
            fileSpecs.Add(new FileSpec(BundleHelper.GetHostPath(fixture), BundleHelper.GetHostName(fixture)));
            fileSpecs.Add(new FileSpec(BundleHelper.GetAppPath(fixture), "rel/app.repeat"));
            fileSpecs.Add(new FileSpec(Path.Join(BundleHelper.GetPublishPath(fixture), "System.dll"), "rel/app.repeat"));

            Bundler bundler = new Bundler(hostName, bundleDir.FullName, targetOS: targetOS, targetArch: targetArch);
            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(fileSpecs))
                .Message
                    .Should().Contain("rel/app.repeat")
                    .And.Contain(BundleHelper.GetAppPath(fixture));
        }

        [Fact]
        public void TestWithCaseSensitiveDuplicateEntriesPasses()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);
            var targetOS = BundleHelper.GetTargetOS(fixture.CurrentRid);
            var targetArch = BundleHelper.GetTargetArch(fixture.CurrentRid);

            // Generate a file specification with duplicate entries
            var fileSpecs = new List<FileSpec>();
            fileSpecs.Add(new FileSpec(BundleHelper.GetHostPath(fixture), BundleHelper.GetHostName(fixture)));
            fileSpecs.Add(new FileSpec(BundleHelper.GetAppPath(fixture), "rel/app.repeat.dll"));
            fileSpecs.Add(new FileSpec(Path.Join(BundleHelper.GetPublishPath(fixture), "System.dll"), "rel/app.Repeat.dll"));

            Bundler bundler = new Bundler(hostName, bundleDir.FullName, targetOS: targetOS, targetArch: targetArch);
            bundler.GenerateBundle(fileSpecs);

            bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals("rel/app.repeat.dll")).Single().Type.Should().Be(FileType.Assembly);
            bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals("rel/app.Repeat.dll")).Single().Type.Should().Be(FileType.Assembly);
        }

        private (string bundleFileName, string bundleId) CreateSampleBundle(bool bundleMultipleFiles)
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var bundleDir = Directory.CreateDirectory(
                Path.Combine(BundleHelper.GetBundleDir(fixture).FullName, Path.GetRandomFileName()));
            var targetOS = BundleHelper.GetTargetOS(fixture.CurrentRid);
            var targetArch = BundleHelper.GetTargetArch(fixture.CurrentRid);

            var fileSpecs = new List<FileSpec>();
            fileSpecs.Add(new FileSpec(BundleHelper.GetHostPath(fixture), BundleHelper.GetHostName(fixture)));
            if (bundleMultipleFiles)
            {
                fileSpecs.Add(new FileSpec(BundleHelper.GetAppPath(fixture), "rel/app.repeat.dll"));
            }

            Bundler bundler = new Bundler(hostName, bundleDir.FullName, targetOS: targetOS, targetArch: targetArch);
            return (bundler.GenerateBundle(fileSpecs), bundler.BundleManifest.BundleID);
        }

        [Fact]
        public void TestWithIdenticalBundlesShouldBeBinaryEqualPasses()
        {
            var firstBundle = CreateSampleBundle(true);
            byte[] firstBundleContent = File.ReadAllBytes(firstBundle.bundleFileName);
            var secondBundle = CreateSampleBundle(true);
            byte[] secondBundleContent = File.ReadAllBytes(secondBundle.bundleFileName);

            firstBundle.bundleId.ShouldBeEquivalentTo(secondBundle.bundleId,
                "Deterministic/Reproducible build should produce identical bundle id for identical inputs");
            firstBundleContent.ShouldBeEquivalentTo(secondBundleContent,
                "Deterministic/Reproducible build should produce identical binary for identical inputs");
        }

        [Fact]
        public void TestWithUniqueBundlesShouldHaveUniqueBundleIdsPasses()
        {
            string firstBundle = CreateSampleBundle(true).bundleId;
            string secondBundle = CreateSampleBundle(false).bundleId;

            Assert.NotEqual(firstBundle, secondBundle, StringComparer.Ordinal);
        }

        [Fact]
        public void TestWithMultipleDuplicateEntriesFails()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var hostName = BundleHelper.GetHostName(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);
            var targetOS = BundleHelper.GetTargetOS(fixture.CurrentRid);
            var targetArch = BundleHelper.GetTargetArch(fixture.CurrentRid);

            // Generate a file specification with duplicate entries
            var fileSpecs = new List<FileSpec>();
            fileSpecs.Add(new FileSpec(BundleHelper.GetHostPath(fixture), BundleHelper.GetHostName(fixture)));
            string appPath = BundleHelper.GetAppPath(fixture);
            fileSpecs.Add(new FileSpec(appPath, "rel/app.repeat.dll"));
            fileSpecs.Add(new FileSpec(appPath, "rel/app.repeat.dll"));
            string systemLibPath = Path.Join(BundleHelper.GetPublishPath(fixture), "System.dll");
            fileSpecs.Add(new FileSpec(appPath, "rel/system.repeat.dll"));
            fileSpecs.Add(new FileSpec(systemLibPath, "rel/system.repeat.dll"));

            Bundler bundler = new Bundler(hostName, bundleDir.FullName, targetOS: targetOS, targetArch: targetArch);
            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(fileSpecs))
                .Message
                    .Should().Contain("rel/system.repeat.dll")
                    .And.NotContain("rel/app.repeat.dll")
                    .And.Contain(appPath)
                    .And.Contain(systemLibPath);
        }

        [Fact]
        public void TestBaseNameComputation()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var publishPath = BundleHelper.GetPublishPath(fixture);
            var bundleDir = BundleHelper.GetBundleDir(fixture);
            var targetOS = BundleHelper.GetTargetOS(fixture.CurrentRid);
            var targetArch = BundleHelper.GetTargetArch(fixture.CurrentRid);

            // Rename the host from "StandaloneApp" to "Stand.Alone.App" to check that baseName computation
            // (and consequently deps.json and runtimeconfig.json name computations) in the bundler
            // work correctly in the presence of "."s in the hostName.
            var originalBaseName = "StandaloneApp";
            var newBaseName = "Stand.Alone.App";
            var exe = OperatingSystem.IsWindows() ? ".exe" : string.Empty;

            void rename(string extension)
            {
                File.Move(Path.Combine(publishPath, originalBaseName + extension), Path.Combine(publishPath, newBaseName + extension));
            }
            rename(exe);
            rename(".deps.json");
            rename(".runtimeconfig.json");

            var hostName = newBaseName + exe;
            var depsJson = newBaseName + ".deps.json";
            var runtimeconfigJson = newBaseName + ".runtimeconfig.json";

            var bundler = new Bundler(hostName, bundleDir.FullName, targetOS: targetOS, targetArch: targetArch);
            BundleHelper.GenerateBundle(bundler, publishPath, bundleDir.FullName);

            string[] jsonFiles = { depsJson, runtimeconfigJson };

            bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals(depsJson)).Single().Type.Should().Be(FileType.DepsJson);
            bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals(runtimeconfigJson)).Single().Type.Should().Be(FileType.RuntimeConfigJson);
            bundleDir.Should().NotHaveFiles(jsonFiles);
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
        public void TestFileSizes()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var bundler = BundleHelper.Bundle(fixture);
            var publishPath = BundleHelper.GetPublishPath(fixture);

            bundler.BundleManifest.Files.ForEach(file =>
                Assert.True(file.Size == new FileInfo(Path.Combine(publishPath, file.RelativePath)).Length));
        }

        [Fact]
        public void TestAssemblyAlignment()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var bundler = BundleHelper.Bundle(fixture);
            var targetOS = BundleHelper.GetTargetOS(fixture.CurrentRid);
            var targetArch = BundleHelper.GetTargetArch(fixture.CurrentRid);
            var alignment = (targetOS == OSPlatform.Linux && targetArch == Architecture.Arm64) ? 4096 : 16;
            bundler.BundleManifest.Files.ForEach(file => 
                Assert.True((file.Type != FileType.Assembly) || (file.Offset % alignment == 0)));
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
