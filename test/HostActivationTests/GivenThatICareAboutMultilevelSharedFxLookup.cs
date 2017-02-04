using System;
using System.IO;
using Xunit;
using Microsoft.DotNet.InternalAbstractions;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.MultilevelSharedFxLookup
{
    public class GivenThatICareAboutMultilevelSharedFxLookup
    {
        private RepoDirectoriesProvider RepoDirectories;
        private TestProjectFixture PreviouslyBuiltAndRestoredPortableTestProjectFixture;

        private string _currentWorkingDir;
        private string _userDir;
        private string _executableDir;
        private string _cwdSharedFxBaseDir;
        private string _userSharedFxBaseDir;
        private string _exeSharedFxBaseDir;
        private string _builtSharedFxDir;
        private string _cwdSelectedMessage;
        private string _userSelectedMessage;
        private string _exeSelectedMessage;
        private string _sharedFxVersion;

        public GivenThatICareAboutMultilevelSharedFxLookup()
        {
            // From the artifacts dir, it's possible to find where the sharedFrameworkPublish folder is. We need
            // to locate it because we'll copy its contents into other folders
            string artifactsDir = Environment.GetEnvironmentVariable("TEST_ARTIFACTS");
            string builtDotnet = Path.Combine(artifactsDir, "..", "..", "intermediate", "sharedFrameworkPublish");

            // The dotnetMultilevelSharedFxLookup dir will contain some folders and files that will be
            // necessary to perform the tests
            string baseMultilevelDir = Path.Combine(artifactsDir, "dotnetMultilevelSharedFxLookup");
            string multilevelDir = CalculateMultilevelDirectory(baseMultilevelDir);

            // The three tested locations will be the cwd, the user folder and the exe dir. Both cwd and exe dir
            // are easily overwritten, so they will be placed inside the multilevel folder. The actual user location will
            // be used during tests
            _currentWorkingDir = Path.Combine(multilevelDir, "cwd");
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Windows)
            {
                _userDir = Environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                _userDir = Environment.GetEnvironmentVariable("HOME");
            }
            _executableDir = Path.Combine(multilevelDir, "exe");

            // SharedFxBaseDirs contain all available version folders
            _cwdSharedFxBaseDir = Path.Combine(_currentWorkingDir, "shared", "Microsoft.NETCore.App");
            _userSharedFxBaseDir = Path.Combine(_userDir, ".dotnet", RuntimeEnvironment.RuntimeArchitecture, "shared", "Microsoft.NETCore.App");
            _exeSharedFxBaseDir = Path.Combine(_executableDir, "shared", "Microsoft.NETCore.App");

            // Create directories. It's necessary to copy the entire publish folder to the exe dir because
            // we'll need to build from it. The CopyDirectory method automatically creates the dest dir
            Directory.CreateDirectory(_cwdSharedFxBaseDir);
            Directory.CreateDirectory(_userSharedFxBaseDir);
            CopyDirectory(builtDotnet, _executableDir);

            // Restore and build SharedFxLookupPortableApp from exe dir
            RepoDirectories = new RepoDirectoriesProvider(builtDotnet:_executableDir);
            PreviouslyBuiltAndRestoredPortableTestProjectFixture = new TestProjectFixture("SharedFxLookupPortableApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages, RepoDirectories.CorehostDummyPackages)
                .BuildProject();
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture;

            // The actual framework version can be obtained from the built fixture. We'll use it to
            // locate the builtSharedFxDir from which we can get the files contained in the version folder
            string greatestVersionSharedFxPath = fixture.BuiltDotnet.GreatestVersionSharedFxPath;
            _sharedFxVersion = (new DirectoryInfo(greatestVersionSharedFxPath)).Name;
            _builtSharedFxDir = Path.Combine(builtDotnet, "shared", "Microsoft.NETCore.App", _sharedFxVersion);

            string hostPolicyDllName = Path.GetFileName(fixture.TestProject.HostPolicyDll);

            // Trace messages used to identify from which folder the framework was picked
            _cwdSelectedMessage = $"The expected {hostPolicyDllName} directory is [{_cwdSharedFxBaseDir}";
            _userSelectedMessage = $"The expected {hostPolicyDllName} directory is [{_userSharedFxBaseDir}";
            _exeSelectedMessage = $"The expected {hostPolicyDllName} directory is [{_exeSharedFxBaseDir}";
        }

        [Fact]
        public void SharedFxLookup_Must_Verify_Folders_in_the_Correct_Order()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 9999.0.0
            SetProductionRuntimeConfig(fixture);

            // Add a dummy version in the exe dir
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0");

            // Version: 9999.0.0
            // CWD: empty
            // User: empty
            // Exe: 9999.0.0
            // Expected: 9999.0.0 from exe dir
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
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
            // CWD: empty
            // User: 9999.0.0
            // Exe: 9999.0.0
            // Expected: 9999.0.0 from user dir
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(_userSelectedMessage);

            // Add a dummy version in the cwd
            AddAvailableSharedFxVersions(_cwdSharedFxBaseDir, "9999.0.0");

            // Version: 9999.0.0
            // CWD: 9999.0.0
            // User: 9999.0.0
            // Exe: 9999.0.0
            // Expected: 9999.0.0 from cwd
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(_cwdSelectedMessage);

            // Remove dummy folders from user dir
            DeleteAvailableSharedFxVersions(_userSharedFxBaseDir, "9999.0.0");
        }

        [Fact]
        public void SharedFxLookup_Must_Roll_Forward_Before_Looking_Into_Another_Folder()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Add some dummy versions
            AddAvailableSharedFxVersions(_userSharedFxBaseDir, "9999.0.2", "9999.0.0-dummy2");
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0", "9999.0.0-dummy0");

            // Set desired version = 9999.0.0-dummy0
            SetPrereleaseRuntimeConfig(fixture);

            // Version: 9999.0.0-dummy0
            // CWD: empty
            // User: 9999.0.2, 9999.0.0-dummy2
            // Exe: 9999.0.0, 9999.0.0-dummy0
            // Expected: 9999.0.0-dummy2 from user dir
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_userSelectedMessage, "9999.0.0-dummy2"));

            // Add a prerelease dummy version in CWD
            AddAvailableSharedFxVersions(_cwdSharedFxBaseDir, "9999.0.0-dummy1");

            // Version: 9999.0.0-dummy0
            // CWD: 9999.0.0-dummy1
            // User: 9999.0.2, 9999.0.0-dummy2
            // Exe: 9999.0.0, 9999.0.0-dummy0
            // Expected: 9999.0.0-dummy1 from cwd
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_cwdSelectedMessage, "9999.0.0-dummy1"));

            // Set desired version = 9999.0.0
            SetProductionRuntimeConfig(fixture);

            // Version: 9999.0.0
            // CWD: 9999.0.0-dummy1
            // User: 9999.0.2, 9999.0.0-dummy2
            // Exe: 9999.0.0, 9999.0.0-dummy0
            // Expected: 9999.0.2 from user dir
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_userSelectedMessage, "9999.0.2"));

            // Add a production dummy version in CWD
            AddAvailableSharedFxVersions(_cwdSharedFxBaseDir, "9999.0.1");

            // Version: 9999.0.0
            // CWD: 9999.0.1, 9999.0.0-dummy1
            // User: 9999.0.2, 9999.0.0-dummy2
            // Exe: 9999.0.0, 9999.0.0-dummy0
            // Expected: 9999.0.1 from cwd
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_cwdSelectedMessage, "9999.0.1"));

            // Remove dummy folders from user dir
            DeleteAvailableSharedFxVersions(_userSharedFxBaseDir, "9999.0.2", "9999.0.0-dummy2");
        }

        [Fact]
        public void SharedFxLookup_Must_Not_Roll_Forward_If_Framework_Version_Is_Specified_Through_Argument()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Add some dummy versions
            AddAvailableSharedFxVersions(_cwdSharedFxBaseDir, "9999.0.1", "9999.0.0-dummy0");
            AddAvailableSharedFxVersions(_userSharedFxBaseDir, "9999.0.2", "9999.0.0-dummy2");
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0", "9999.0.3", "9999.0.0-dummy3");

            // Version: 9999.0.0 (through --fx-version arg)
            // CWD: 9999.0.1, 9999.0.0-dummy0
            // User: 9999.0.2, 9999.0.0-dummy2
            // Exe: 9999.0.0, 9999.0.3, 9999.0.0-dummy3
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
            // CWD: 9999.0.1, 9999.0.0-dummy0
            // User: 9999.0.2, 9999.0.0-dummy2
            // Exe: 9999.0.0, 9999.0.3, 9999.0.0-dummy3
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

            // Remove dummy folders from user dir
            DeleteAvailableSharedFxVersions(_userSharedFxBaseDir, "9999.0.2", "9999.0.0-dummy2");
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

        // Overwrites the fixture's runtimeconfig.json. The specified version is 9999.0.0-dummy0
        private void SetPrereleaseRuntimeConfig(TestProjectFixture fixture)
        {
            string destFile = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            string srcFile = Path.Combine(RepoDirectories.RepoRoot, "TestAssets", "TestUtils",
                "SharedFxLookup", "SharedFxLookupPortableApp_prerelease.runtimeconfig.json");
            File.Copy(srcFile, destFile, true);
        }

        // Overwrites the fixture's runtimeconfig.json. The specified version is 9999.0.0
        private void SetProductionRuntimeConfig(TestProjectFixture fixture)
        {
            string destFile = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            string srcFile = Path.Combine(RepoDirectories.RepoRoot, "TestAssets", "TestUtils",
                "SharedFxLookup", "SharedFxLookupPortableApp_production.runtimeconfig.json");
            File.Copy(srcFile, destFile, true);
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
    }
}
