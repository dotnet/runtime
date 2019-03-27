// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test.BundleTests.BundleExtract
{
    public class BundleAndExtract : IClassFixture<BundleAndExtract.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleAndExtract(BundleAndExtract.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Test()
        {
            var fixture = sharedTestState.TestFixture
                .Copy();

            var dotnet = fixture.SdkDotnet;
            var appDll = fixture.TestProject.AppDll;
            var hostName = Path.GetFileName(fixture.TestProject.AppExe);
            var publishDir = fixture.TestProject.OutputDirectory;

            // Run the App normally
           Command.Create(Path.Combine(publishDir, hostName))
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Wow! We now say hello to the big world and you.");

            // Create a directory for bundle/extraction output.
            // This directory shouldn't be within TestProject.OutputDirectory, since the bundler
            // will (attempt to) embed all files below the TestProject.OutputDirectory tree into one file.
            string singleFileDir = Path.Combine(fixture.TestProject.ProjectDirectory, "oneExe");
            Directory.CreateDirectory(singleFileDir);

            // Bundle to a single-file
            string bundleDll = Path.Combine(sharedTestState.RepoDirectories.Artifacts,
                                            "Microsoft.DotNet.Build.Bundle",
                                            "netcoreapp2.0",
                                            "Microsoft.DotNet.Build.Bundle.dll");
            string[] bundleArgs = { "--source", publishDir,
                                    "--apphost", hostName,
                                    "--output", singleFileDir };

            dotnet.Exec(bundleDll, bundleArgs)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass();

            // Extract the contents
            string singleFile = Path.Combine(singleFileDir, hostName);
            string[] extractArgs = { "--extract", singleFile,
                                     "--output", singleFileDir };

            dotnet.Exec(bundleDll, extractArgs)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass();

            // Run the extracted app
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Wow! We now say hello to the big world and you.");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("StandaloneAppWithSubDirs", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid);
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
