// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.CoreSetup.Test;
using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.Tools.Publish.Tests
{
    public class DotnetTestXunit
    {
        private string DotnetTestXunitVersion => "1.0.0-rc2-192208-24";
        private RepoDirectoriesProvider RepoDirectories { get; set; }

        public DotnetTestXunit()
        {
            RepoDirectories = new RepoDirectoriesProvider();
        }

        [Fact]
        public void Muxer_activation_of_dotnet_test_XUnit_on_Portable_Test_App_Succeeds()
        {
            using (var portableTestAppFixture = new TestProjectFixture("PortableTestApp", RepoDirectories))
            {
                portableTestAppFixture
                    .EnsureRestored()
                    .BuildProject();

                ActivateDotnetTestXunitOnTestProject(RepoDirectories, portableTestAppFixture);
            }
        }

        [Fact]
        public void Muxer_activation_of_dotnet_test_XUnit_on_Standalone_Test_App_Succeeds()
        {
            using (var standaloneTestAppFixture = new TestProjectFixture("StandaloneTestApp", RepoDirectories))
            {
                standaloneTestAppFixture
                    .EnsureRestoredForRid(standaloneTestAppFixture.CurrentRid)
                    .BuildProject(runtime: standaloneTestAppFixture.CurrentRid);

                ActivateDotnetTestXunitOnTestProject(RepoDirectories, standaloneTestAppFixture);
            }
        }

        private void ActivateDotnetTestXunitOnTestProject(
            RepoDirectoriesProvider repoDirectories,
            TestProjectFixture testProjectFixture)
        {
            var dotnet = testProjectFixture.BuiltDotnet;

            var dotnetTestXunitDll = FindDotnetTestXunitDll(repoDirectories, DotnetTestXunitVersion);
            var depsJson = testProjectFixture.TestProject.DepsJson;
            var runtimeConfig = testProjectFixture.TestProject.RuntimeConfigJson;
            var additionalProbingPath = RepoDirectories.NugetPackages;
            var appDll = testProjectFixture.TestProject.AppDll;

            dotnet.Exec(
                    "exec",
                    "--runtimeconfig", runtimeConfig,
                    "--depsfile", depsJson,
                    "--additionalProbingPath", additionalProbingPath,
                    dotnetTestXunitDll,
                    appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(expectedToFail:true)
                .Should().Fail()
                .And.HaveStdOutContaining("Total: 2")
                .And.HaveStdOutContaining("Failed: 1");
        }

        private string FindDotnetTestXunitDll(RepoDirectoriesProvider repoDirectories, string dotnetTestXunitVersion)
        {
            var dotnetTestXunitDll = Path.Combine(
                repoDirectories.NugetPackages,
                "dotnet-test-xunit",
                dotnetTestXunitVersion,
                "lib",
                "netcoreapp1.0",
                "dotnet-test-xunit.dll");

            if ( ! File.Exists(dotnetTestXunitDll))
            {
                throw new Exception(
                    $"Unable to find '{dotnetTestXunitDll}', ensure {nameof(DotnetTestXunitVersion)} is updated to the version in Portable/StandaloneTestApp");
            }

            return dotnetTestXunitDll;
        }
    }
}
