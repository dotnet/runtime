// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class LightupAppActivation : IClassFixture<LightupAppActivation.SharedTestState>, IDisposable
    {
        private SharedTestState sharedTestState;

        private const string SystemCollectionsImmutableFileVersion = "88.2.3.4";
        private const string SystemCollectionsImmutableAssemblyVersion = "88.0.1.2";

        private readonly TestArtifact _baseDirArtifact;
        private readonly string _builtSharedFxDir;
        private readonly string _builtSharedUberFxDir;
        private readonly string _fxBaseDir;
        private readonly string _uberFxBaseDir;

        private TestProjectFixture GlobalLightupClientFixture;

        public LightupAppActivation(LightupAppActivation.SharedTestState fixture)
        {
            sharedTestState = fixture;

            // From the artifacts dir, it's possible to find where the sharedFrameworkPublish folder is. We need
            // to locate it because we'll copy its contents into other folders
            string artifactsDir = new RepoDirectoriesProvider().GetTestContextVariable("TEST_ARTIFACTS");
            string builtDotnet = Path.Combine(artifactsDir, "sharedFrameworkPublish");

            // The dotnetLightupSharedFxLookup dir will contain some folders and files that will be necessary to perform the tests
            string sharedLookupDir = Path.Combine(artifactsDir, "dotnetLightupSharedFxLookup");
            _baseDirArtifact = new TestArtifact(SharedFramework.CalculateUniqueTestDirectory(sharedLookupDir));
            _fxBaseDir = Path.Combine(_baseDirArtifact.Location, "shared", "Microsoft.NETCore.App");
            _uberFxBaseDir = Path.Combine(_baseDirArtifact.Location, "shared", "Microsoft.UberFramework");

            SharedFramework.CopyDirectory(builtDotnet, _baseDirArtifact.Location);

            var repoDirectories = new RepoDirectoriesProvider(builtDotnet: _baseDirArtifact.Location);
            GlobalLightupClientFixture = new TestProjectFixture("LightupClient", repoDirectories)
                .EnsureRestored()
                .BuildProject();

            string greatestVersionSharedFxPath = sharedTestState.LightupLibFixture_Built.BuiltDotnet.GreatestVersionSharedFxPath;
            string sharedFxVersion = (new DirectoryInfo(greatestVersionSharedFxPath)).Name;
            _builtSharedFxDir = Path.Combine(builtDotnet, "shared", "Microsoft.NETCore.App", sharedFxVersion);
            _builtSharedUberFxDir = Path.Combine(builtDotnet, "shared", "Microsoft.UberFramework", sharedFxVersion);
            SharedFramework.CreateUberFrameworkArtifacts(_builtSharedFxDir, _builtSharedUberFxDir, SystemCollectionsImmutableAssemblyVersion, SystemCollectionsImmutableFileVersion);
        }

        public void Dispose()
        {
            GlobalLightupClientFixture.Dispose();
            _baseDirArtifact.Dispose();
        }

        // Attempt to run the app with lightup deps.json specified but lightup library missing in the expected
        // probe locations.
        [Fact]
        public void Muxer_activation_of_LightupApp_NoLightupLib_Fails()
        {
            var fixtureLib = sharedTestState.LightupLibFixture_Built
                .Copy();

            var fixtureApp = sharedTestState.LightupClientFixture
                .Copy();

            var dotnet = fixtureApp.BuiltDotnet;
            var appDll = fixtureApp.TestProject.AppDll;
            var libDepsJson = fixtureLib.TestProject.DepsJson;

            dotnet.Exec("exec", "--additional-deps", libDepsJson, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(
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
            var fixtureLib = sharedTestState.LightupLibFixture_Published
                .Copy();

            var fixtureApp = sharedTestState.LightupClientFixture
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
                .Should().Pass()
                .And.HaveStdOutContaining("Hello LightupClient");
        }

        [Fact]
        public void Muxer_activation_of_LightupApp_WithLightupLib_and_Roll_Backwards_From_Release_To_Release_Succeeds()
        {
            var fixtureLib = sharedTestState.LightupLibFixture_Published
                .Copy();

            var fixtureApp = GlobalLightupClientFixture
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
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello LightupClient")
                .And.HaveStdErrContaining($"Using specified additional deps.json: '{selectedLightupPath}");
        }

        [Fact]
        public void Muxer_activation_of_LightupApp_WithLightupLib_and_Roll_Backwards_From_Prerelease_To_Release_Succeeds()
        {
            var fixtureLib = sharedTestState.LightupLibFixture_Published
                .Copy();

            var fixtureApp = GlobalLightupClientFixture
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
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello LightupClient")
                .And.HaveStdErrContaining($"Using specified additional deps.json: '{selectedLightupPath}");
        }

        [Fact]
        public void Muxer_activation_of_LightupApp_WithLightupLib_and_Roll_Backwards_Fails()
        {
            var fixtureLib = sharedTestState.LightupLibFixture_Published
                .Copy();

            var fixtureApp = GlobalLightupClientFixture
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
                .EnableTracingAndCaptureOutputs()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"No additional deps directory less than or equal to [8888.0.1] found with same major and minor version.");
        }

        // Attempt to run the app without lightup deps.json specified but lightup library present in the expected
        // probe location (of being app-local).
        [Fact]
        public void Muxer_activation_of_LightupApp_WithLightupLib_NoLightupDepsJson_Fails()
        {
            var fixtureLib = sharedTestState.LightupLibFixture_Built
                .Copy();

            var fixtureApp = sharedTestState.LightupClientFixture
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
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdOutContaining("Exception: Failed to load the lightup assembly!");
        }

        [Fact]
        public void Additional_Deps_Lightup_Folder_With_Bad_JsonFile()
        {
            var fixture = GlobalLightupClientFixture
                .Copy();

            var fixtureLib = sharedTestState.LightupLibFixture_Published
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
            string additionalDepsPath = Path.Combine(additionalDepsRootPath, "shared", "Microsoft.NETCore.App", "9999.0.0", "myAdditionalDeps.deps.json");
            FileInfo additionalDepsFile = new FileInfo(additionalDepsPath);
            additionalDepsFile.Directory.Create();
            File.WriteAllText(additionalDepsFile.FullName, "THIS IS A BAD JSON FILE");

            // Expected: a parsing error since the json file is bad.
            dotnet.Exec("exec", "--additional-deps", additionalDepsRootPath, appDll)
                .EnableTracingAndCaptureOutputs()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"Error initializing the dependency resolver: An error occurred while parsing: {additionalDepsPath}");
        }

        [Fact]
        public void SharedFx_With_Higher_Version_Wins_Against_Additional_Deps()
        {
            var fixture = GlobalLightupClientFixture
                .Copy();

            var fixtureLib = sharedTestState.LightupLibFixture_Published
                .Copy();

            CopyLightupLib(fixture, fixtureLib);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 7777.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "LightupClient.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folder
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _fxBaseDir, "9999.0.0");
            SharedFramework.AddAvailableSharedUberFxVersions(_builtSharedUberFxDir, _uberFxBaseDir, "9999.0.0", "7777.0.0");

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
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Using specified additional deps.json: '{additionalDepsPath}'")
                .And.HaveStdErrContaining($"Adding tpa entry: {uberAssembly}")
                .And.HaveStdErrContaining($"Adding tpa entry: {appAssembly}")
                .And.HaveStdErrContaining($"Replacing deps entry [{appAssembly}")
                .And.HaveStdErrContaining($"with [{uberAssembly}, AssemblyVersion:{SystemCollectionsImmutableAssemblyVersion}, FileVersion:{SystemCollectionsImmutableFileVersion}]")
                // Verify final selection in TRUSTED_PLATFORM_ASSEMBLIES
                .And.HaveStdErrContaining($"{uberAssembly}{Path.PathSeparator}")
                .And.NotHaveStdErrContaining($"{appAssembly}{Path.PathSeparator}");
        }

        [Fact]
        public void SharedFx_With_Lower_Version_Loses_Against_Additional_Deps()
        {
            var fixture = GlobalLightupClientFixture
                .Copy();

            var fixtureLib = sharedTestState.LightupLibFixture_Published
                .Copy();

            CopyLightupLib(fixture, fixtureLib);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 7777.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "LightupClient.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folder
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _fxBaseDir, "9999.0.0");
            SharedFramework.AddAvailableSharedUberFxVersions(_builtSharedUberFxDir, _uberFxBaseDir, "9999.0.0", "7777.0.0");

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
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Using specified additional deps.json: '{additionalDepsPath}'")
                .And.HaveStdErrContaining($"Adding tpa entry: {appAssembly}, AssemblyVersion: 99.9.9.9, FileVersion: 98.9.9.9")
                // Verify final selection in TRUSTED_PLATFORM_ASSEMBLIES
                .And.HaveStdErrContaining($"{appAssembly}{Path.PathSeparator}")
                .And.NotHaveStdErrContaining($"{uberAssembly}{Path.PathSeparator}");
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
            SharedFramework.AddReferenceToDepsJson(depsFile, "LightupLib/1.0.0", "Newtonsoft.Json", "13.0.1");

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

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture LightupLibFixture_Built { get; }
            public TestProjectFixture LightupLibFixture_Published { get; }

            public TestProjectFixture LightupClientFixture { get; }

            public RepoDirectoriesProvider RepoDirectories { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                LightupLibFixture_Built = new TestProjectFixture("LightupLib", RepoDirectories)
                    .EnsureRestored()
                    .BuildProject();

                LightupLibFixture_Published = new TestProjectFixture("LightupLib", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                LightupClientFixture = new TestProjectFixture("LightupClient", RepoDirectories)
                    .EnsureRestored()
                    .BuildProject();
            }

            public void Dispose()
            {
                LightupLibFixture_Built.Dispose();
                LightupLibFixture_Published.Dispose();
                LightupClientFixture.Dispose();
            }
        }
    }
}
