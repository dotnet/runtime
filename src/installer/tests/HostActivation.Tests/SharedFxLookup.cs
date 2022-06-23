// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class SharedFxLookup : IDisposable
    {
        private const string SystemCollectionsImmutableFileVersion = "88.2.3.4";
        private const string SystemCollectionsImmutableAssemblyVersion = "88.0.1.2";

        private readonly RepoDirectoriesProvider RepoDirectories;
        private readonly TestProjectFixture SharedFxLookupPortableAppFixture;

        private readonly string _currentWorkingDir;
        private readonly string _executableDir;
        private readonly string _exeSharedFxBaseDir;
        private readonly string _exeSharedUberFxBaseDir;
        private readonly string _builtSharedFxDir;
        private readonly string _builtSharedUberFxDir;

        private readonly string _sharedFxVersion;
        private readonly TestArtifact _baseDirArtifact;
        private readonly string _builtDotnet;

        public SharedFxLookup()
        {
            // From the artifacts dir, it's possible to find where the sharedFrameworkPublish folder is. We need
            // to locate it because we'll copy its contents into other folders
            string artifactsDir = new RepoDirectoriesProvider().GetTestContextVariable("TEST_ARTIFACTS");
            _builtDotnet = Path.Combine(artifactsDir, "sharedFrameworkPublish");

            // The dotnetSharedFxLookup dir will contain some folders and files that will be
            // necessary to perform the tests
            string baseDir = Path.Combine(artifactsDir, "dotnetSharedFxLookup");
            _baseDirArtifact = new TestArtifact(SharedFramework.CalculateUniqueTestDirectory(baseDir));

            // The two tested locations will be the cwd and the exe dir. Both cwd and exe dir
            // are easily overwritten, so they will be placed inside the multilevel folder.
            _currentWorkingDir = Path.Combine(_baseDirArtifact.Location, "cwd");
            _executableDir = Path.Combine(_baseDirArtifact.Location, "exe");

            RepoDirectories = new RepoDirectoriesProvider(builtDotnet: _executableDir);

            // SharedFxBaseDirs contain all available version folders
            _exeSharedFxBaseDir = Path.Combine(_executableDir, "shared", "Microsoft.NETCore.App");

            _exeSharedUberFxBaseDir = Path.Combine(_executableDir, "shared", "Microsoft.UberFramework");

            // Create directories. It's necessary to copy the entire publish folder to the exe dir because
            // we'll need to build from it. The CopyDirectory method automatically creates the dest dir
            Directory.CreateDirectory(_currentWorkingDir);
            SharedFramework.CopyDirectory(_builtDotnet, _executableDir);

            // Restore and build SharedFxLookupPortableApp from exe dir
            SharedFxLookupPortableAppFixture = new TestProjectFixture("SharedFxLookupPortableApp", RepoDirectories)
                .EnsureRestored()
                .BuildProject();
            var fixture = SharedFxLookupPortableAppFixture;

            // The actual framework version can be obtained from the built fixture. We'll use it to
            // locate the builtSharedFxDir from which we can get the files contained in the version folder
            string greatestVersionSharedFxPath = fixture.BuiltDotnet.GreatestVersionSharedFxPath;
            _sharedFxVersion = (new DirectoryInfo(greatestVersionSharedFxPath)).Name;
            _builtSharedFxDir = Path.Combine(_builtDotnet, "shared", "Microsoft.NETCore.App", _sharedFxVersion);
            _builtSharedUberFxDir = Path.Combine(_builtDotnet, "shared", "Microsoft.UberFramework", _sharedFxVersion);
            SharedFramework.CreateUberFrameworkArtifacts(_builtSharedFxDir, _builtSharedUberFxDir, SystemCollectionsImmutableAssemblyVersion, SystemCollectionsImmutableFileVersion);
        }

        public void Dispose()
        {
            SharedFxLookupPortableAppFixture.Dispose();
            _baseDirArtifact.Dispose();
        }

        [Fact]
        public void Multiple_SharedFxLookup_NetCoreApp_MinorRollForward_Wins_Over_UberFx()
        {
            var fixture = SharedFxLookupPortableAppFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Modify the Uber values
            SharedFramework.CreateUberFrameworkArtifacts(_builtSharedFxDir, _builtSharedUberFxDir, "0.0.0.1", "0.0.0.2");

            // Add versions in the exe folders
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _exeSharedFxBaseDir, "9999.1.0");
            SharedFramework.AddAvailableSharedUberFxVersions(_builtSharedUberFxDir, _exeSharedUberFxBaseDir, "9999.0.0", "7777.0.0");

            string uberFile = Path.Combine(_exeSharedUberFxBaseDir, "7777.0.0", "System.Collections.Immutable.dll");
            string netCoreAppFile = Path.Combine(_exeSharedFxBaseDir, "9999.1.0", "System.Collections.Immutable.dll");
            // The System.Collections.Immutable.dll is located in the UberFramework and NetCoreApp
            // Version: NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            //          'Roll forward on no candidate fx' enabled through config
            // Exe: NetCoreApp 9999.1.0
            //      UberFramework 7777.0.0
            // Expected: 9999.1.0
            //           7777.0.0
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Replacing deps entry [{uberFile}, AssemblyVersion:0.0.0.1, FileVersion:0.0.0.2] with [{netCoreAppFile}");
        }

        [Fact]
        public void Multiple_SharedFxLookup_Uber_Wins_Over_NetCoreApp_On_PatchRollForward()
        {
            var fixture = SharedFxLookupPortableAppFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folders
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _exeSharedFxBaseDir, "9999.0.1");
            SharedFramework.AddAvailableSharedUberFxVersions(_builtSharedUberFxDir, _exeSharedUberFxBaseDir, "9999.0.0", "7777.0.0");

            // The System.Collections.Immutable.dll is located in the UberFramework and NetCoreApp
            // Version: NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            //          'Roll forward on no candidate fx' enabled through config
            // Exe: NetCoreApp 9999.0.1
            //      UberFramework 7777.0.0
            // Expected: 9999.0.1
            //           7777.0.0
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine("7777.0.0", "System.Collections.Immutable.dll"))
                .And.NotHaveStdErrContaining(Path.Combine("9999.1.0", "System.Collections.Immutable.dll"));
        }
    }
}
