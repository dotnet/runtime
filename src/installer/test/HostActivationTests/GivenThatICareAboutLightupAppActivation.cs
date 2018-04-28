// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
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
        private const string SystemCollectionsImmutableFileVersion = "1.2.3.4";
        private const string SystemCollectionsImmutableAssemblyVersion = "1.0.1.2";

        private static TestProjectFixture PreviouslyBuiltAndRestoredLightupLibTestProjectFixture { get; set; }
        private static TestProjectFixture PreviouslyPublishedAndRestoredLightupLibTestProjectFixture { get; set; }

        private static TestProjectFixture PreviouslyBuiltAndRestoredLightupAppTestProjectFixture { get; set; }
        private static TestProjectFixture PreviouslyPublishedAndRestoredLightupAppTestProjectFixture { get; set; }

        private static TestProjectFixture PreviouslyGlobalBuiltAndRestoredLightupAppTestProjectFixture { get; set; }


        private static RepoDirectoriesProvider RepoDirectories { get; set; }

        private string _currentWorkingDir;
        private string _builtDotnet;
        private string _builtSharedFxDir;
        private string _builtSharedUberFxDir;
        private string _fxBaseDir;
        private string _uberFxBaseDir;

        static GivenThatICareAboutLightupAppActivation()
        {
            RepoDirectories = new RepoDirectoriesProvider();

            PreviouslyBuiltAndRestoredLightupLibTestProjectFixture = new TestProjectFixture("LightupLib", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .BuildProject();

            PreviouslyPublishedAndRestoredLightupLibTestProjectFixture = new TestProjectFixture("LightupLib", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .PublishProject();

            PreviouslyBuiltAndRestoredLightupAppTestProjectFixture = new TestProjectFixture("LightupClient", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .BuildProject();

            PreviouslyPublishedAndRestoredLightupAppTestProjectFixture = new TestProjectFixture("LightupClient", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .PublishProject();
        }

        public GivenThatICareAboutLightupAppActivation()
        {
            // From the artifacts dir, it's possible to find where the sharedFrameworkPublish folder is. We need
            // to locate it because we'll copy its contents into other folders
            string artifactsDir = Environment.GetEnvironmentVariable("TEST_ARTIFACTS");
            _builtDotnet = Path.Combine(artifactsDir, "sharedFrameworkPublish");

            // The dotnetLightupSharedFxLookup dir will contain some folders and files that will be necessary to perform the tests
            string sharedLookupDir = Path.Combine(artifactsDir, "dotnetLightupSharedFxLookup");
            _currentWorkingDir = SharedFramework.CalculateUniqueTestDirectory(sharedLookupDir);
            _fxBaseDir = Path.Combine(_currentWorkingDir, "shared", "Microsoft.NETCore.App");
            _uberFxBaseDir = Path.Combine(_currentWorkingDir, "shared", "Microsoft.UberFramework");

            SharedFramework.CopyDirectory(_builtDotnet, _currentWorkingDir);

            var repoDirectories = new RepoDirectoriesProvider(builtDotnet: _currentWorkingDir);
            PreviouslyGlobalBuiltAndRestoredLightupAppTestProjectFixture = new TestProjectFixture("LightupClient", repoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .BuildProject();

            string greatestVersionSharedFxPath = PreviouslyBuiltAndRestoredLightupLibTestProjectFixture.BuiltDotnet.GreatestVersionSharedFxPath;
            string sharedFxVersion = (new DirectoryInfo(greatestVersionSharedFxPath)).Name;
            _builtSharedFxDir = Path.Combine(_builtDotnet, "shared", "Microsoft.NETCore.App", sharedFxVersion);
            _builtSharedUberFxDir = Path.Combine(_builtDotnet, "shared", "Microsoft.UberFramework", sharedFxVersion);
            SharedFramework.CreateUberFrameworkArtifacts(_builtSharedFxDir, _builtSharedUberFxDir, SystemCollectionsImmutableAssemblyVersion, SystemCollectionsImmutableFileVersion);
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
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(
                    "Error:" + Environment.NewLine +
                    "  An assembly specified in the application dependencies manifest (LightupLib.deps.json) was not found:" + Environment.NewLine +
                    "    package: \'LightupLib\', version: \'1.0.0\'" + Environment.NewLine +
                    "    path: \'LightupLib.dll\'");
        }

        // Attempt to run the app with lightup deps.json specified and lightup library present in the expected
        // probe locations.
        [Fact]
        public void Muxer_activation_of_LightupApp_WithLightupLib_Succeeds()
        {
            var fixtureLib = PreviouslyPublishedAndRestoredLightupLibTestProjectFixture
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

        [Fact]
        public void Muxer_activation_of_LightupApp_WithLightupLib_and_Roll_Backwards_From_Release_To_Release_Succeeds()
        {
            var fixtureLib = PreviouslyPublishedAndRestoredLightupLibTestProjectFixture
                .Copy();

            var fixtureApp = PreviouslyGlobalBuiltAndRestoredLightupAppTestProjectFixture
                .Copy();

            var dotnet = fixtureApp.BuiltDotnet;
            var appDll = fixtureApp.TestProject.AppDll;
            var libDepsJson = fixtureLib.TestProject.DepsJson;

            // Set desired version = 8888.0.0
            string runtimeConfig = Path.Combine(fixtureApp.TestProject.OutputDirectory, "LightupClient.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "8888.0.0");

            // Add versions in the exe folder
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _fxBaseDir, "8888.0.5");

            CopyLightupLib(fixtureApp, fixtureLib);

            // Create the M.N.App specific folder where lightup.deps.json can be found.
            var baseDir = fixtureApp.TestProject.ProjectDirectory;
            var customLightupPath = Path.Combine(baseDir, "shared");

            // Delete any existing artifacts
            if (Directory.Exists(customLightupPath))
            {
                Directory.Delete(customLightupPath, true);
            }

            customLightupPath = Path.Combine(customLightupPath, "Microsoft.NETCore.App");

            CreateLightupFolder(customLightupPath, $"8887.0.0", libDepsJson);
            CreateLightupFolder(customLightupPath, $"8888.0.0", libDepsJson);
            CreateLightupFolder(customLightupPath, $"8888.0.4-preview", libDepsJson);

            // Closest backwards patch version (selected)
            CreateLightupFolder(customLightupPath, $"8888.0.4", libDepsJson);
            string selectedLightupPath = Path.Combine(customLightupPath, "8888.0.4");

            CreateLightupFolder(customLightupPath, $"8888.0.9", libDepsJson);
            CreateLightupFolder(customLightupPath, $"8889.0.0", libDepsJson);

            // Version targeted: NetCoreApp 8888.0.0
            // Version existing: NetCoreApp 8888.0.5
            // Lightup folders: 8887.0.0
            //                  8888.0.0
            //                  8888.0.4-preview
            //                  8888.0.4
            //                  8888.0.9
            //                  8889.0.0
            // Expected: 8888.0.4
            dotnet.Exec("exec", "--additional-deps", baseDir, appDll)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello LightupClient")
                .And
                .HaveStdErrContaining($"Using specified additional deps.json: '{selectedLightupPath}");

            SharedFramework.DeleteAvailableSharedFxVersions(_fxBaseDir, "8888.0.5");
        }

        [Fact]
        public void Muxer_activation_of_LightupApp_WithLightupLib_and_Roll_Backwards_From_Prerelease_To_Release_Succeeds()
        {
            var fixtureLib = PreviouslyPublishedAndRestoredLightupLibTestProjectFixture
                .Copy();

            var fixtureApp = PreviouslyGlobalBuiltAndRestoredLightupAppTestProjectFixture
                .Copy();

            var dotnet = fixtureApp.BuiltDotnet;
            var appDll = fixtureApp.TestProject.AppDll;
            var libDepsJson = fixtureLib.TestProject.DepsJson;

            // Set desired version = 8888.0.0
            string runtimeConfig = Path.Combine(fixtureApp.TestProject.OutputDirectory, "LightupClient.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "8888.0.5-preview1");

            // Add versions in the exe folder
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _fxBaseDir, "8888.0.5-preview2");

            CopyLightupLib(fixtureApp, fixtureLib);

            // Create the M.N.App specific folder where lightup.deps.json can be found.
            var baseDir = fixtureApp.TestProject.ProjectDirectory;
            var customLightupPath = Path.Combine(baseDir, "shared");

            // Delete any existing artifacts
            if (Directory.Exists(customLightupPath))
            {
                Directory.Delete(customLightupPath, true);
            }

            customLightupPath = Path.Combine(customLightupPath, "Microsoft.NETCore.App");

            CreateLightupFolder(customLightupPath, $"8888.0.0", libDepsJson);
            CreateLightupFolder(customLightupPath, $"8888.0.4-preview", libDepsJson);

            // Closest backwards patch version (selected)
            CreateLightupFolder(customLightupPath, $"8888.0.4", libDepsJson);
            string selectedLightupPath = Path.Combine(customLightupPath, "8888.0.4");

            CreateLightupFolder(customLightupPath, $"8888.0.5", libDepsJson);

            // Version targeted: NetCoreApp 8888.0.0-preview1
            // Version existing: NetCoreApp 8888.0.5-preview2
            // Lightup folders: 8888.0.0
            //                  8888.0.4-preview
            //                  8888.0.4
            //                  8888.0.5
            // Expected: 8888.0.4
            dotnet.Exec("exec", "--additional-deps", baseDir, appDll)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello LightupClient")
                .And
                .HaveStdErrContaining($"Using specified additional deps.json: '{selectedLightupPath}");

            SharedFramework.DeleteAvailableSharedFxVersions(_fxBaseDir, "8888.0.5-preview2");
        }

        [Fact]
        public void Muxer_activation_of_LightupApp_WithLightupLib_and_Roll_Backwards_Fails()
        {
            var fixtureLib = PreviouslyPublishedAndRestoredLightupLibTestProjectFixture
                .Copy();

            var fixtureApp = PreviouslyGlobalBuiltAndRestoredLightupAppTestProjectFixture
                .Copy();

            var dotnet = fixtureApp.BuiltDotnet;
            var appDll = fixtureApp.TestProject.AppDll;
            var libDepsJson = fixtureLib.TestProject.DepsJson;

            // Set desired version = 8888.0.0
            string runtimeConfig = Path.Combine(fixtureApp.TestProject.OutputDirectory, "LightupClient.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "8888.0.0");

            // Add versions in the exe folder
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _fxBaseDir, "8888.0.1");

            CopyLightupLib(fixtureApp, fixtureLib);

            // Create the M.N.App specific folder where lightup.deps.json can be found.
            var baseDir = fixtureApp.TestProject.ProjectDirectory;
            var customLightupPath = Path.Combine(baseDir, "shared");

            // Delete any existing artifacts
            if (Directory.Exists(customLightupPath))
            {
                Directory.Delete(customLightupPath, true);
            }

            customLightupPath = Path.Combine(customLightupPath, "Microsoft.NETCore.App");

            CreateLightupFolder(customLightupPath, $"8887.0.0", libDepsJson);
            CreateLightupFolder(customLightupPath, $"8889.0.0", libDepsJson);

            // Version targeted: NetCoreApp 8888.0.0
            // Version existing: NetCoreApp 8888.0.1
            // Lightup folders: 8887.0.0
            //                  8889.0.0
            // Expected: fail since we only roll backward on patch, not minor
            dotnet.Exec("exec", "--additional-deps", baseDir, appDll)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining($"No additional deps directory less than or equal to [8888.0.1] found with same major and minor version.");

            SharedFramework.DeleteAvailableSharedFxVersions(_fxBaseDir, "8888.0.1");
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
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("Exception: Failed to load the lightup assembly!");
        }

        [Fact]
        public void Additional_Deps_Lightup_Folder_With_Bad_JsonFile()
        {
            var fixture = PreviouslyGlobalBuiltAndRestoredLightupAppTestProjectFixture
                .Copy();

            var fixtureLib = PreviouslyPublishedAndRestoredLightupLibTestProjectFixture
                .Copy();

            CopyLightupLib(fixture, fixtureLib);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Add version in the exe folder
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _fxBaseDir, "9999.0.0");

            // Set desired version = 9999.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "LightupClient.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "9999.0.0");

            string additionalDepsRootPath = Path.Combine(_fxBaseDir, "additionalDeps");

            // Create a deps.json file in the folder "additionalDeps\shared\Microsoft.NETCore.App\9999.0.0"
            string additionalDepsPath = Path.Combine(additionalDepsRootPath, "shared", "Microsoft.NETCore.App", "9999.0.0", "myAddtionalDeps.deps.json");
            FileInfo additionalDepsFile = new FileInfo(additionalDepsPath);
            additionalDepsFile.Directory.Create();
            File.WriteAllText(additionalDepsFile.FullName, "THIS IS A BAD JSON FILE");

            // Expected: a parsing error since the json file is bad.
            dotnet.Exec("exec", "--additional-deps", additionalDepsRootPath, appDll)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining($"Error initializing the dependency resolver: An error occurred while parsing: {additionalDepsPath}");

            SharedFramework.DeleteAvailableSharedFxVersions(_fxBaseDir, "9999.0.0", "additionalDeps");
        }

        [Fact]
        public void SharedFx_With_Higher_Version_Wins_Against_Additional_Deps()
        {
            var fixture = PreviouslyGlobalBuiltAndRestoredLightupAppTestProjectFixture
                .Copy();

            var fixtureLib = PreviouslyPublishedAndRestoredLightupLibTestProjectFixture
                .Copy();

            CopyLightupLib(fixture, fixtureLib);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 7777.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "LightupClient.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folder
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _fxBaseDir, "9999.0.0");
            SharedFramework.AddAvailableSharedUberFxVersions(_builtSharedUberFxDir, _uberFxBaseDir, "9999.0.0", null, "7777.0.0");

            // Copy NetCoreApp's copy of the assembly to the app location
            string netcoreAssembly = Path.Combine(_fxBaseDir, "9999.0.0", "System.Collections.Immutable.dll");
            string appAssembly = Path.Combine(fixture.TestProject.OutputDirectory, "System.Collections.Immutable.dll");
            File.Copy(netcoreAssembly, appAssembly);

            // Create a deps.json file in the folder "additionalDeps\shared\Microsoft.NETCore.App\9999.0.0"
            string additionalDepsRootPath = Path.Combine(_fxBaseDir, "additionalDeps");
            JObject versionInfo = new JObject();
            versionInfo.Add(new JProperty("assemblyVersion", "0.0.0.1"));
            versionInfo.Add(new JProperty("fileVersion", "0.0.0.2"));
            string additionalDepsPath = CreateAdditionalDeps(additionalDepsRootPath, versionInfo);

            // Version: NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            // Existing:NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            // Expected: 9999.0.0
            //           7777.0.0
            // Expected: the uber framework's version of System.Collections.Immutable is used instead of the additional-deps
            string uberAssembly = Path.Combine(_uberFxBaseDir, "7777.0.0", "System.Collections.Immutable.dll");
            dotnet.Exec("exec", "--additional-deps", additionalDepsPath, appDll)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining($"Using specified additional deps.json: '{additionalDepsPath}'")
                .And
                .HaveStdErrContaining($"Adding tpa entry: {uberAssembly}")
                .And
                .NotHaveStdErrContaining($"Adding tpa entry: {appAssembly}")
                .And
                .NotHaveStdErrContaining($"Replacing deps entry");

            SharedFramework.DeleteAvailableSharedFxVersions(_fxBaseDir, "9999.0.0", "additionalDeps");
            SharedFramework.DeleteAvailableSharedFxVersions(_uberFxBaseDir, "7777.0.0");
        }

        [Fact]
        public void SharedFx_With_Lower_Version_Loses_Against_Additional_Deps()
        {
            var fixture = PreviouslyGlobalBuiltAndRestoredLightupAppTestProjectFixture
                .Copy();

            var fixtureLib = PreviouslyPublishedAndRestoredLightupLibTestProjectFixture
                .Copy();

            CopyLightupLib(fixture, fixtureLib);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 7777.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "LightupClient.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folder
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _fxBaseDir, "9999.0.0");
            SharedFramework.AddAvailableSharedUberFxVersions(_builtSharedUberFxDir, _uberFxBaseDir, "9999.0.0", null, "7777.0.0");

            // Copy NetCoreApp's copy of the assembly to the app location
            string netcoreAssembly = Path.Combine(_fxBaseDir, "9999.0.0", "System.Collections.Immutable.dll");
            string appAssembly = Path.Combine(fixture.TestProject.OutputDirectory, "System.Collections.Immutable.dll");
            File.Copy(netcoreAssembly, appAssembly);

            // Create a deps.json file in the folder "additionalDeps\shared\Microsoft.NETCore.App\9999.0.0"
            string additionalDepsRootPath = Path.Combine(_fxBaseDir, "additionalDeps");
            JObject versionInfo = new JObject();
            // Use Higher version numbers to win
            versionInfo.Add(new JProperty("assemblyVersion", "99.9.9.9"));
            versionInfo.Add(new JProperty("fileVersion", "98.9.9.9"));
            string additionalDepsPath = CreateAdditionalDeps(additionalDepsRootPath, versionInfo);

            // Version: NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            // Existing:NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            // Expected: 9999.0.0
            //           7777.0.0
            // Expected: the additional dep's version of System.Collections.Immutable is used instead of the uber's assembly
            string uberAssembly = Path.Combine(_uberFxBaseDir, "7777.0.0", "System.Collections.Immutable.dll");
            dotnet.Exec("exec", "--additional-deps", additionalDepsPath, appDll)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining($"Using specified additional deps.json: '{additionalDepsPath}'")
                .And
                .HaveStdErrContaining($"Adding tpa entry: {uberAssembly}")
                .And
                .HaveStdErrContaining($"Adding tpa entry: {appAssembly}")
                .And
                .HaveStdErrContaining($"Replacing deps entry [{uberAssembly}, AssemblyVersion:{SystemCollectionsImmutableAssemblyVersion}, FileVersion:{SystemCollectionsImmutableFileVersion}] with [{appAssembly}, AssemblyVersion:99.9.9.9, FileVersion:98.9.9.9]");

            SharedFramework.DeleteAvailableSharedFxVersions(_fxBaseDir, "9999.0.0", "additionalDeps");
            SharedFramework.DeleteAvailableSharedFxVersions(_uberFxBaseDir, "7777.0.0");
        }

        private static void CreateLightupFolder(string customLightupPath, string version, string libDepsJson)
        {
            customLightupPath = Path.Combine(customLightupPath, version);

            // Create the folder to which lightup.deps.json will be copied to.
            Directory.CreateDirectory(customLightupPath);

            // Copy the lightup.deps.json
            File.Copy(libDepsJson, Path.Combine(customLightupPath, Path.GetFileName(libDepsJson)));
        }

        private static string CreateAdditionalDeps(string destDir, JObject immutableCollectionVersionInfo)
        {
            DirectoryInfo dir = new DirectoryInfo(destDir);
            if (dir.Exists)
            {
                dir.Delete(true);
            }

            dir.Create();

            JObject depsjson = SharedFramework.CreateDepsJson("Microsoft.NETCore.App", "LightupLib/1.0.0", "LightupLib");

            string depsFile = Path.Combine(destDir, "My.deps.json");
            File.WriteAllText(depsFile, depsjson.ToString());

            SharedFramework.AddReferenceToDepsJson(depsFile, "LightupLib/1.0.0", "System.Collections.Immutable", "1.0.0", immutableCollectionVersionInfo);
            SharedFramework.AddReferenceToDepsJson(depsFile, "LightupLib/1.0.0", "Newtonsoft.Json", "9.0.1");

            return depsFile;
        }

        private static void CopyLightupLib(TestProjectFixture fixtureApp, TestProjectFixture fixtureLib)
        {
            var appDll = fixtureApp.TestProject.AppDll;
            var libDll = fixtureLib.TestProject.AppDll;

            // Copy the library to the location of the lightup app (app-local)
            var destLibPath = Path.Combine(Path.GetDirectoryName(appDll), Path.GetFileName(libDll));
            File.Copy(libDll, destLibPath);

            // Copy the newtonsoft dependency to the location of the lightup app (app-local)
            var srcNewtonsoftPath = Path.Combine(Path.GetDirectoryName(libDll), "Newtonsoft.Json.dll");
            var destNewtonsoftPath = Path.Combine(Path.GetDirectoryName(appDll), "Newtonsoft.Json.dll");
            File.Copy(srcNewtonsoftPath, destNewtonsoftPath);
        }
    }
}
