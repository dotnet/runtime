// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace Microsoft.NET.HostModel.Bundle.Tests
{
    public class BundlerConsistencyTests : IClassFixture<BundlerConsistencyTests.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundlerConsistencyTests(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private static string BundlerHostName = Binaries.GetExeFileNameForCurrentPlatform(SharedTestState.AppName);
        private Bundler CreateBundlerInstance(BundleOptions bundleOptions = BundleOptions.None, Version version = null, bool macosCodesign = true)
            => new Bundler(BundlerHostName, sharedTestState.App.GetUniqueSubdirectory("bundle"), bundleOptions, targetFrameworkVersion: version, macosCodesign: macosCodesign);

        [Fact]
        public void EnableCompression_Before60_Fails()
        {
            // compression must be off when targeting pre-6.0
            Assert.Throws<ArgumentException>(() =>
                CreateBundlerInstance(BundleOptions.EnableCompression, new Version(5, 0)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void InvalidFileSpec_Fails(string invalidSpecPath)
        {
            FileSpec invalidSourcePath = new FileSpec(invalidSpecPath, BundlerHostName);
            Assert.False(invalidSourcePath.IsValid());

            FileSpec invalidBundlePath = new FileSpec(BundlerHostName, invalidSpecPath);
            Assert.False(invalidBundlePath.IsValid());

            Bundler bundler = CreateBundlerInstance();
            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(new[] { invalidSourcePath }));
            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(new[] { invalidBundlePath }));
        }

        [Fact]
        public void NoHostInFileSpecs_Fails()
        {
            var appName = Path.GetFileNameWithoutExtension(BundlerHostName);

            // File specification without the apphost
            var fileSpecs = new FileSpec[]
            {
                new FileSpec($"{appName}.dll", $"{appName}.dll"),
                new FileSpec($"{appName}.deps.json", $"{appName}.deps.json"),
                new FileSpec($"{appName}.runtimeconfig.json", $"{appName}.runtimeconfig.json")
            };

            Bundler bundler = CreateBundlerInstance();
            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(fileSpecs));
        }

        [Fact]
        public void ExactDuplicateEntries()
        {
            string appPath = sharedTestState.App.AppDll;
            string systemLibPath = sharedTestState.SystemDll;

            // File specification with duplicate entries with matching source paths
            var fileSpecs = new FileSpec[]
            {
                new FileSpec(Binaries.AppHost.FilePath, BundlerHostName),
                new FileSpec(appPath, "rel/app.repeat.dll"),
                new FileSpec(appPath, "rel/app.repeat.dll"),
                new FileSpec(systemLibPath, "rel/system.repeat.dll"),
                new FileSpec(systemLibPath, "rel/system.repeat.dll")
            };

            Bundler bundler = CreateBundlerInstance();
            bundler.GenerateBundle(fileSpecs);

            // Exact duplicates are not duplicated in the bundle
            bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals("rel/app.repeat.dll")).Single().Type.Should().Be(FileType.Assembly);
            bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals("rel/system.repeat.dll")).Single().Type.Should().Be(FileType.Assembly);
        }

        [Fact]
        public void DuplicateBundleRelativePath_Fails()
        {
            // File specification with duplicate entries with different source paths
            var fileSpecs = new FileSpec[]
            {
                new FileSpec(Binaries.AppHost.FilePath, BundlerHostName),
                new FileSpec(sharedTestState.App.AppDll, "rel/app.repeat"),
                new FileSpec(sharedTestState.SystemDll, "rel/app.repeat"),
            };

            Bundler bundler = CreateBundlerInstance();
            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(fileSpecs))
                .Message
                    .Should().Contain("rel/app.repeat")
                    .And.Contain(sharedTestState.App.AppDll);
        }

        [Fact]
        public void CaseSensitiveBundleRelativePath()
        {
            // File specification with entries with bundle paths differing only in casing
            var fileSpecs = new FileSpec[]
            {
                new FileSpec(Binaries.AppHost.FilePath, BundlerHostName),
                new FileSpec(sharedTestState.App.AppDll, "rel/app.repeat.dll"),
                new FileSpec(sharedTestState.SystemDll, "rel/app.Repeat.dll"),
            };

            Bundler bundler = CreateBundlerInstance();
            bundler.GenerateBundle(fileSpecs);

            bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals("rel/app.repeat.dll")).Single().Type.Should().Be(FileType.Assembly);
            bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals("rel/app.Repeat.dll")).Single().Type.Should().Be(FileType.Assembly);
        }

        private (string bundleFileName, string bundleId) CreateSampleBundle(bool bundleMultipleFiles)
        {
            var fileSpecs = new List<FileSpec>()
            {
                new FileSpec(Binaries.AppHost.FilePath, BundlerHostName)
            };
            if (bundleMultipleFiles)
            {
                fileSpecs.Add(new FileSpec(sharedTestState.App.AppDll, "rel/app.repeat.dll"));
            }

            Bundler bundler = CreateBundlerInstance();
            return (bundler.GenerateBundle(fileSpecs), bundler.BundleManifest.BundleID);
        }

        [Fact]
        public void IdenticalBundles_BinaryEqual()
        {
            var firstBundle = CreateSampleBundle(true);
            byte[] firstBundleContent = File.ReadAllBytes(firstBundle.bundleFileName);
            var secondBundle = CreateSampleBundle(true);
            byte[] secondBundleContent = File.ReadAllBytes(secondBundle.bundleFileName);

            firstBundle.bundleId.Should().BeEquivalentTo(secondBundle.bundleId,
                "Deterministic/Reproducible build should produce identical bundle id for identical inputs");
            firstBundleContent.Should().BeEquivalentTo(secondBundleContent,
                "Deterministic/Reproducible build should produce identical binary for identical inputs");
        }

        [Fact]
        public void UniqueBundles_UniqueBundleIds()
        {
            string firstBundle = CreateSampleBundle(true).bundleId;
            string secondBundle = CreateSampleBundle(false).bundleId;

            Assert.NotEqual(firstBundle, secondBundle, StringComparer.Ordinal);
        }

        [Fact]
        public void MultipleDuplicateBundleRelativePath_Fails()
        {
            // File specification with a mix of duplicate entries with different/matching source paths
            string appPath = sharedTestState.App.AppDll;
            string systemLibPath = sharedTestState.SystemDll;
            var fileSpecs = new FileSpec[]
            {
                new FileSpec(Binaries.AppHost.FilePath, BundlerHostName),
                new FileSpec(appPath, "rel/app.repeat.dll"),
                new FileSpec(appPath, "rel/app.repeat.dll"),
                new FileSpec(appPath, "rel/system.repeat.dll"),
                new FileSpec(systemLibPath, "rel/system.repeat.dll"),
            };

            Bundler bundler = CreateBundlerInstance();
            Assert.Throws<ArgumentException>(() => bundler.GenerateBundle(fileSpecs))
                .Message
                    .Should().Contain("rel/system.repeat.dll")
                    .And.NotContain("rel/app.repeat.dll")
                    .And.Contain(appPath)
                    .And.Contain(systemLibPath);
        }

        [Fact]
        public void BaseNameComputation()
        {
            // Create an app with multiple periods in its name to check that baseName computation
            // (and consequently deps.json and runtimeconfig.json name computations) in the bundler
            // work correctly in the presence of "."s in the hostName.
            using (var app = TestApp.CreateEmpty("App.With.Periods"))
            {
                app.PopulateFrameworkDependent(Constants.MicrosoftNETCoreApp, TestContext.MicrosoftNETCoreAppVersion);

                string hostName = Path.GetFileName(app.AppExe);
                string depsJsonName = Path.GetFileName(app.DepsJson);
                string runtimeConfigName = Path.GetFileName(app.RuntimeConfigJson);
                FileSpec[] fileSpecs = new FileSpec[]
                {
                    new FileSpec(Binaries.AppHost.FilePath, hostName),
                    new FileSpec(app.AppDll, Path.GetRelativePath(app.Location, app.AppDll)),
                    new FileSpec(app.DepsJson, depsJsonName),
                    new FileSpec(app.RuntimeConfigJson, runtimeConfigName),
                };

                var bundleDir = new DirectoryInfo(app.GetUniqueSubdirectory("bundle"));
                var bundler = new Bundler(hostName, bundleDir.FullName);
                bundler.GenerateBundle(fileSpecs);


                bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals(depsJsonName)).Single().Type.Should().Be(FileType.DepsJson);
                bundler.BundleManifest.Files.Where(entry => entry.RelativePath.Equals(runtimeConfigName)).Single().Type.Should().Be(FileType.RuntimeConfigJson);
                bundleDir.Should().NotHaveFile(depsJsonName);
                bundleDir.Should().NotHaveFile(runtimeConfigName);
            }
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleOtherFiles)]
        [InlineData(BundleOptions.BundleAllContent)]
        [InlineData(BundleOptions.BundleSymbolFiles)]
        [Theory]
        public void BundleOptions_IncludedExcludedFiles(BundleOptions options)
        {
            TestApp app = sharedTestState.App;
            string devJsonName = Path.GetFileName(app.RuntimeDevConfigJson);
            string appSymbolName = $"{app.Name}.pdb";
            string otherContentName = "other.txt";
            FileSpec[] fileSpecs = new FileSpec[]
            {
                new FileSpec(Binaries.AppHost.FilePath, BundlerHostName),
                new FileSpec(app.AppDll, Path.GetRelativePath(app.Location, app.AppDll)),
                new FileSpec(app.DepsJson, Path.GetRelativePath(app.Location, app.DepsJson)),
                new FileSpec(app.RuntimeConfigJson, Path.GetRelativePath(app.Location, app.RuntimeConfigJson)),
                new FileSpec(app.RuntimeConfigJson, devJsonName),
                new FileSpec(Path.Combine(app.Location, appSymbolName), appSymbolName),
                new FileSpec(Binaries.CoreClr.FilePath, Binaries.CoreClr.FileName),
                new FileSpec(app.RuntimeConfigJson, otherContentName),
            };

            Bundler bundler = CreateBundlerInstance(options);
            bundler.GenerateBundle(fileSpecs);

            // App's dll, .deps.json, and .runtimeconfig.json should always be bundled
            Assert.True(bundler.BundleManifest.Contains(Path.GetFileName(app.AppDll)));
            Assert.True(bundler.BundleManifest.Contains(Path.GetFileName(app.DepsJson)));
            Assert.True(bundler.BundleManifest.Contains(Path.GetFileName(app.RuntimeConfigJson)));

            // App's .runtimeconfig.dev.json is always excluded
            Assert.False(bundler.BundleManifest.Contains(devJsonName));

            // Symbols should only be bundled if option is explicitly set
            bundler.BundleManifest.Contains(appSymbolName).Should().Be(options.HasFlag(BundleOptions.BundleSymbolFiles));

            // Native libararies should only be bundled if option is explicitly set
            bundler.BundleManifest.Contains(Binaries.CoreClr.FileName).Should().Be(options.HasFlag(BundleOptions.BundleNativeBinaries));

            // Other files should only be bundled if option is explicitly set
            bundler.BundleManifest.Contains(otherContentName).Should().Be(options.HasFlag(BundleOptions.BundleOtherFiles));
        }

        [Fact]
        public void FileSizes()
        {
            var app = sharedTestState.App;
            List<FileSpec> fileSpecs = new List<FileSpec>
            {
                new FileSpec(Binaries.AppHost.FilePath, BundlerHostName),
                new FileSpec(app.AppDll, Path.GetRelativePath(app.Location, app.AppDll)),
                new FileSpec(app.DepsJson, Path.GetRelativePath(app.Location, app.DepsJson)),
                new FileSpec(app.RuntimeConfigJson, Path.GetRelativePath(app.Location, app.RuntimeConfigJson)),
            };
            fileSpecs.AddRange(SingleFileTestApp.GetRuntimeFilesToBundle());

            Bundler bundler = CreateBundlerInstance();
            bundler.GenerateBundle(fileSpecs);
            foreach (FileEntry file in bundler.BundleManifest.Files)
            {
                var spec = fileSpecs.Single(f => f.BundleRelativePath == file.RelativePath);
                Assert.True(file.Size == new FileInfo(spec.SourcePath).Length);
            }
        }

        [Fact]
        public void AssemblyAlignment()
        {
            var app = sharedTestState.App;
            List<FileSpec> fileSpecs = new List<FileSpec>
            {
                new FileSpec(Binaries.AppHost.FilePath, BundlerHostName),
                new FileSpec(app.AppDll, Path.GetRelativePath(app.Location, app.AppDll)),
            };
            fileSpecs.AddRange(SingleFileTestApp.GetRuntimeFilesToBundle());

            Bundler bundler = CreateBundlerInstance();
            bundler.GenerateBundle(fileSpecs);

            var alignment = OperatingSystem.IsLinux() && RuntimeInformation.OSArchitecture == Architecture.Arm64 ? 4096 : 16;
            bundler.BundleManifest.Files.ForEach(file =>
                Assert.True((file.Type != FileType.Assembly) || (file.Offset % alignment == 0)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void Codesign(bool shouldCodesign)
        {
            TestApp app = sharedTestState.App;
            FileSpec[] fileSpecs = new FileSpec[]
            {
                new FileSpec(Binaries.AppHost.FilePath, BundlerHostName),
                new FileSpec(app.AppDll, Path.GetRelativePath(app.Location, app.AppDll)),
                new FileSpec(app.DepsJson, Path.GetRelativePath(app.Location, app.DepsJson)),
                new FileSpec(app.RuntimeConfigJson, Path.GetRelativePath(app.Location, app.RuntimeConfigJson)),
            };

            Bundler bundler = CreateBundlerInstance(macosCodesign: shouldCodesign);
            string bundledApp = bundler.GenerateBundle(fileSpecs);

            // Check if the file is signed
            CommandResult result = Command.Create("codesign", $"-v {bundledApp}")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(expectedToFail: !shouldCodesign);

            if (shouldCodesign)
            {
                result.Should().Pass();
            }
            else
            {
                result.Should().Fail();
            }
        }

        public class SharedTestState : IDisposable
        {
            public const string AppName = "HelloWorld";
            public TestApp App { get; }
            public string SystemDll { get; }

            public SharedTestState()
            {
                App = TestApp.CreateFromBuiltAssets(AppName);

                SystemDll = Path.Combine(TestContext.BuiltDotNet.GreatestVersionSharedFxPath, "System.dll");
            }

            public void Dispose()
            {
                App.Dispose();
            }
        }
    }
}
