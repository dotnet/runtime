using Microsoft.DotNet.InternalAbstractions;
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
        private string _cwdSharedUberFxBaseDir;
        private string _userSharedFxBaseDir;
        private string _userSharedUberFxBaseDir;
        private string _exeSharedFxBaseDir;
        private string _exeSharedUberFxBaseDir;
        private string _globalSharedFxBaseDir;
        private string _globalSharedUberFxBaseDir;
        private string _builtSharedFxDir;
        private string _builtSharedUberFxDir;

        private string _cwdSelectedMessage;
        private string _userSelectedMessage;
        private string _exeSelectedMessage;
        private string _globalSelectedMessage;

        private string _cwdFoundUberFxMessage;
        private string _userFoundUberFxMessage;
        private string _exeFoundUberFxMessage;
        private string _globalFoundUberFxMessage;

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

            _cwdSharedUberFxBaseDir = Path.Combine(_currentWorkingDir, "shared", "Microsoft.UberFramework");
            _userSharedUberFxBaseDir = Path.Combine(_userDir, ".dotnet", RepoDirectories.BuildArchitecture, "shared", "Microsoft.UberFramework");
            _exeSharedUberFxBaseDir = Path.Combine(_executableDir, "shared", "Microsoft.UberFramework");
            _globalSharedUberFxBaseDir = Path.Combine(_globalDir, "shared", "Microsoft.UberFramework");

            // Create directories. It's necessary to copy the entire publish folder to the exe dir because
            // we'll need to build from it. The CopyDirectory method automatically creates the dest dir
            Directory.CreateDirectory(_cwdSharedFxBaseDir);
            Directory.CreateDirectory(_userSharedFxBaseDir);
            Directory.CreateDirectory(_globalSharedFxBaseDir);
            Directory.CreateDirectory(_cwdSharedUberFxBaseDir);
            Directory.CreateDirectory(_userSharedUberFxBaseDir);
            Directory.CreateDirectory(_globalSharedUberFxBaseDir);
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

            // The uber framework is a copy of the base framework, minus a few files
            _builtSharedUberFxDir = Path.Combine(_builtDotnet, "shared", "Microsoft.UberFramework", _sharedFxVersion);
            CreateUberFrameworkArtifacts(_builtSharedFxDir, _builtSharedUberFxDir, "1.0.1.2", "1.2.3.4");

            _hostPolicyDllName = Path.GetFileName(fixture.TestProject.HostPolicyDll);

            // Trace messages used to identify from which folder the framework was picked
            _cwdSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_cwdSharedFxBaseDir}";
            _userSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_userSharedFxBaseDir}";
            _exeSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_exeSharedFxBaseDir}";
            _globalSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_globalSharedFxBaseDir}";

            _cwdFoundUberFxMessage = $"Chose FX version [{_cwdSharedUberFxBaseDir}";
            _userFoundUberFxMessage = $"Chose FX version [{_userSharedUberFxBaseDir}";
            _exeFoundUberFxMessage = $"Chose FX version [{_exeSharedUberFxBaseDir}";
            _globalFoundUberFxMessage = $"Chose FX version [{_globalSharedUberFxBaseDir}";
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

            // Add version in the exe dir
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
                .WithUserProfile(_userDir)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.0");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0");
            DeleteAvailableSharedFxVersions(_cwdSharedFxBaseDir, "9999.0.0");
        }

        [Fact]
        public void SharedFxLookup_Must_Not_Roll_Forward_If_Framework_Version_Is_Specified_Through_Argument()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Add some dummy versions
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0", "9999.0.2", "9999.0.0-dummy2", "9999.0.3", "9999.0.0-dummy3");

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
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.0")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.0-dummy2")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.2")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.3")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.0-dummy3");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0", "9999.0.2", "9999.0.0-dummy2", "9999.0.3", "9999.0.0-dummy3");
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
            // 'Roll forward on no candidate fx' enabled with value 2 (major+minor) through env var
            // exe: 10000.1.1, 10000.1.3
            // Expected: 10000.1.3 from exe
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "2")
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
            // 'Roll forward on no candidate fx' enabled with value 2 (major+minor) through env var
            // exe: 9999.1.1, 10000.1.1, 10000.1.3
            // Expected: 9999.1.1 from exe
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "2")
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
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.1.1")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 10000.1.1")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 10000.1.3");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.1", "10000.1.1", "10000.1.3");
        }

        [Fact]
        public void Roll_Forward_On_No_Candidate_Fx_Minor_And_Disabled()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 9999.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "9999.0.0");

            // Add some dummy versions in the exe
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "10000.1.1");

            // Version: 9999.0.0
            // 'Roll forward on no candidate fx' default value of 1 (minor)
            // exe: 10000.1.1
            // Expected: fail with no framework
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("It was not possible to find any compatible framework version");

            // Add a dummy version in the exe dir 
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.1");

            // Version: 9999.0.0
            // 'Roll forward on no candidate fx' default value of 1 (minor)
            // exe: 9999.1.1, 10000.1.1
            // Expected: 9999.1.1 from exe
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.1.1"));

            // Version: 9999.0.0
            // 'Roll forward on no candidate fx' disabled through env var
            // exe: 9999.1.1, 10000.1.1
            // Expected: fail with no framework
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
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
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.1.1")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 10000.1.1");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.1", "10000.1.1");
        }

        [Fact]
        public void Roll_Forward_On_No_Candidate_Fx_Production_To_Preview()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 9999.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "9999.0.0");

            // Add preview version in the exe
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.1-dummy1");

            // Version: 9999.0.0
            // 'Roll forward on no candidate fx' default value of 1 (minor)
            // exe: 9999.1.1-dummy1
            // Expected: 9999.1.1-dummy1 since there is no production version
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.1.1-dummy1"));

            // Add a production version with higher value
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.2.1");

            // Version: 9999.0.0
            // 'Roll forward on no candidate fx' default value of 1 (minor)
            // exe: 9999.1.1-dummy1, 9999.2.1
            // Expected: 9999.2.1 since we favor production over preview
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.2.1"));

            // Add a preview version with same major.minor as production
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.2.1-dummy1");

            // Version: 9999.0.0
            // 'Roll forward on no candidate fx' default value of 1 (minor)
            // exe: 9999.1.1-dummy1, 9999.2.1, 9999.2.1-dummy1
            // Expected: 9999.2.1 since we favor production over preview
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.2.1"));

            // Add a preview version with same major.minor as production but higher patch version
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.2.2-dummy1");

            // Version: 9999.0.0
            // 'Roll forward on no candidate fx' default value of 1 (minor)
            // exe: 9999.1.1-dummy1, 9999.2.1, 9999.2.1-dummy1, 9999.2.2-dummy1
            // Expected: 9999.2.1 since we favor production over preview
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.2.1"));

            // Verify we have the expected runtime versions
            dotnet.Exec("--list-runtimes")
                .WorkingDirectory(_currentWorkingDir)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.1.1-dummy1")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.2.1")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.2.1-dummy1")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.2.2-dummy1");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.1-dummy1", "9999.2.1", "9999.2.1-dummy1", "9999.2.2-dummy1");
        }

        [Fact]
        public void Roll_Forward_On_No_Candidate_Fx_Preview_To_Production()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 9999.0.0-dummy1
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "9999.0.0-dummy1");

            // Add dummy versions in the exe
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0", "9999.0.1-dummy1");

            // Version: 9999.0.0-dummy1
            // exe: 9999.0.0, 9999.0.1-dummy1
            // Expected: fail since we don't roll forward unless match on major.minor.patch and never roll forward to production
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("It was not possible to find any compatible framework version");

            // Add preview versions in the exe with name major.minor.patch
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0-dummy2", "9999.0.0-dummy3");

            // Version: 9999.0.0-dummy1
            // exe: 9999.0.0-dummy2, 9999.0.0-dummy3, 9999.0.0, 9999.0.1-dummy1
            // Expected: 9999.0.0-dummy2
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.0-dummy2"));

            // Verify we have the expected runtime versions
            dotnet.Exec("--list-runtimes")
                .WorkingDirectory(_currentWorkingDir)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("9999.0.0-dummy2")
                .And
                .HaveStdOutContaining("9999.0.0-dummy3")
                .And
                .HaveStdOutContaining("9999.0.0")
                .And
                .HaveStdOutContaining("9999.0.1-dummy1");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0-dummy2", "9999.0.0-dummy3", "9999.0.0", "9999.0.1-dummy1");
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
            // exe: 9998.0.1, 9998.1.0, 9999.0.0, 9999.0.1, 9999.1.0
            // Expected: no compatible version
            dotnet.Exec(appDll)
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
                .HaveStdOutContaining("Microsoft.NETCore.App 9998.0.1")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9998.1.0")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.0")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.1")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.1.0");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9998.0.1", "9998.1.0", "9999.0.0", "9999.0.1", "9999.1.0");
        }

        [Fact]
        public void Multiple_SharedFxLookup_Independent_Roll_Forward()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folders
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0");
            AddAvailableSharedUberFxVersions(_exeSharedUberFxBaseDir, "9999.0.0", null, "7777.0.0");

            // Version: NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            // Exe: NetCoreApp 9999.0.0
            //      UberFramework 7777.0.0
            // Expected: 9999.0.0
            //           7777.0.0
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.0"))
                .And
                .HaveStdErrContaining(Path.Combine(_exeFoundUberFxMessage, "7777.0.0"));

            // Add a newer version to verify roll-forward
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.1");
            AddAvailableSharedUberFxVersions(_exeSharedUberFxBaseDir, "9999.0.0", null, "7777.0.1");

            // Version: NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            // Exe: NetCoreApp 9999.0.0, 9999.0.1
            //      UberFramework 7777.0.0, 7777.0.1
            // Expected: 9999.0.1
            //           7777.0.1
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.1"))
                .And
                .HaveStdErrContaining(Path.Combine(_exeFoundUberFxMessage, "7777.0.1"));

            // Verify we have the expected runtime versions
            dotnet.Exec("--list-runtimes")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.0")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.1")
                .And
                .HaveStdOutContaining("Microsoft.UberFramework 7777.0.0")
                .And
                .HaveStdOutContaining("Microsoft.UberFramework 7777.0.1");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0", "9999.0.1");
            DeleteAvailableSharedFxVersions(_exeSharedUberFxBaseDir, "7777.0.0", "7777.0.1");
        }

        [Fact]
        public void Multiple_SharedFxLookup_Propagated_Global_RuntimeConfig_Values()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folders
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.0");
            AddAvailableSharedUberFxVersions(_exeSharedUberFxBaseDir, "9999.0.0", "UberValue", "7777.0.0");

            // Version: NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            // 'Roll forward on no candidate fx' disabled through env var
            // Exe: NetCoreApp 9999.1.0
            //      UberFramework 7777.0.0
            // Expected: no compatible version
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("It was not possible to find any compatible framework version");

            // Enable rollForwardOnNoCandidateFx on app's config, which will be used as the default for Uber's config
            SetRuntimeConfigJson(runtimeConfig, "7777.0.0", rollFwdOnNoCandidateFx: 1, testConfigPropertyValue : null, useUberFramework: true);

            // Version: NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            //          'Roll forward on no candidate fx' enabled through config
            // Exe: NetCoreApp 9999.1.0
            //      UberFramework 7777.0.0
            // Expected: 9999.1.0
            //           7777.0.0
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.1.0"))
                .And
                .HaveStdErrContaining(Path.Combine(_exeFoundUberFxMessage, "7777.0.0"))
                .And
                .HaveStdErrContaining("Property TestProperty = UberValue");

            // Change the app's TestProperty value which should override the uber's config value
            SetRuntimeConfigJson(runtimeConfig, "7777.0.0", rollFwdOnNoCandidateFx: 1, testConfigPropertyValue: "AppValue", useUberFramework: true);

            // Version: NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            //          'Roll forward on no candidate fx' enabled through config
            // Exe: NetCoreApp 9999.1.0
            //      UberFramework 7777.0.0
            // Expected: 9999.1.0
            //           7777.0.0
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.1.0"))
                .And
                .HaveStdErrContaining(Path.Combine(_exeFoundUberFxMessage, "7777.0.0"))
                .And
                .HaveStdErrContaining("Property TestProperty = AppValue");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.0");
            DeleteAvailableSharedFxVersions(_exeSharedUberFxBaseDir, "7777.0.0");
        }

        [Fact]
        public void Multiple_SharedFxLookup_Propagated_Additional_Framework_RuntimeConfig_Values()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");

            var additionalfxs = new JArray();
            additionalfxs.Add(GetAdditionalFramework("Microsoft.NETCore.App", "9999.1.0", applyPatches: false, rollForwardOnNoCandidateFx: 0));
            SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true, additionalFrameworks : additionalfxs);

            // Add versions in the exe folders
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.0");
            AddAvailableSharedUberFxVersions(_exeSharedUberFxBaseDir, "9999.5.5", "UberValue", "7777.0.0");

            // Version: NetCoreApp 9999.5.5 (in framework section)
            //          NetCoreApp 9999.1.0 (in app's additionalFrameworks section)
            //          UberFramework 7777.0.0
            // Exe: NetCoreApp 9999.1.0
            //      UberFramework 7777.0.0
            // Expected: 9999.1.0
            //           7777.0.0
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.1.0"))
                .And
                .HaveStdErrContaining(Path.Combine(_exeFoundUberFxMessage, "7777.0.0"));

            // Change the additionalFrameworks to allow roll forward, overriding Uber's global section and ignoring Uber's framework section
            additionalfxs.Clear();
            additionalfxs.Add(GetAdditionalFramework("Microsoft.NETCore.App", "9999.0.0", applyPatches: false, rollForwardOnNoCandidateFx: 1));
            additionalfxs.Add(GetAdditionalFramework("UberFx", "7777.0.0", applyPatches: false, rollForwardOnNoCandidateFx: 0));
            SetRuntimeConfigJson(runtimeConfig, "7777.0.0", rollFwdOnNoCandidateFx:0, useUberFramework: true, additionalFrameworks: additionalfxs);

            // Version: NetCoreApp 9999.5.5 (in framework section)
            //          NetCoreApp 9999.0.0 (in app's additionalFrameworks section)
            //          UberFramework 7777.0.0
            //          UberFramework 7777.0.0 (in app's additionalFrameworks section)
            // 'Roll forward on no candidate fx' disabled through env var
            // 'Roll forward on no candidate fx' disabled through Uber's global runtimeconfig
            // 'Roll forward on no candidate fx' enabled for NETCore.App enabled through additionalFrameworks section
            // Exe: NetCoreApp 9999.1.0
            //      UberFramework 7777.0.0
            // Expected: 9999.1.0
            //           7777.0.0
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.1.0"))
                .And
                .HaveStdErrContaining(Path.Combine(_exeFoundUberFxMessage, "7777.0.0"));

            // Same as previous except use of '--roll-forward-on-no-candidate-fx'
            // Expected: Fail since '--roll-forward-on-no-candidate-fx' should apply to all layers
            dotnet.Exec(
                    "exec",
                    "--roll-forward-on-no-candidate-fx", "0",
                    appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("It was not possible to find any compatible framework version");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.0");
            DeleteAvailableSharedFxVersions(_exeSharedUberFxBaseDir, "7777.0.0");
        }

        /* This test will be added once the SDK write the assemblyVersion and fileVersion properties. Verified manually.
        [Fact]
        public void Multiple_SharedFxLookup_NetCoreApp_MinorRollForward_Wins_Over_UberFx()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folders
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.0");
            AddAvailableSharedUberFxVersions(_exeSharedUberFxBaseDir, "9999.0.0", null, "7777.0.0");

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
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining($"Replacing deps entry [{uberFile}, AssemblyVersion:1.0.1.2, FileVersion:1.2.3.4] with [{netCoreAppFile}");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.1.0");
            DeleteAvailableSharedFxVersions(_exeSharedUberFxBaseDir, "7777.0.0");
        }
        */

        [Fact]
        public void Multiple_SharedFxLookup_Uber_Wins_Over_NetCoreApp_On_PatchRollForward()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folders
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.1");
            AddAvailableSharedUberFxVersions(_exeSharedUberFxBaseDir, "9999.0.0", null, "7777.0.0");

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
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine("7777.0.0", "System.Collections.Immutable.dll"))
                .And
                .NotHaveStdErrContaining(Path.Combine("9999.1.0", "System.Collections.Immutable.dll"));

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.1");
            DeleteAvailableSharedFxVersions(_exeSharedUberFxBaseDir, "7777.0.0");
        }

        [Fact]
        public void Additional_Deps_Lightup_Folder_With_Roll_Forward_And_Bad_JsonFile()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Add version in the exe folder
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.1");

            // Set desired version = 9999.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "9999.0.0");

            // Create a deps.json file in the folder "additionalDeps\shared\Microsoft.NETCore.App\9999.0.0"
            string additionalDepsRootPath = Path.Combine(_exeSharedFxBaseDir, "additionalDeps");
            string additionalDepsPath = Path.Combine(additionalDepsRootPath, "shared", "Microsoft.NETCore.App", "9999.0.0", "myAddtionalDeps.deps.json");
            FileInfo additionalDepsFile = new FileInfo(additionalDepsPath);
            additionalDepsFile.Directory.Create();
            File.WriteAllText(additionalDepsFile.FullName, "THIS IS A BAD JSON FILE");

            // Version: NetCoreApp 9999.0.0
            // Exe: NetCoreApp 9999.0.1
            // Expected: 9999.0.1
            // Expected: the "specified" location (9999.0.0) is used to find the lightup folder, not the "found" location (9999.0.1)
            dotnet.Exec("exec", "--additional-deps", additionalDepsRootPath, appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.1"))
                .And
                .HaveStdErrContaining($"Error initializing the dependency resolver: An error occurred while parsing: {additionalDepsPath}");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.1", "additionalDeps");
        }

        [Fact]
        public void SharedFxLookup_Wins_Over_Additional_Deps_On_RollForward_And_Version_Tie()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 7777.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folder
            AddAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0");
            AddAvailableSharedUberFxVersions(_exeSharedUberFxBaseDir, "9999.0.0", null, "7777.1.0");

            // Copy NetCoreApp's copy of the assembly to the app location
            string fxAssemblyPath = Path.Combine(_exeSharedFxBaseDir, "9999.0.0", "System.Collections.Immutable.dll");
            string appAssembly = Path.Combine(fixture.TestProject.OutputDirectory, "System.Collections.Immutable.dll");
            File.Copy(fxAssemblyPath, appAssembly);

            // Modify the app's deps.json to add System.Collections.Immmutable
            string appDepsJson = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.deps.json");
            AddImmutableAssemblyToDepsJson(appDepsJson);

            // Version: NetCoreApp 9999.0.0
            //          UberFramework 7777.0.0
            // Exe: NetCoreApp 9999.0.0
            //      UberFramework 7777.1.0
            // Expected: 9999.0.0
            //           7777.1.0
            // Expected: the framework's version of System.Collections.Immutable is used
            string fxAssembly = Path.Combine(_exeSharedUberFxBaseDir, "7777.1.0", "System.Collections.Immutable.dll");
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining($"Replacing deps entry [{appAssembly}, AssemblyVersion:1.0.1.2, FileVersion:1.2.3.4] with [{fxAssembly}, AssemblyVersion:1.0.1.2, FileVersion:1.2.3.4]");

            DeleteAvailableSharedFxVersions(_exeSharedFxBaseDir, "9999.0.0");
            DeleteAvailableSharedFxVersions(_exeSharedUberFxBaseDir, "7777.1.0");
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

        // This method adds a list of new framework version folders in the specified
        // sharedFxUberBaseDir. A runtimeconfig file is created that references
        // Microsoft.NETCore.App version=sharedFxBaseVersion
        private void AddAvailableSharedUberFxVersions(string sharedUberFxBaseDir, string sharedFxBaseVersion, string testConfigPropertyValue = null, params string[] availableUberVersions)
        {
            DirectoryInfo sharedFxUberBaseDirInfo = new DirectoryInfo(sharedUberFxBaseDir);

            if (!sharedFxUberBaseDirInfo.Exists)
            {
                sharedFxUberBaseDirInfo.Create();
            }

            foreach (string version in availableUberVersions)
            {
                string newSharedFxDir = Path.Combine(sharedUberFxBaseDir, version);
                CopyDirectory(_builtSharedUberFxDir, newSharedFxDir);

                string runtimeBaseConfig = Path.Combine(newSharedFxDir, "Microsoft.UberFramework.runtimeconfig.json");
                SetRuntimeConfigJson(runtimeBaseConfig, sharedFxBaseVersion, null, testConfigPropertyValue);
            }
        }

        // This method removes a list of framework version folders from the specified
        // sharedFxBaseDir.
        // Remarks:
        // - If the sharedFxBaseDir does not exist, then a DirectoryNotFoundException
        //   is thrown.
        // - If a specified version folder does not exist, then a DirectoryNotFoundException
        //   is thrown.
        static private void DeleteAvailableSharedFxVersions(string sharedFxBaseDir, params string[] availableVersions)
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
        static private void CopyDirectory(string srcDir, string dstDir)
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
        static private string CalculateMultilevelDirectory(string baseMultilevelDir)
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
        private void SetRuntimeConfigJson(string destFile, string version, int? rollFwdOnNoCandidateFx = null, string testConfigPropertyValue = null, bool? useUberFramework = false, JArray additionalFrameworks = null)
        {
            string name = useUberFramework.HasValue && useUberFramework.Value ? "Microsoft.UberFramework" : "Microsoft.NETCore.App";

            JObject runtimeOptions = new JObject(
                new JProperty("framework",
                    new JObject(
                        new JProperty("name", name),
                        new JProperty("version", version)
                    )
                )
            );

            if (rollFwdOnNoCandidateFx.HasValue)
            {
                runtimeOptions.Add("rollForwardOnNoCandidateFx", rollFwdOnNoCandidateFx);
            }

            if (testConfigPropertyValue != null)
            {
                runtimeOptions.Add(
                    new JProperty("configProperties",
                        new JObject(
                            new JProperty("TestProperty", testConfigPropertyValue)
                        )
                    )
                );
            }

            if (additionalFrameworks != null)
            {
                runtimeOptions.Add("additionalFrameworks", additionalFrameworks);
            }

            FileInfo file = new FileInfo(destFile);
            if (!file.Directory.Exists)
            {
                file.Directory.Create();
            }

            JObject json = new JObject();
            json.Add("runtimeOptions", runtimeOptions);
            File.WriteAllText(destFile, json.ToString());
        }

        static private JObject GetAdditionalFramework(string fxName, string fxVersion, bool? applyPatches, int? rollForwardOnNoCandidateFx)
        {
            var jobject = new JObject(new JProperty("name", fxName));

            if (fxVersion != null)
            {
                jobject.Add(new JProperty("version", fxVersion));
            }

            if (applyPatches.HasValue)
            {
                jobject.Add(new JProperty("applyPatches", applyPatches.Value));
            }

            if (rollForwardOnNoCandidateFx.HasValue)
            {
                jobject.Add(new JProperty("rollForwardOnNoCandidateFx", rollForwardOnNoCandidateFx));
            }

            return jobject;
        }

        static private void CreateUberFrameworkArtifacts(string builtSharedFxDir, string builtSharedUberFxDir, string assemblyVersion = null, string fileVersion = null)
        {
            DirectoryInfo dir = new DirectoryInfo(builtSharedUberFxDir);
            if (dir.Exists)
            {
                dir.Delete(true);
            }

            dir.Create();

            string fxName = "UberFx";
            string testPackage = "System.Collections.Immutable/1.0.0";
            string testAssembly = "System.Collections.Immutable";

            // Create the deps.json. Generated file:
            /*
                {
                  "runtimeTarget": {
                    "name": "UberFx"
                  },
                  "targets": {
                    "UberFx": {
                      "System.Collections.Immutable/1.0.0": {
                        "runtime": {
                          "System.Collections.Immutable.dll": {}
                        }
                      }
                    }
                  },
                  "libraries": {
                    "System.Collections.Immutable/1.0.0": {
                      "type": "assemblyreference",
                      "serviceable": false,
                      "sha512": ""
                    }
                  }
                }
             */
            JObject versionInfo = new JObject();
            if (assemblyVersion != null)
            {
                versionInfo.Add(new JProperty("assemblyVersion", assemblyVersion));
            }

            if (fileVersion != null)
            {
                versionInfo.Add(new JProperty("fileVersion", fileVersion));
            }

            JObject depsjson = new JObject(
                new JProperty("runtimeTarget",
                    new JObject(
                        new JProperty("name", fxName)
                    )
                ),
                new JProperty("targets",
                    new JObject(
                      new JProperty(fxName,
                          new JObject(
                              new JProperty(testPackage,
                                  new JObject(
                                      new JProperty("runtime",
                                          new JObject(
                                              new JProperty(testAssembly + ".dll",
                                                  versionInfo
                                              )
                                          )
                                      )
                                  )
                              )
                          )
                      )
                  )
              ),
                  new JProperty("libraries",
                      new JObject(
                          new JProperty(testPackage,
                            new JObject(
                                new JProperty("type", "assemblyreference"),
                                new JProperty("serviceable", false),
                                new JProperty("sha512", "")
                            )
                        )
                    )
                )
            );

            string depsFile = Path.Combine(builtSharedUberFxDir, "Microsoft.UberFramework.deps.json");

            File.WriteAllText(depsFile, depsjson.ToString());

            // Copy the test assembly
            string fileSource = Path.Combine(builtSharedFxDir, testAssembly + ".dll");
            string fileDest = Path.Combine(builtSharedUberFxDir, testAssembly + ".dll");
            File.Copy(fileSource, fileDest);
        }

        static private void AddImmutableAssemblyToDepsJson(string jsonFile)
        {
            JObject depsjson = JObject.Parse(File.ReadAllText(jsonFile));

            string assemblyVersion = "1.0.1.2";
            string fileVersion = "1.2.3.4";
            string testPackage = "System.Collections.Immutable";
            string testPackageVersion = "1.0.0";
            string testPackageWithVersion = testPackage + "/" + testPackageVersion;
            string testAssembly = testPackage + ".dll";

            JProperty targetsProperty = (JProperty)depsjson["targets"].First;
            JObject targetsValue = (JObject)targetsProperty.Value;

            var assembly = new JProperty(testPackage, "1.0.0");
            JObject packageDependencies = (JObject)targetsValue["SharedFxLookupPortableApp/1.0.0"]["dependencies"];
            packageDependencies.Add(assembly);

            var package = new JProperty(testPackageWithVersion,
                new JObject(
                    new JProperty("runtime",
                        new JObject(
                            new JProperty(testAssembly,
                                new JObject(
                                    new JProperty("assemblyVersion", assemblyVersion),
                                    new JProperty("fileVersion", fileVersion)
                                )
                            )
                        )
                    )
                )
            );

            targetsValue.Add(package);

            var library = new JProperty(testPackageWithVersion,
                new JObject(
                    new JProperty("type", "assemblyreference"),
                    new JProperty("serviceable", false),
                    new JProperty("sha512", "")
                )
            );

            JObject libraries = (JObject)depsjson["libraries"];
            libraries.Add(library);

            File.WriteAllText(jsonFile, depsjson.ToString());
        }
    }
}
