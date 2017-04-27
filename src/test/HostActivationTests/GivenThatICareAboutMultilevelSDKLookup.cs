using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.InternalAbstractions;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.MultilevelSDKLookup
{
    public class GivenThatICareAboutMultilevelSDKLookup
    {
        private RepoDirectoriesProvider RepoDirectories;
        private TestProjectFixture PreviouslyBuiltAndRestoredPortableTestProjectFixture;

        private string _currentWorkingDir;
        private string _userDir;
        private string _executableDir;
        private string _cwdSdkBaseDir;
        private string _userSdkBaseDir;
        private string _exeSdkBaseDir;
        private string _cwdSelectedMessage;
        private string _userSelectedMessage;
        private string _exeSelectedMessage;
        private string _sdkDir;

        private const string _dotnetSdkDllMessageTerminator = "dotnet.dll]";

        public GivenThatICareAboutMultilevelSDKLookup()
        {
            // From the artifacts dir, it's possible to find where the sharedFrameworkPublish folder is. We need
            // to locate it because we'll copy its contents into other folders
            string artifactsDir = Environment.GetEnvironmentVariable("TEST_ARTIFACTS");
            string builtDotnet = Path.Combine(artifactsDir, "sharedFrameworkPublish");

            // The dotnetMultilevelSDKLookup dir will contain some folders and files that will be
            // necessary to perform the tests
            string baseMultilevelDir = Path.Combine(artifactsDir, "dotnetMultilevelSDKLookup");
            string multilevelDir = CalculateMultilevelDirectory(baseMultilevelDir);

            // The three tested locations will be the cwd, the user folder and the exe dir. Both cwd and exe dir
            // are easily overwritten, so they will be placed inside the multilevel folder. The actual user location will
            // be used during tests.
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

            // It's necessary to copy the entire publish folder to the exe dir because
            // we'll need to build from it. The CopyDirectory method automatically creates the dest dir
            CopyDirectory(builtDotnet, _executableDir);

            // SdkBaseDirs contain all available version folders
            _cwdSdkBaseDir = Path.Combine(_currentWorkingDir, "sdk");
            _userSdkBaseDir = Path.Combine(_userDir, ".dotnet", RuntimeEnvironment.RuntimeArchitecture, "sdk");
            _exeSdkBaseDir = Path.Combine(_executableDir, "sdk");

            // Create directories
            Directory.CreateDirectory(_cwdSdkBaseDir);
            Directory.CreateDirectory(_userSdkBaseDir);
            Directory.CreateDirectory(_exeSdkBaseDir);

            // Restore and build PortableApp from exe dir
            RepoDirectories = new RepoDirectoriesProvider(builtDotnet: _executableDir);
            PreviouslyBuiltAndRestoredPortableTestProjectFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .BuildProject();
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture;

            // Set a dummy framework version (9999.0.0) in the cwd sharedFx location. We will
            // always pick the framework from cwd to avoid interference with the sharedFxLookup
            // test folders in the user dir
            string cwdDummyFxVersion = Path.Combine(_currentWorkingDir, "shared", "Microsoft.NETCore.App", "9999.0.0");
            string builtSharedFxDir = fixture.BuiltDotnet.GreatestVersionSharedFxPath;
            CopyDirectory(builtSharedFxDir, cwdDummyFxVersion);

            // The actual SDK version can be obtained from the built fixture. We'll use it to
            // locate the sdkDir from which we can get the files contained in the version folder
            string sdkBaseDir = Path.Combine(fixture.SdkDotnet.BinPath, "sdk");

            var sdkVersionDirs = Directory.EnumerateDirectories(sdkBaseDir);
            string greatestVersionSdk = sdkVersionDirs
                .OrderByDescending(p => p.ToLower())
                .First();

            _sdkDir = Path.Combine(sdkBaseDir, greatestVersionSdk);

            // Trace messages used to identify from which folder the SDK was picked
            _cwdSelectedMessage = $"Using dotnet SDK dll=[{_cwdSdkBaseDir}";
            _userSelectedMessage = $"Using dotnet SDK dll=[{_userSdkBaseDir}";
            _exeSelectedMessage = $"Using dotnet SDK dll=[{_exeSdkBaseDir}";
        }

        [Fact]
        public void SdkLookup_Must_Verify_Folders_in_the_Correct_Order()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Add a dummy version in the exe dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.0-dummy");

            // Specified CLI version: none
            // CWD: empty
            // User: empty
            // Exe: 9999.0.0
            // Expected: 9999.0.0 from exe dir
            dotnet.Exec("help")
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
            AddAvailableSdkVersions(_userSdkBaseDir, "9999.0.0-dummy");

            // Specified CLI version: none
            // CWD: empty
            // User: 9999.0.0
            // Exe: 9999.0.0
            // Expected: 9999.0.0 from user dir
            dotnet.Exec("help")
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
            AddAvailableSdkVersions(_cwdSdkBaseDir, "9999.0.0-dummy");

            // Specified CLI version: none
            // CWD: 9999.0.0
            // User: 9999.0.0
            // Exe: 9999.0.0
            // Expected: 9999.0.0 from cwd
            dotnet.Exec("help")
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
            DeleteAvailableSdkVersions(_userSdkBaseDir, "9999.0.0-dummy");
        }

        [Fact]
        public void SdkLookup_Must_Look_For_Available_Versions_Before_Looking_Into_Another_Folder()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Set specified CLI version = 9999.0.0-global-dummy
            SetGlobalJsonVersion();

            // Add some dummy versions
            AddAvailableSdkVersions(_userSdkBaseDir, "9999.0.0", "9999.0.0-dummy");
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.0-dummy");

            // Specified CLI version: 9999.0.0-global-dummy
            // CWD: empty
            // User: 9999.0.0, 9999.0.0-dummy
            // Exe: 9999.0.0-dummy
            // Expected: 9999.0.0 from user dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_userSelectedMessage, "9999.0.0", _dotnetSdkDllMessageTerminator));

            // Add some dummy versions
            AddAvailableSdkVersions(_cwdSdkBaseDir, "9999.0.0");
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.0-global-dummy");

            // Specified CLI version: 9999.0.0-global-dummy
            // CWD: 9999.0.0
            // User: 9999.0.0, 9999.0.0-dummy
            // Exe: 9999.0.0-dummy, 9999.0.0-global-dummy
            // Expected: 9999.0.0 from cwd
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_cwdSelectedMessage, "9999.0.0", _dotnetSdkDllMessageTerminator));
            
            // Add a prerelease dummy version in the cwd
            AddAvailableSdkVersions(_cwdSdkBaseDir, "9999.0.0-global-dummy");

            // Specified CLI version: 9999.0.0-global-dummy
            // CWD: 9999.0.0, 9999.0.0-global-dummy
            // User: 9999.0.0, 9999.0.0-dummy
            // Exe: 9999.0.0-dummy, 9999.0.0-global-dummy
            // Expected: 9999.0.0-global-dummy from cwd
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_cwdSelectedMessage, "9999.0.0-global-dummy", _dotnetSdkDllMessageTerminator));

            // Remove dummy folders from user dir
            DeleteAvailableSdkVersions(_userSdkBaseDir, "9999.0.0", "9999.0.0-dummy");
        }

        [Fact]
        public void SdkLookup_Must_Pick_The_Highest_Semantic_Version()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Add a dummy version in the exe dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.0");

            // Specified CLI version: none
            // CWD: empty
            // User: empty
            // Exe: 9999.0.0
            // Expected: 9999.0.0 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.0", _dotnetSdkDllMessageTerminator));

            // Add a dummy version in the exe dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.1");

            // Specified CLI version: none
            // CWD: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.1
            // Expected: 9999.0.1 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.1", _dotnetSdkDllMessageTerminator));

            // Add a dummy version in the exe dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.0-dummy");

            // Specified CLI version: none
            // CWD: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.1, 9999.0.0-dummy
            // Expected: 9999.0.1 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.1", _dotnetSdkDllMessageTerminator));

            // Add a dummy version in the exe dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "10000.0.0-dummy");

            // Specified CLI version: none
            // CWD: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.1, 9999.0.0-dummy, 10000.0.0-dummy
            // Expected: 10000.0.0-dummy from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "10000.0.0-dummy", _dotnetSdkDllMessageTerminator));

            // Add a dummy version in the exe dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "10000.0.0");

            // Specified CLI version: none
            // CWD: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.1, 9999.0.0-dummy, 10000.0.0-dummy, 10000.0.0
            // Expected: 10000.0.0 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "10000.0.0", _dotnetSdkDllMessageTerminator));

        }

        // This method adds a list of new sdk version folders in the specified
        // sdkBaseDir. The files are copied from the _sdkDir. Also, the dotnet.runtimeconfig.json
        // file is overwritten in order to use a dummy framework version (9999.0.0)
        // Remarks:
        // - If the sdkBaseDir does not exist, then a DirectoryNotFoundException
        //   is thrown.
        // - If a specified version folder already exists, then it is deleted and replaced
        //   with the contents of the _builtSharedFxDir.
        private void AddAvailableSdkVersions(string sdkBaseDir, params string[] availableVersions)
        {
            DirectoryInfo sdkBaseDirInfo = new DirectoryInfo(sdkBaseDir);

            if (!sdkBaseDirInfo.Exists)
            {
                throw new DirectoryNotFoundException();
            }

            string dummyRuntimeConfig = Path.Combine(RepoDirectories.RepoRoot, "src", "test", "Assets", "TestUtils",
                "SDKLookup", "dotnet.runtimeconfig.json");

            foreach (string version in availableVersions)
            {
                string newSdkDir = Path.Combine(sdkBaseDir, version);
                CopyDirectory(_sdkDir, newSdkDir);

                string runtimeConfig = Path.Combine(newSdkDir, "dotnet.runtimeconfig.json");
                File.Copy(dummyRuntimeConfig, runtimeConfig, true);
            }
        }

        // This method removes a list of sdk version folders from the specified sdkBaseDir.
        // Remarks:
        // - If the sdkBaseDir does not exist, then a DirectoryNotFoundException
        //   is thrown.
        // - If a specified version folder does not exist, then a DirectoryNotFoundException
        //   is thrown.
        private void DeleteAvailableSdkVersions(string sdkBaseDir, params string[] availableVersions)
        {
            DirectoryInfo sdkBaseDirInfo = new DirectoryInfo(sdkBaseDir);

            if (!sdkBaseDirInfo.Exists)
            {
                throw new DirectoryNotFoundException();
            }

            foreach (string version in availableVersions)
            {
                string sdkDir = Path.Combine(sdkBaseDir, version);
                if (!Directory.Exists(sdkDir))
                {
                    throw new DirectoryNotFoundException();
                }
                Directory.Delete(sdkDir, true);
            }
        }

        // CopyDirectory recursively copies a directory.
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

        // Put a global.json file in the cwd in order to specify a CLI
        // dummy version (9999.0.0-global-dummy)
        public void SetGlobalJsonVersion()
        {
            string destFile = Path.Combine(_currentWorkingDir, "global.json");
            string srcFile = Path.Combine(RepoDirectories.RepoRoot, "src", "test", "Assets", "TestUtils",
                "SDKLookup", "global.json");

            File.Copy(srcFile, destFile, true);
        }

        // MultilevelDirectory is %TEST_ARTIFACTS%\dotnetMultilevelSDKLookup\id.
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
