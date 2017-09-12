﻿using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.Cli.Build.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.MultilevelSharedFxLookup
{
    public class GivenThatICareAboutMultilevelSharedFxLookup
    {
        private RepoDirectoriesProvider RepoDirectories;
        private TestProjectFixture PreviouslyBuiltAndRestoredPortableTestProjectFixture;

        private string _currentWorkingDir;
        private string _userDir;
        private string _executableDir;
        private string _globalDir;
        private string _cwdSharedFxBaseDir;
        private string _userSharedFxBaseDir;
        private string _exeSharedFxBaseDir;
        private string _globalSharedFxBaseDir;
        private string _builtSharedFxDir;
        private string _cwdSelectedMessage;
        private string _userSelectedMessage;
        private string _exeSelectedMessage;
        private string _globalSelectedMessage;
        private string _sharedFxVersion;
        private string _multilevelDir;
        private string _builtDotnet;
        private string _hostPolicyDllName;
        
        public GivenThatICareAboutMultilevelSharedFxLookup()
        {
            // From the artifacts dir, it's possible to find where the sharedFrameworkPublish folder is. We need
            // to locate it because we'll copy its contents into other folders
            string artifactsDir = Environment.GetEnvironmentVariable("TEST_ARTIFACTS");
            _builtDotnet = Path.Combine(artifactsDir, "sharedFrameworkPublish");

            // The dotnetMultilevelSharedFxLookup dir will contain some folders and files that will be
            // necessary to perform the tests
            string baseMultilevelDir = Path.Combine(artifactsDir, "dotnetMultilevelSharedFxLookup");
            _multilevelDir = CalculateMultilevelDirectory(baseMultilevelDir);

            // The three tested locations will be the cwd, the user folder and the exe dir. Both cwd and exe dir
            // are easily overwritten, so they will be placed inside the multilevel folder. The actual user location will
            // be used during tests
            _currentWorkingDir = Path.Combine(_multilevelDir, "cwd");
            _userDir = Path.Combine(_multilevelDir, "user");
            _executableDir = Path.Combine(_multilevelDir, "exe");
            _globalDir = Path.Combine(_multilevelDir, "global");

            RepoDirectories = new RepoDirectoriesProvider(builtDotnet: _executableDir);

            // SharedFxBaseDirs contain all available version folders
            _cwdSharedFxBaseDir = Path.Combine(_currentWorkingDir, "shared", "Microsoft.NETCore.App");
            _userSharedFxBaseDir = Path.Combine(_userDir, ".dotnet", RepoDirectories.BuildArchitecture, "shared", "Microsoft.NETCore.App");
            _exeSharedFxBaseDir = Path.Combine(_executableDir, "shared", "Microsoft.NETCore.App");
            _globalSharedFxBaseDir = Path.Combine(_globalDir, "shared", "Microsoft.NETCore.App");

            // Create directories. It's necessary to copy the entire publish folder to the exe dir because
            // we'll need to build from it. The CopyDirectory method automatically creates the dest dir
            Directory.CreateDirectory(_cwdSharedFxBaseDir);
            Directory.CreateDirectory(_userSharedFxBaseDir);
            Directory.CreateDirectory(_globalSharedFxBaseDir);
            CopyDirectory(_builtDotnet, _executableDir);

            //Copy dotnet to global directory
            File.Copy(Path.Combine(_builtDotnet, $"dotnet{Constants.ExeSuffix}"), Path.Combine(_globalDir, $"dotnet{Constants.ExeSuffix}"), true);

            // Restore and build SharedFxLookupPortableApp from exe dir
            PreviouslyBuiltAndRestoredPortableTestProjectFixture = new TestProjectFixture("SharedFxLookupPortableApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .BuildProject();
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture;

            // The actual framework version can be obtained from the built fixture. We'll use it to
            // locate the builtSharedFxDir from which we can get the files contained in the version folder
            string greatestVersionSharedFxPath = fixture.BuiltDotnet.GreatestVersionSharedFxPath;
            _sharedFxVersion = (new DirectoryInfo(greatestVersionSharedFxPath)).Name;
            _builtSharedFxDir = Path.Combine(_builtDotnet, "shared", "Microsoft.NETCore.App", _sharedFxVersion);

            _hostPolicyDllName = Path.GetFileName(fixture.TestProject.HostPolicyDll);

            // Trace messages used to identify from which folder the framework was picked
            _cwdSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_cwdSharedFxBaseDir}";
            _userSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_userSharedFxBaseDir}";
            _exeSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_exeSharedFxBaseDir}";
            _globalSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_globalSharedFxBaseDir}";
        }

        [Fact]
        public void SharedFxLookup_Must_Verify_Folders_in_the_Correct_Order()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 9999.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "9999.0.0");

            // Add a dummy version in the exe dir
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0");

            // Version: 9999.0.0
            // User: empty
            // Exe: 9999.0.0
            // Expected: 9999.0.0 from exe dir
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .WithUserProfile(_userDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(_exeSelectedMessage);

            // Add a dummy version in the user dir
            AddAvailableSharedFxVersions(_userSharedFxBaseDir, "9999.0.0");

            // Version: 9999.0.0
            // User: 9999.0.0 --> should not be picked
            // Exe: 9999.0.0
            // Expected: 9999.0.0 from user dir
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .WithUserProfile(_userDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(_exeSelectedMessage);

            // Add a dummy version in the cwd
            AddAvailableSharedFxVersions(_cwdSharedFxBaseDir, "9999.0.0");

            // Version: 9999.0.0
            // CWD: 9999.0.0   --> should not be picked
            // User: 9999.0.0
            // Exe: 9999.0.0
            // Expected: 9999.0.0 from user Exe
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .WithUserProfile(_userDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(_exeSelectedMessage);

            // Verify we have the expected runtime versions
            dotnet.Exec("--list-runtimes")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .WithUserProfile(_userDir)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("9999.0.0");
        }

        [Fact]
        public void SharedFxLookup_Must_Not_Roll_Forward_If_Framework_Version_Is_Specified_Through_Argument()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Add some dummy versions
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.2", "9999.0.0-dummy2", "9999.0.0", "9999.0.3", "9999.0.0-dummy3");
            

            // Version: 9999.0.0 (through --fx-version arg)
            // Exe: 9999.0.2, 9999.0.0-dummy2, 9999.0.0, 9999.0.3, 9999.0.0-dummy3
            // global: empty
            // Expected: 9999.0.0 from exe dir
            dotnet.Exec("--fx-version", "9999.0.0", appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.0"));

            // Version: 9999.0.0-dummy1 (through --fx-version arg)
            // Exe: 9999.0.2, 9999.0.0-dummy2,9999.0.0, 9999.0.3, 9999.0.0-dummy3
            // global: empty
            // Expected: no compatible version
            dotnet.Exec("--fx-version", "9999.0.0-dummy1", appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail:true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("It was not possible to find any compatible framework version");

            // Verify we have the expected runtime versions
            dotnet.Exec("--list-runtimes")
                .WorkingDirectory(_currentWorkingDir)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("9999.0.0")
                .And
                .HaveStdOutContaining("9999.0.0-dummy2")
                .And
                .HaveStdOutContaining("9999.0.2")
                .And
                .HaveStdOutContaining("9999.0.3");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0", "9999.0.3", "9999.0.0-dummy3", "9999.0.2", "9999.0.0-dummy2");
        }

        [Fact]
        public void Roll_Forward_On_No_Candidate_Fx_Must_Happen_If_Compatible_Patch_Version_Is_Not_Available()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 9999.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "9999.0.0");

            // Add some dummy versions in the exe
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "10000.1.1", "10000.1.3");

            // Version: 9999.0.0
            // 'Roll forward on no candidate fx' enabled through env var
            // exe: 10000.1.1, 10000.1.3
            // Expected: 10000.1.3 from exe
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "1")
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "10000.1.3"));

            // Add a dummy version in the exe dir 
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.1");

            // Version: 9999.0.0
            // 'Roll forward on no candidate fx' enabled through env var
            // exe: 9999.1.1, 10000.1.1, 10000.1.3
            // Expected: 9999.1.1 from exe
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "1")
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.1.1"));

            // Verify we have the expected runtime versions
            dotnet.Exec("--list-runtimes")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "1")
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("9999.1.1")
                .And
                .HaveStdOutContaining("10000.1.1")
                .And
                .HaveStdOutContaining("10000.1.3");
        }

        [Fact]
        public void Roll_Forward_On_No_Candidate_Fx_Fails_If_No_Higher_Version_Is_Available()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 9999.1.1
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "9999.1.1");

            // Add some dummy versions in the exe
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9998.0.1", "9998.1.0", "9999.0.0", "9999.0.1", "9999.1.0");

            // Version: 9999.1.1
            // 'Roll forward on no candidate fx' enabled through env var
            // exe: 9998.0.1, 9998.1.0, 9999.0.0, 9999.0.1, 9999.1.0
            // Expected: no compatible version
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "1")
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail:true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("It was not possible to find any compatible framework version");

            // Verify we have the expected runtime versions
            dotnet.Exec("--list-runtimes")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "1")
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("9998.0.1")
                .And
                .HaveStdOutContaining("9998.1.0")
                .And
                .HaveStdOutContaining("9999.0.0")
                .And
                .HaveStdOutContaining("9999.0.1")
                .And
                .HaveStdOutContaining("9999.1.0");
        }

        // This method adds a list of new framework version folders in the specified
        // sharedFxBaseDir. The files are copied from the _buildSharedFxDir.
        // Remarks:
        // - If the sharedFxBaseDir does not exist, then a DirectoryNotFoundException
        //   is thrown.
        // - If a specified version folder already exists, then it is deleted and replaced
        //   with the contents of the _builtSharedFxDir.
        private void AddAvailableSharedFxVersions(string sharedFxBaseDir, params string[] availableVersions)
        {
            DirectoryInfo sharedFxBaseDirInfo = new DirectoryInfo(sharedFxBaseDir);

            if (!sharedFxBaseDirInfo.Exists)
            {
                throw new DirectoryNotFoundException();
            }

            foreach(string version in availableVersions)
            {
                string newSharedFxDir = Path.Combine(sharedFxBaseDir, version);
                CopyDirectory(_builtSharedFxDir, newSharedFxDir);
            }
        }

        // This method removes a list of framework version folders from the specified
        // sharedFxBaseDir.
        // Remarks:
        // - If the sharedFxBaseDir does not exist, then a DirectoryNotFoundException
        //   is thrown.
        // - If a specified version folder does not exist, then a DirectoryNotFoundException
        //   is thrown.
        private void DeleteAvailableSharedFxVersions(string sharedFxBaseDir, params string[] availableVersions)
        {
            DirectoryInfo sharedFxBaseDirInfo = new DirectoryInfo(sharedFxBaseDir);

            if (!sharedFxBaseDirInfo.Exists)
            {
                throw new DirectoryNotFoundException();
            }

            foreach (string version in availableVersions)
            {
                string sharedFxDir = Path.Combine(sharedFxBaseDir, version);
                if (!Directory.Exists(sharedFxDir))
                {
                    throw new DirectoryNotFoundException();
                }
                Directory.Delete(sharedFxDir, true);
            }
        }

        // CopyDirectory recursively copies a directory
        // Remarks:
        // - If the dest dir does not exist, then it is created.
        // - If the dest dir exists, then it is substituted with the new one
        //   (original files and subfolders are deleted).
        // - If the src dir does not exist, then a DirectoryNotFoundException
        //   is thrown.
        private void CopyDirectory(string srcDir, string dstDir)
        {
            DirectoryInfo srcDirInfo = new DirectoryInfo(srcDir);

            if (!srcDirInfo.Exists)
            {
                throw new DirectoryNotFoundException();
            }

            DirectoryInfo dstDirInfo = new DirectoryInfo(dstDir);

            if (dstDirInfo.Exists)
            {
                dstDirInfo.Delete(true);
            }

            dstDirInfo.Create();

            foreach (FileInfo fileInfo in srcDirInfo.GetFiles())
            {
                string newFile = Path.Combine(dstDir, fileInfo.Name);
                fileInfo.CopyTo(newFile);
            }

            foreach (DirectoryInfo subdirInfo in srcDirInfo.GetDirectories())
            {
                string newDir = Path.Combine(dstDir, subdirInfo.Name);
                CopyDirectory(subdirInfo.FullName, newDir);
            }
        }

        // MultilevelDirectory is %TEST_ARTIFACTS%\dotnetMultilevelSharedFxLookup\id.
        // We must locate the first non existing id.
        private string CalculateMultilevelDirectory(string baseMultilevelDir)
        {
            int count = 0;
            string multilevelDir;

            do
            {
                multilevelDir = Path.Combine(baseMultilevelDir, count.ToString());
                count++;
            } while (Directory.Exists(multilevelDir));

            return multilevelDir;
        }

        // Generated json file:
        /*
         * {
         *   "runtimeOptions": {
         *     "framework": {
         *       "name": "Microsoft.NETCore.App",
         *       "version": {version}
         *     },
         *     "rollForwardOnNoCandidateFx": {rollFwdOnNoCandidateFx} <-- only if rollFwdOnNoCandidateFx is defined
         *   }
         * }
        */
        private void SetRuntimeConfigJson(string destFile, string version, int? rollFwdOnNoCandidateFx = null)
        {
            JObject runtimeOptions = new JObject(
                new JProperty("framework",
                    new JObject(
                        new JProperty("name", "Microsoft.NETCore.App"),
                        new JProperty("version", version)
                    )
                )
            );

            if (rollFwdOnNoCandidateFx.HasValue)
            {
                runtimeOptions.Add("rollForwardOnNoCandidateFx", rollFwdOnNoCandidateFx);
            }

            JObject json = new JObject();
            json.Add("runtimeOptions", runtimeOptions);

            File.WriteAllText(destFile, json.ToString());
        }
    }
}
