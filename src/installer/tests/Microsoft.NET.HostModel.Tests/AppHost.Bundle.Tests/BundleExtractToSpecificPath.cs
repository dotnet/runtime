// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace AppHost.Bundle.Tests
{
    [Trait("category", "FlakyAppHostTests")]
    public class BundleExtractToSpecificPath : BundleTestBase, IClassFixture<BundleExtractToSpecificPath.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleExtractToSpecificPath(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        private void Bundle_Extraction_To_Specific_Path_Succeeds()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var hostName = BundleHelper.GetHostName(fixture);

            // Publish the bundle
            BundleOptions options = BundleOptions.BundleNativeBinaries;
            Bundler bundler = BundleSelfContainedApp(fixture, out string singleFile, options);

            // Verify expected files in the bundle directory
            var bundleDir = BundleHelper.GetBundleDir(fixture);
            bundleDir.Should().HaveFile(hostName);
            bundleDir.Should().NotHaveFiles(BundleHelper.GetBundledFiles(fixture));

            // Create a directory for extraction.
            var extractBaseDir = BundleHelper.GetExtractionRootDir(fixture);
            extractBaseDir.Should().NotHaveDirectory(BundleHelper.GetAppBaseName(fixture));

            // Run the bundled app for the first time, and extract files to
            // $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/bundle-id
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractBaseDir.FullName)
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            var extractDir = BundleHelper.GetExtractionDir(fixture, bundler);
            extractDir.Should().HaveFiles(BundleHelper.GetExtractedFiles(fixture, BundleOptions.BundleNativeBinaries));
            extractDir.Should().NotHaveFiles(BundleHelper.GetFilesNeverExtracted(fixture));
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
        private void Bundle_Extraction_To_Relative_Path_Succeeds(string relativePath, BundleOptions bundleOptions)
        {
            // As we don't modify user defined environment variables, we will not convert
            // any forward slashes to the standard Windows dir separator ('\'), thus
            // failing to create directory trees for bundle extraction that use Unix
            // style dir separator in Windows.
            if (relativePath == "foo/bar" && OperatingSystem.IsWindows())
                return;

            var fixture = sharedTestState.TestFixture.Copy();
            var bundler = BundleSelfContainedApp(fixture, out var singleFile, bundleOptions);

            // Run the bundled app (extract files to <path>)
            var cmd = Command.Create(singleFile);
            cmd.WorkingDirectory(Path.GetDirectoryName(singleFile))
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, relativePath)
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            var extractedFiles = BundleHelper.GetExtractedFiles(fixture, bundleOptions);
            var extractedDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(singleFile),
                relativePath,
                fixture.TestProject.ProjectName,
                bundler.BundleManifest.BundleID));

            extractedDir.Should().HaveFiles(extractedFiles);
        }

        [Fact]
        private void Bundle_extraction_is_reused()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            // Publish the bundle
            BundleOptions options = BundleOptions.BundleNativeBinaries;
            Bundler bundler = BundleSelfContainedApp(fixture, out string singleFile, options);

            // Create a directory for extraction.
            var extractBaseDir = BundleHelper.GetExtractionRootDir(fixture);

            // Run the bunded app for the first time, and extract files to
            // $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/bundle-id
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractBaseDir.FullName)
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            var extractDir = BundleHelper.GetExtractionDir(fixture, bundler);

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
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractBaseDir.FullName)
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            extractDir.Should().NotBeModifiedAfter(firstWriteTime);
        }

        [Fact]
        private void Bundle_extraction_can_recover_missing_files()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var hostName = BundleHelper.GetHostName(fixture);
            var appName = Path.GetFileNameWithoutExtension(hostName);

            // Publish the bundle
            BundleOptions options = BundleOptions.BundleNativeBinaries;
            Bundler bundler = BundleSelfContainedApp(fixture, out string singleFile, options);

            // Create a directory for extraction.
            var extractBaseDir = BundleHelper.GetExtractionRootDir(fixture);

            // Run the bunded app for the first time, and extract files to
            // $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/bundle-id
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractBaseDir.FullName)
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            // Remove the extracted files, but keep the extraction directory
            var extractDir = BundleHelper.GetExtractionDir(fixture, bundler);
            var extractedFiles = BundleHelper.GetExtractedFiles(fixture, BundleOptions.BundleNativeBinaries);

            Array.ForEach(extractedFiles, file => File.Delete(Path.Combine(extractDir.FullName, file)));

            extractDir.Should().Exist();
            extractDir.Should().NotHaveFiles(extractedFiles);

            // Run the bundled app again (recover deleted files)
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractBaseDir.FullName)
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            extractDir.Should().HaveFiles(extractedFiles);
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                TestFixture = PreparePublishedSelfContainedTestProject("StandaloneApp");
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
