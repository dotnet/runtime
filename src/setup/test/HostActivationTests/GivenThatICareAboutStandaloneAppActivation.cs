// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.StandaloneApp
{
    public class GivenThatICareAboutStandaloneAppActivation
    {
        private static TestProjectFixture PreviouslyBuiltAndRestoredStandaloneTestProjectFixture { get; set; }
        private static TestProjectFixture PreviouslyPublishedAndRestoredStandaloneTestProjectFixture { get; set; }
        private static RepoDirectoriesProvider RepoDirectories { get; set; }

        static GivenThatICareAboutStandaloneAppActivation()
        {
            RepoDirectories = new RepoDirectoriesProvider();

            var buildFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
            buildFixture
                .EnsureRestored(RepoDirectories.CorehostPackages, RepoDirectories.CorehostDummyPackages)
                .BuildProject(runtime: buildFixture.CurrentRid);

            var publishFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
            publishFixture
                .EnsureRestored(RepoDirectories.CorehostPackages, RepoDirectories.CorehostDummyPackages)
                .PublishProject(runtime: publishFixture.CurrentRid);

            ReplaceTestProjectOutputHostInTestProjectFixture(buildFixture);

            PreviouslyBuiltAndRestoredStandaloneTestProjectFixture = buildFixture;
            PreviouslyPublishedAndRestoredStandaloneTestProjectFixture = publishFixture;
        }

        [Fact]
        public void Running_Build_Output_Standalone_EXE_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = PreviouslyBuiltAndRestoredStandaloneTestProjectFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = PreviouslyPublishedAndRestoredStandaloneTestProjectFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }

        /*
         * This method is needed to workaround dotnet build not placing the host from the package
         * graph in the build output.
         * https://github.com/dotnet/cli/issues/2343
         */
        private static void ReplaceTestProjectOutputHostInTestProjectFixture(TestProjectFixture testProjectFixture)
        {
            var dotnet = testProjectFixture.BuiltDotnet;

            var testProjectHost = testProjectFixture.TestProject.AppExe;
            var testProjectHostPolicy = testProjectFixture.TestProject.HostPolicyDll;
            var testProjectHostFxr = testProjectFixture.TestProject.HostFxrDll;

            if (!File.Exists(testProjectHost) || !File.Exists(testProjectHostPolicy))
            {
                throw new Exception("host or hostpolicy does not exist in test project output. Is this a standalone app?");
            }

            var dotnetHost = Path.Combine(dotnet.GreatestVersionSharedFxPath, $"dotnet{testProjectFixture.ExeExtension}");
            var dotnetHostPolicy = Path.Combine(dotnet.GreatestVersionSharedFxPath, $"{testProjectFixture.SharedLibraryPrefix}hostpolicy{testProjectFixture.SharedLibraryExtension}");
            var dotnetHostFxr = Path.Combine(dotnet.GreatestVersionSharedFxPath, $"{testProjectFixture.SharedLibraryPrefix}hostfxr{testProjectFixture.SharedLibraryExtension}");

            File.Copy(dotnetHost, testProjectHost, true);
            File.Copy(dotnetHostPolicy, testProjectHostPolicy, true);

            if (File.Exists(testProjectHostFxr))
            {
                File.Copy(dotnetHostFxr, testProjectHostFxr, true);
            }
        }
    }
}
