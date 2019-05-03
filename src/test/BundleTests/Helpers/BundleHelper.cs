// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.DotNet.CoreSetup.Test;

namespace BundleTests.Helpers
{
    public static class BundleHelper
    {
        public const string DotnetBundleExtractBaseEnvVariable = "DOTNET_BUNDLE_EXTRACT_BASE_DIR";
        public static string GetHostName(TestProjectFixture fixture)
        {
            return Path.GetFileName(fixture.TestProject.AppExe);
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

    }
}
