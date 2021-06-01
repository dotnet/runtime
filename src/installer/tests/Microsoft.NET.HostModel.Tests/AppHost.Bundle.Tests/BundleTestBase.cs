// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.IO;
using BundleTests.Helpers;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.Bundle;

namespace AppHost.Bundle.Tests
{
    public abstract class BundleTestBase
    {
        // This helper is used in lieu of SDK support for publishing apps using the singlefilehost.
        // It replaces the apphost with singlefilehost, and along with appropriate app.dll updates in the host.
        // For now, we leave behind the hostpolicy and hostfxr DLLs in the publish directory, because
        // removing them requires deps.json update.
        public static string UseSingleFileSelfContainedHost(TestProjectFixture testFixture)
        {
            var singleFileHost = Path.Combine(
                testFixture.RepoDirProvider.HostArtifacts,
                RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("singlefilehost"));
            var publishedHostPath = BundleHelper.GetHostPath(testFixture);
            HostWriter.CreateAppHost(singleFileHost,
                                     publishedHostPath,
                                     BundleHelper.GetAppPath(testFixture));
            return publishedHostPath;
        }

        public static string UseFrameworkDependentHost(TestProjectFixture testFixture)
        {
            var appHost = Path.Combine(
                testFixture.RepoDirProvider.HostArtifacts,
                RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("apphost"));
            var publishedHostPath = BundleHelper.GetHostPath(testFixture);
            HostWriter.CreateAppHost(appHost,
                                     publishedHostPath,
                                     BundleHelper.GetAppPath(testFixture));
            return publishedHostPath;
        }

        public static string BundleSelfContainedApp(
            TestProjectFixture testFixture,
            BundleOptions options = BundleOptions.None,
            Version targetFrameworkVersion = null,
            bool disableCompression = false)
        {
            string singleFile;
            BundleSelfContainedApp(testFixture, out singleFile, options, targetFrameworkVersion);
            return singleFile;
        }

        public static Bundler BundleSelfContainedApp(
            TestProjectFixture testFixture,
            out string singleFile,
            BundleOptions options = BundleOptions.None,
            Version targetFrameworkVersion = null,
            bool disableCompression = false)
        {
            UseSingleFileSelfContainedHost(testFixture);
            if (targetFrameworkVersion == null || targetFrameworkVersion >= new Version(6, 0))
            {
                options |= BundleOptions.EnableCompression;
            }

            return BundleHelper.BundleApp(testFixture, out singleFile, options, targetFrameworkVersion);
        }

        public abstract class SharedTestStateBase
        {
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestStateBase()
            {
                RepoDirectories = new RepoDirectoriesProvider();
            }

            public TestProjectFixture PreparePublishedSelfContainedTestProject(string projectName, params string[] extraArgs)
            {
                var testFixture = new TestProjectFixture(projectName, RepoDirectories);
                testFixture
                    .EnsureRestoredForRid(testFixture.CurrentRid)
                    .PublishProject(runtime: testFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(testFixture),
                                    extraArgs: extraArgs);

                return testFixture;
            }
        }
    }
}
