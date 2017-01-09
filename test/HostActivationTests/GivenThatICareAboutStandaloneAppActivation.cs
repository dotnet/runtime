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
using System.Security.Cryptography;
using System.Text;

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

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_with_Unbound_AppHost_Fails()
        {
            var fixture = PreviouslyPublishedAndRestoredStandaloneTestProjectFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;

            string hostExeName = $"apphost{Constants.ExeSuffix}";
            string builtAppHost = Path.Combine(RepoDirectories.HostArtifacts, hostExeName);
            File.Copy(builtAppHost, appExe, true);

            int exitCode = Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .ExitCode;

            if (CurrentPlatform.IsWindows)
            {
                exitCode.Should().Be(-2147450731);
            }
            else
            {
                // Some Unix flavors filter exit code to ubyte.
                (exitCode & 0xFF).Should().Be(0x95);
            }
        }

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_with_Bound_AppHost_Succeeds()
        {
            var fixture = PreviouslyPublishedAndRestoredStandaloneTestProjectFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;

            string hostExeName = $"apphost{Constants.ExeSuffix}";
            string builtAppHost = Path.Combine(RepoDirectories.HostArtifacts, hostExeName);
            string appName = Path.GetFileNameWithoutExtension(appExe);
            string appDll = $"{appName}.dll";
            string appDir = Path.GetDirectoryName(appExe);
            string appDirHostExe = Path.Combine(appDir, hostExeName);

            // Make a copy of apphost first, replace hash and overwrite app.exe, rather than
            // overwrite app.exe and edit in place, because the file is opened as "write" for
            // the replacement -- the test fails with ETXTBSY (exit code: 26) in Linux when
            // executing a file opened in "write" mode.
            File.Copy(builtAppHost, appDirHostExe, true);
            using (var sha256 = SHA256.Create())
            {
                // Replace the hash with the managed DLL name.
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes("foobar"));
                var hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower();  
                AppHostExtensions.SearchAndReplace(appDirHostExe, Encoding.UTF8.GetBytes(hashStr), Encoding.UTF8.GetBytes(appDll), true);
            }
            File.Copy(appDirHostExe, appExe, true);

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
