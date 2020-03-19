// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace BundleTests.Helpers
{
    public static class BundleHelper
    {
        public const string DotnetBundleExtractBaseEnvVariable = "DOTNET_BUNDLE_EXTRACT_BASE_DIR";

        public static string GetHostPath(TestProjectFixture fixture)
        {
            return Path.Combine(GetPublishPath(fixture), GetHostName(fixture));
        }

        public static string GetAppPath(TestProjectFixture fixture)
        {
            return Path.Combine(GetPublishPath(fixture), GetAppName(fixture));
        }

        public static string GetPublishedSingleFilePath(TestProjectFixture fixture)
        {
            return GetHostPath(fixture);
        }

        public static string GetHostName(TestProjectFixture fixture)
        {
            return Path.GetFileName(fixture.TestProject.AppExe);
        }

        public static string GetAppName(TestProjectFixture fixture)
        {
            return Path.GetFileName(fixture.TestProject.AppDll);
        }

        public static string GetPublishPath(TestProjectFixture fixture)
        {
            return Path.Combine(fixture.TestProject.ProjectDirectory, "publish");
        }

        public static DirectoryInfo GetBundleDir(TestProjectFixture fixture)
        {
            return Directory.CreateDirectory(Path.Combine(fixture.TestProject.ProjectDirectory, "bundle"));
        }

        public static DirectoryInfo GetExtractDir(TestProjectFixture fixture)
        {
            return Directory.CreateDirectory(Path.Combine(fixture.TestProject.ProjectDirectory, "extract"));
        }

        /// Generate a bundle containind the (embeddable) files in sourceDir
        public static string GenerateBundle(Bundler bundler, string sourceDir)
        {
            // Convert sourceDir to absolute path
            sourceDir = Path.GetFullPath(sourceDir);

            // Get all files in the source directory and all sub-directories.
            string[] sources = Directory.GetFiles(sourceDir, searchPattern: "*", searchOption: SearchOption.AllDirectories);

            // Sort the file names to keep the bundle construction deterministic.
            Array.Sort(sources, StringComparer.Ordinal);

            List<FileSpec> fileSpecs = new List<FileSpec>(sources.Length);
            foreach (var file in sources)
            {
                fileSpecs.Add(new FileSpec(file, Path.GetRelativePath(sourceDir, file)));
            }

            return bundler.GenerateBundle(fileSpecs);
        }

        // Bundle to a single-file
        // In several tests, the single-file bundle is created explicitly using Bundle API
        // instead of the SDK via /p:PublishSingleFile=true.
        // This is necessary when the test needs the latest changes in the AppHost, 
        // which may not (yet) be available in the SDK.
        //
        // Currently, AppHost can only handle bundles if all content is extracted to disk on startup.
        // Therefore, the BundleOption is BundleAllContent by default.
        // The default should be BundleOptions.None once host/runtime no longer requires full-extraction.
        public static string BundleApp(TestProjectFixture fixture,
                                       BundleOptions options = BundleOptions.BundleAllContent,
                                       Version targetFrameworkVersion = null)
        {
            var hostName = GetHostName(fixture);
            string publishPath = GetPublishPath(fixture);
            var bundleDir = GetBundleDir(fixture);

            var bundler = new Bundler(hostName, bundleDir.FullName, options, targetFrameworkVersion: targetFrameworkVersion);
            string singleFile = GenerateBundle(bundler, publishPath);
            return singleFile;
        }

        public static void AddLongNameContentToAppWithSubDirs(TestProjectFixture fixture)
        {
            // For tests using the AppWithSubDirs, One of the sub-directories with a really long name
            // is generated during test-runs rather than being checked in as a test asset.
            // This prevents git-clone of the repo from failing if long-file-name support is not enabled on windows.
            var longDirName = "This is a really, really, really, really, really, really, really, really, really, really, really, really, really, really long file name for punctuation";
            var longDirPath = Path.Combine(fixture.TestProject.ProjectDirectory, "Sentence", longDirName);
            Directory.CreateDirectory(longDirPath);
            using (var writer = File.CreateText(Path.Combine(longDirPath, "word")))
            {
                writer.Write(".");
            }
        }

        public static void AddEmptyContentToApp(TestProjectFixture fixture)
        {
            XDocument projectDoc = XDocument.Load(fixture.TestProject.ProjectFile);
            projectDoc.Root.Add(
                new XElement("ItemGroup",
                    new XElement("Content",
                        new XAttribute("Include", "empty.txt"),
                        new XElement("CopyToOutputDirectory", "PreserveNewest"))));
            projectDoc.Save(fixture.TestProject.ProjectFile);
            File.WriteAllBytes(Path.Combine(fixture.TestProject.Location, "empty.txt"), new byte[0]);
        }

    }
}
