// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class HammerServiceTest : IClassFixture<HammerServiceTest.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public HammerServiceTest(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        private void SingleFile_Apps_Are_Serviced()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, the hammer servicing location is %ProgramFiles%\coreservicing.
                // Since writing to this location requires administrative privilege, we do not run the test on Windows.
                // On Unix systems, the servicing location is obtained from the environment variable $CORE_SERVICING.

                return;
            }

            var fixture = sharedTestState.TestFixture.Copy();
            var servicer = sharedTestState.ServiceFixture.Copy();

            // Annotate the app as servicible, and then publish to a single-file.
            string depsjson = BundleHelper.GetDepsJsonPath(fixture);
            File.WriteAllText(depsjson, File.ReadAllText(depsjson).Replace("\"serviceable\": false", "\"serviceable\": true"));
            var singleFile = BundleHelper.BundleApp(fixture);

            // Create the servicing directory, and copy the servived DLL from service fixture to the servicing directory.
            var serviceBasePath = Path.Combine(fixture.TestProject.ProjectDirectory, "coreservicing");
            var servicePath = Path.Combine(serviceBasePath, "pkgs", BundleHelper.GetAppBaseName(servicer), "1.0.0");
            Directory.CreateDirectory(servicePath);
            File.Copy(BundleHelper.GetAppPath(servicer), Path.Combine(servicePath, BundleHelper.GetAppName(servicer)));

            // Verify that the test DLL is loaded from the bundle when not being serviced
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hi Bellevue!");

            // Verify that the test DLL is loaded from the servicing location when being serviced
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.CoreServicingEnvVariable, serviceBasePath)
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hi Bengaluru!");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public TestProjectFixture ServiceFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();
                TestFixture = new TestProjectFixture("HammerServiceApp", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid, 
                                    outputDirectory: BundleHelper.GetPublishPath(TestFixture));

                ServiceFixture = new TestProjectFixture("ServicedLocation", RepoDirectories, assemblyName: "Location");
                ServiceFixture
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject(outputDirectory: BundleHelper.GetPublishPath(ServiceFixture));

            }

            public void Dispose()
            {
                TestFixture.Dispose();
                ServiceFixture.Dispose();
            }
        }
    }
}
