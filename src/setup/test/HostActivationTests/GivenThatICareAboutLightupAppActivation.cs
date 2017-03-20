﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
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

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.LightupApp
{
    public class GivenThatICareAboutLightupAppActivation
    {
        private static TestProjectFixture PreviouslyBuiltAndRestoredLightupLibTestProjectFixture { get; set; }
        private static TestProjectFixture PreviouslyPublishedAndRestoredLightupLibTestProjectFixture { get; set; }

        private static TestProjectFixture PreviouslyBuiltAndRestoredLightupAppTestProjectFixture { get; set; }
        private static TestProjectFixture PreviouslyPublishedAndRestoredLightupAppTestProjectFixture { get; set; }

        private static TestProjectFixture PreviouslyBuiltAndRestoredStandaloneLightupAppTestProjectFixture { get; set; }
        private static TestProjectFixture PreviouslyPublishedAndRestoredStandaloneLightupAppTestProjectFixture { get; set; }

        private static RepoDirectoriesProvider RepoDirectories { get; set; }

        static GivenThatICareAboutLightupAppActivation()
        {
            RepoDirectories = new RepoDirectoriesProvider();

            PreviouslyBuiltAndRestoredLightupLibTestProjectFixture = new TestProjectFixture("LightupLib", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages, RepoDirectories.CorehostDummyPackages)
                .BuildProject();

            PreviouslyPublishedAndRestoredLightupLibTestProjectFixture = new TestProjectFixture("LightupLib", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages, RepoDirectories.CorehostDummyPackages)
                .PublishProject();

            PreviouslyBuiltAndRestoredLightupAppTestProjectFixture = new TestProjectFixture("LightupClient", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages, RepoDirectories.CorehostDummyPackages)
                .BuildProject();

            PreviouslyPublishedAndRestoredLightupAppTestProjectFixture = new TestProjectFixture("LightupClient", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages, RepoDirectories.CorehostDummyPackages)
                .PublishProject();

            PreviouslyBuiltAndRestoredStandaloneLightupAppTestProjectFixture = new TestProjectFixture("LightupClientSA", RepoDirectories);

            PreviouslyBuiltAndRestoredStandaloneLightupAppTestProjectFixture.EnsureRestoredForRid(PreviouslyBuiltAndRestoredStandaloneLightupAppTestProjectFixture.CurrentRid, RepoDirectories.CorehostDummyPackages)
                .BuildProject(runtime: PreviouslyBuiltAndRestoredStandaloneLightupAppTestProjectFixture.CurrentRid);

            PreviouslyPublishedAndRestoredStandaloneLightupAppTestProjectFixture = new TestProjectFixture("LightupClientSA", RepoDirectories);

            PreviouslyPublishedAndRestoredStandaloneLightupAppTestProjectFixture.EnsureRestoredForRid(PreviouslyPublishedAndRestoredStandaloneLightupAppTestProjectFixture.CurrentRid,RepoDirectories.CorehostPackages, RepoDirectories.CorehostDummyPackages)
                .PublishProject(runtime: PreviouslyPublishedAndRestoredStandaloneLightupAppTestProjectFixture.CurrentRid);

            ReplaceTestProjectOutputHostInTestProjectFixture(PreviouslyBuiltAndRestoredStandaloneLightupAppTestProjectFixture);
        }

        // Attempt to run the app with lightup deps.json specified but lightup library missing in the expected 
        // probe locations.
        [Fact]
        public void Muxer_activation_of_LightupApp_NoLightupLib_Fails()
        {
            var fixtureLib = PreviouslyBuiltAndRestoredLightupLibTestProjectFixture
                .Copy();

            var fixtureApp = PreviouslyBuiltAndRestoredLightupAppTestProjectFixture
                .Copy();

            var dotnet = fixtureApp.BuiltDotnet;
            var appDll = fixtureApp.TestProject.AppDll;
            var libDepsJson = fixtureLib.TestProject.DepsJson;

            dotnet.Exec("exec", "--additional-deps", libDepsJson, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail:true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("Error: assembly specified in the dependencies manifest was not found -- package: \'lightuplib\', version: \'1.0.0\', path: \'LightupLib.dll\'");
        }

        // Attempt to run a standalone app with lightup deps.json specified but missing lightup library.
        [Fact]
        public void Standalone_activation_of_LightupApp_NoLightupLib_Fails()
        {
            var fixtureLib = PreviouslyBuiltAndRestoredLightupLibTestProjectFixture
                .Copy();

            var fixtureApp = PreviouslyBuiltAndRestoredStandaloneLightupAppTestProjectFixture
                .Copy();

            var appExe = fixtureApp.TestProject.AppExe;
            var libDepsJson = fixtureLib.TestProject.DepsJson;

            Command.Create(appExe, new string[]{"--additional-deps", libDepsJson})
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail:true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("Error: assembly specified in the dependencies manifest was not found -- package: \'lightuplib\', version: \'1.0.0\', path: \'LightupLib.dll\'");
        }

        // Attempt to run the app with lightup deps.json specified and lightup library present in the expected 
        // probe locations.
        [Fact]
        public void Muxer_activation_of_LightupApp_WithLightupLib_Succeeds()
        {
            var fixtureLib = PreviouslyBuiltAndRestoredLightupLibTestProjectFixture
                .Copy();

            var fixtureApp = PreviouslyBuiltAndRestoredLightupAppTestProjectFixture
                .Copy();

            var dotnet = fixtureApp.BuiltDotnet;
            var appDll = fixtureApp.TestProject.AppDll;
            var libDll = fixtureLib.TestProject.AppDll;

            // Get the version number of the SharedFX we just built since that is the version
            // going to be specified in the test's runtimeconfig.json.
            var builtSharedFXVersion = Path.GetFileName(dotnet.GreatestVersionSharedFxPath);

            // Create the M.N.App specific folder where lightup.deps.json can be found.
            var baseDir = fixtureApp.TestProject.ProjectDirectory;
            var customLightupPath = Path.Combine(baseDir, "shared");

            // Delete any existing artifacts
            if (Directory.Exists(customLightupPath))
            {
                Directory.Delete(customLightupPath, true);
            }

            customLightupPath = Path.Combine(customLightupPath, "Microsoft.NETCore.App");
            customLightupPath = Path.Combine(customLightupPath, builtSharedFXVersion);

            // Create the folder to which lightup.deps.json will be copied to.
            Directory.CreateDirectory(customLightupPath);
            
            // Copy the lightup.deps.json
            var libDepsJson = fixtureLib.TestProject.DepsJson;
            File.Copy(libDepsJson, Path.Combine(customLightupPath, Path.GetFileName(libDepsJson)));

            // Copy the library to the location of the lightup app (app-local)
            var destLibPath = Path.Combine(Path.GetDirectoryName(appDll), Path.GetFileName(libDll));
            File.Copy(libDll, destLibPath);

            // Execute the test using the custom lightup path where lightup.deps.json can be found.
            dotnet.Exec("exec", "--additional-deps", baseDir, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello LightupClient");
        }

        // Attempt to run the app without lightup deps.json specified but lightup library present in the expected 
        // probe location (of being app-local).
        [Fact]
        public void Muxer_activation_of_LightupApp_WithLightupLib_NoLightupDepsJson_Fails()
        {
            var fixtureLib = PreviouslyBuiltAndRestoredLightupLibTestProjectFixture
                .Copy();

            var fixtureApp = PreviouslyBuiltAndRestoredLightupAppTestProjectFixture
                .Copy();

            var dotnet = fixtureApp.BuiltDotnet;
            var appDll = fixtureApp.TestProject.AppDll;
            var libDll = fixtureLib.TestProject.AppDll;

            var destLibPath = Path.Combine(Path.GetDirectoryName(appDll), Path.GetFileName(libDll));

            // Copy the library to the location of the lightup app
            File.Copy(libDll, destLibPath);

            dotnet.Exec("exec", appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail:true)
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("Exception: Failed to load the lightup assembly!");
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
