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
    public class BundleExtractToSpecificPath : BundleTestBase, IClassFixture<BundleExtractToSpecificPath.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleExtractToSpecificPath(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        private void AbsolutePath()
        {
            SingleFileTestApp app = sharedTestState.SelfContainedApp;

            // Publish the bundle
            BundleOptions options = BundleOptions.BundleNativeBinaries;
            Manifest manifest;
            string singleFile = sharedTestState.SelfContainedApp.Bundle(options, out manifest);

            // Verify expected files in the bundle directory
            var bundleDir = Directory.GetParent(singleFile);
            bundleDir.Should().OnlyHaveFiles(new[]
            {
                Binaries.GetExeFileNameForCurrentPlatform(app.Name),
                $"{app.Name}.pdb"
            });

            // Directory for extraction.
            string extractBaseDir = app.GetNewExtractionRootPath();

            // Run the bundled app for the first time, and extract files to
            // $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/bundle-id
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractBaseDir)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

            var extractDir = app.GetExtractionDir(extractBaseDir, manifest);
            extractDir.Should().OnlyHaveFiles(BundleHelper.GetExtractedFiles(manifest, options));
        }

        [InlineData("./foo", BundleOptions.BundleAllContent)]
        [InlineData("../foo", BundleOptions.BundleAllContent)]
        [InlineData("foo", BundleOptions.BundleAllContent)]
        [InlineData("foo/bar", BundleOptions.BundleAllContent)]
        [InlineData("./foo", BundleOptions.BundleNativeBinaries)]
        [InlineData("../foo", BundleOptions.BundleNativeBinaries)]
        [InlineData("foo", BundleOptions.BundleNativeBinaries)]
        [InlineData("foo/bar", BundleOptions.BundleNativeBinaries)]
        [InlineData("foo\\bar", BundleOptions.BundleNativeBinaries)]
        [Theory]
        private void RelativePath(string relativePath, BundleOptions bundleOptions)
        {
            // As we don't modify user defined environment variables, we will not convert
            // any forward slashes to the standard Windows dir separator ('\'), thus
            // failing to create directory trees for bundle extraction that use Unix
            // style dir separator in Windows.
            if (relativePath == "foo/bar" && OperatingSystem.IsWindows())
                return;

            // Similarly on non-Windows OSes, we don't convert backslash directory separators
            // to forward ones.
            if (relativePath == "foo\\bar" && !OperatingSystem.IsWindows())
                return;

            Manifest manifest;
            string singleFile = sharedTestState.SelfContainedApp.Bundle(bundleOptions, out manifest);

            // Run the bundled app (extract files to <path>)
            Command.Create(singleFile)
                .WorkingDirectory(Path.GetDirectoryName(singleFile))
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, relativePath)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

            using (TestArtifact extractionRoot = new TestArtifact(Path.Combine(Path.GetDirectoryName(singleFile), relativePath)))
            {
                var extractedDir = sharedTestState.SelfContainedApp.GetExtractionDir(extractionRoot.Location, manifest);
                var extractedFiles = BundleHelper.GetExtractedFiles(manifest, bundleOptions);
                extractedDir.Should().OnlyHaveFiles(extractedFiles);
            }
        }

        [Fact]
        private void ExtractionDirectoryReused()
        {
            var app = sharedTestState.SelfContainedApp;

            // Publish the bundle
            Manifest manifest;
            BundleOptions options = BundleOptions.BundleNativeBinaries;
            string singleFile = app.Bundle(options, out manifest);

            // Directory for extraction.
            string extractBaseDir = app.GetNewExtractionRootPath();

            // Run the bunded app for the first time, and extract files to
            // $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/bundle-id
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractBaseDir)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

            var extractDir = app.GetExtractionDir(extractBaseDir, manifest);
            extractDir.Refresh();
            DateTime firstWriteTime = extractDir.LastWriteTimeUtc;

            while (DateTime.Now == firstWriteTime)
            {
                Thread.Sleep(1);
            }

            // Run the bundled app again (reuse extracted files)
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractBaseDir)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

            extractDir.Should().NotBeModifiedAfter(firstWriteTime);
        }

        [Fact]
        private void RecoverMissingFiles()
        {
            var app = sharedTestState.SelfContainedApp;

            // Publish the bundle
            Manifest manifest;
            BundleOptions options = BundleOptions.BundleNativeBinaries;
            string singleFile = app.Bundle(options, out manifest);

            // Directory for extraction.
            string extractBaseDir = app.GetNewExtractionRootPath();

            // Run the bunded app for the first time, and extract files to
            // $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/bundle-id
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractBaseDir)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

            // Remove the extracted files, but keep the extraction directory
            var extractDir = app.GetExtractionDir(extractBaseDir, manifest);
            var extractedFiles = BundleHelper.GetExtractedFiles(manifest, options);

            foreach (string file in extractedFiles)
                File.Delete(Path.Combine(extractDir.FullName, file));

            extractDir.Should().Exist();
            extractDir.Should().NotHaveFiles(extractedFiles);

            // Run the bundled app again (recover deleted files)
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractBaseDir)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

            extractDir.Should().OnlyHaveFiles(extractedFiles);
        }

        [Fact]
        private void NonexistentDefault_Fails()
        {
            string nonExistentPath = Path.Combine(
                Path.GetDirectoryName(sharedTestState.BundledAppExecutablePath),
                "nonexistent");

            string defaultExpansionEnvVariable = OperatingSystem.IsWindows() ? "TMP" : "HOME";
            string expectedErrorMessagePart = OperatingSystem.IsWindows() ?
                $"Failed to determine default extraction location. Check if 'TMP'" :
                $"Default extraction directory [{nonExistentPath}] either doesn't exist or is not accessible for read/write.";

            Command.Create(sharedTestState.BundledAppExecutablePath)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(defaultExpansionEnvVariable, nonExistentPath)
                .Execute().Should().Fail()
                .And.HaveStdErrContaining(expectedErrorMessagePart);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "On Windows the default extraction path is determined by calling GetTempPath which looks at multiple places and can't really be undefined.")]
        private void UndefinedHOME_getpwuidFallback()
        {
            string home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home))
            {
                // HOME is not set. Use System.Environment (which also fallsback to getpwuid)
                home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            Command.Create(sharedTestState.BundledAppExecutablePath)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("HOME", null)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

            DirectoryInfo expectedExtractDir = sharedTestState.SelfContainedApp.GetExtractionDir(Path.Combine(home, ".net"), sharedTestState.BundledAppManifest);
            var extractedFiles = BundleHelper.GetExtractedFiles(sharedTestState.BundledAppManifest, BundleOptions.BundleNativeBinaries);
            expectedExtractDir.Should().HaveFiles(extractedFiles);
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public string BundledAppExecutablePath { get; }
            public Manifest BundledAppManifest { get; }

            public SingleFileTestApp SelfContainedApp { get; }

            public SharedTestState()
            {
                SelfContainedApp = SingleFileTestApp.CreateSelfContained("HelloWorld");

                // Copy over mockcoreclr so that the app will have a native binary
                File.Copy(Binaries.CoreClr.MockPath, Path.Combine(SelfContainedApp.NonBundledLocation, Binaries.CoreClr.MockName));

                // Create a bundled app that can be used by multiple tests
                BundledAppExecutablePath = SelfContainedApp.Bundle(BundleOptions.BundleNativeBinaries, out Manifest manifest);
                BundledAppManifest = manifest;
            }

            public void Dispose()
            {
                SelfContainedApp.Dispose();
            }
        }
    }
}
