// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.CoreSetup.Test;
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

        // Bundle to a single-file
        // This step should be removed in favor of publishing with /p:PublishSingleFile=true
        // once core-setup tests use 3.0 SDK 
        public static string BundleApp(TestProjectFixture fixture)
        {
            var hostName = GetHostName(fixture);
            string publishPath = GetPublishPath(fixture);
            var bundleDir = GetBundleDir(fixture);

            var bundler = new Microsoft.NET.HostModel.Bundle.Bundler(hostName, bundleDir.FullName);
            string singleFile = bundler.GenerateBundle(publishPath);
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
