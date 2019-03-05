using Microsoft.DotNet.Cli.Build.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.MultilevelSharedFxLookup
{
    public partial class GivenThatICareAboutMultilevelSharedFxLookup : IDisposable
    {
        private const string SystemCollectionsImmutableFileVersion = "88.2.3.4";
        private const string SystemCollectionsImmutableAssemblyVersion = "88.0.1.2";

        private RepoDirectoriesProvider RepoDirectories;
        private TestProjectFixture PreviouslyBuiltAndRestoredPortableTestProjectFixture;

        private string _currentWorkingDir;
        private string _userDir;
        private string _exeDir;
        private string _regDir;
        private string _cwdSharedFxBaseDir;
        private string _cwdSharedUberFxBaseDir;
        private string _userSharedFxBaseDir;
        private string _userSharedUberFxBaseDir;
        private string _exeSharedFxBaseDir;
        private string _exeSharedUberFxBaseDir;
        private string _regSharedFxBaseDir;
        private string _regSharedUberFxBaseDir;
        private string _builtSharedFxDir;
        private string _builtSharedUberFxDir;

        private string _cwdSelectedMessage;
        private string _userSelectedMessage;
        private string _exeSelectedMessage;
        private string _regSelectedMessage;

        private string _cwdFoundUberFxMessage;
        private string _userFoundUberFxMessage;
        private string _exeFoundUberFxMessage;
        private string _regFoundUberFxMessage;

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
            _multilevelDir = SharedFramework.CalculateUniqueTestDirectory(baseMultilevelDir);

            // The three tested locations will be the cwd, the user folder and the exe dir. Both cwd and exe dir
            // are easily overwritten, so they will be placed inside the multilevel folder. The actual user location will
            // be used during tests
            _currentWorkingDir = Path.Combine(_multilevelDir, "cwd");
            _userDir = Path.Combine(_multilevelDir, "user");
            _exeDir = Path.Combine(_multilevelDir, "exe");
            _regDir = Path.Combine(_multilevelDir, "reg");

            RepoDirectories = new RepoDirectoriesProvider(builtDotnet: _exeDir);

            // SharedFxBaseDirs contain all available version folders
            _cwdSharedFxBaseDir = Path.Combine(_currentWorkingDir, "shared", "Microsoft.NETCore.App");
            _userSharedFxBaseDir = Path.Combine(_userDir, ".dotnet", RepoDirectories.BuildArchitecture, "shared", "Microsoft.NETCore.App");
            _exeSharedFxBaseDir = Path.Combine(_exeDir, "shared", "Microsoft.NETCore.App");
            _regSharedFxBaseDir = Path.Combine(_regDir, "shared", "Microsoft.NETCore.App");

            _cwdSharedUberFxBaseDir = Path.Combine(_currentWorkingDir, "shared", "Microsoft.UberFramework");
            _userSharedUberFxBaseDir = Path.Combine(_userDir, ".dotnet", RepoDirectories.BuildArchitecture, "shared", "Microsoft.UberFramework");
            _exeSharedUberFxBaseDir = Path.Combine(_exeDir, "shared", "Microsoft.UberFramework");
            _regSharedUberFxBaseDir = Path.Combine(_regDir, "shared", "Microsoft.UberFramework");

            // Create directories. It's necessary to copy the entire publish folder to the exe dir because
            // we'll need to build from it. The CopyDirectory method automatically creates the dest dir
            Directory.CreateDirectory(_cwdSharedFxBaseDir);
            Directory.CreateDirectory(_userSharedFxBaseDir);
            Directory.CreateDirectory(_regSharedFxBaseDir);
            Directory.CreateDirectory(_cwdSharedUberFxBaseDir);
            Directory.CreateDirectory(_userSharedUberFxBaseDir);
            Directory.CreateDirectory(_regSharedUberFxBaseDir);
            SharedFramework.CopyDirectory(_builtDotnet, _exeDir);

            //Copy dotnet to self-registered directory
            File.Copy(Path.Combine(_builtDotnet, $"dotnet{Constants.ExeSuffix}"), Path.Combine(_regDir, $"dotnet{Constants.ExeSuffix}"), true);

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
            _builtSharedUberFxDir = Path.Combine(_builtDotnet, "shared", "Microsoft.UberFramework", _sharedFxVersion);
            SharedFramework.CreateUberFrameworkArtifacts(_builtSharedFxDir, _builtSharedUberFxDir, SystemCollectionsImmutableAssemblyVersion, SystemCollectionsImmutableFileVersion);

            // Trace messages used to identify from which folder the framework was picked
            _hostPolicyDllName = Path.GetFileName(fixture.TestProject.HostPolicyDll);
            _cwdSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_cwdSharedFxBaseDir}";
            _userSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_userSharedFxBaseDir}";
            _exeSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_exeSharedFxBaseDir}";
            _regSelectedMessage = $"The expected {_hostPolicyDllName} directory is [{_regSharedFxBaseDir}";

            _cwdFoundUberFxMessage = $"Chose FX version [{_cwdSharedUberFxBaseDir}";
            _userFoundUberFxMessage = $"Chose FX version [{_userSharedUberFxBaseDir}";
            _exeFoundUberFxMessage = $"Chose FX version [{_exeSharedUberFxBaseDir}";
            _regFoundUberFxMessage = $"Chose FX version [{_regSharedUberFxBaseDir}";
        }

        public void Dispose()
        {
            PreviouslyBuiltAndRestoredPortableTestProjectFixture.Dispose();

            if (!TestProject.PreserveTestRuns())
            {
                Directory.Delete(_multilevelDir, true);
            }
        }

        [Fact]
        public void SharedMultilevelFxLookup_Must_Verify_Folders_in_the_Correct_Order()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Multi-level lookup is only supported on Windows.
                return;
            }

            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Set desired version = 9999.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "9999.0.0");

            // Add version in the reg dir
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _regSharedFxBaseDir, "9999.0.0");

            // Version: 9999.0.0
            // Cwd: empty
            // User: empty
            // Exe: empty
            // Reg: 9999.0.0
            // Expected: 9999.0.0 from reg dir
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.0"));

            // Add a dummy version in the user dir
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _userSharedFxBaseDir, "9999.0.0");

            // Version: 9999.0.0
            // Cwd: empty
            // User: 9999.0.0 --> should not be picked
            // Exe: empty
            // Reg: 9999.0.0
            // Expected: 9999.0.0 from reg dir
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.0"));

            // Add a dummy version in the cwd dir
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _cwdSharedFxBaseDir, "9999.0.0");

            // Version: 9999.0.0
            // Cwd: 9999.0.0    --> should not be picked
            // User: 9999.0.0   --> should not be picked
            // Exe: empty
            // Reg: 9999.0.0
            // Expected: 9999.0.0 from reg dir
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.0"));

            // Add version in the exe dir
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _exeSharedFxBaseDir, "9999.0.0");

            // Version: 9999.0.0
            // Cwd: 9999.0.0    --> should not be picked
            // User: 9999.0.0   --> should not be picked
            // Exe: 9999.0.0
            // Reg: 9999.0.0
            // Expected: 9999.0.0 from exe dir
            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.0"));

            // Verify we have the expected runtime versions
            dotnet.Exec("--list-runtimes")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App 9999.0.0");
        }

        [Fact]
        public void SharedMultilevelFxLookup_Must_Not_Roll_Forward_If_Framework_Version_Is_Specified_Through_Argument()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Multi-level lookup is only supported on Windows.
                return;
            }

            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            // Add some dummy versions
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _exeSharedFxBaseDir, "9999.0.0", "9999.0.1", "9999.0.0-dummy2", "9999.0.4");
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _regSharedFxBaseDir, "9999.0.0", "9999.0.2", "9999.0.3", "9999.0.0-dummy3");

            // Version: 9999.0.0 (through --fx-version arg)
            // Cwd: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.1, 9999.0.4, 9999.0.0-dummy2
            // Reg: 9999.0.0, 9999.0.2, 9999.0.3, 9999.0.0-dummy3
            // Expected: 9999.0.1 from exe dir
            dotnet.Exec("--fx-version", "9999.0.1", appDll)
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.1"));

            // Version: 9999.0.0-dummy1 (through --fx-version arg)
            // Cwd: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.1, 9999.0.4, 9999.0.0-dummy2
            // Reg: 9999.0.0, 9999.0.2, 9999.0.3, 9999.0.0-dummy3
            // Expected: no compatible version
            dotnet.Exec("--fx-version", "9999.0.0-dummy1", appDll)
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("It was not possible to find any compatible framework version");

            // Version: 9999.0.0 (through --fx-version arg)
            // Cwd: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.1, 9999.0.4, 9999.0.0-dummy2
            // Reg: 9999.0.0, 9999.0.2, 9999.0.3, 9999.0.0-dummy3
            // Expected: 9999.0.2 from reg dir
            dotnet.Exec("--fx-version", "9999.0.2", appDll)
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.2"));

            // Version: 9999.0.0 (through --fx-version arg)
            // Cwd: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.1, 9999.0.4, 9999.0.0-dummy2
            // Reg: 9999.0.0, 9999.0.2, 9999.0.3, 9999.0.0-dummy3
            // Expected: 9999.0.0 from exe dir
            dotnet.Exec("--fx-version", "9999.0.0", appDll)
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.0"));

            // Verify we have the expected runtime versions
            dotnet.Exec("--list-runtimes")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
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

        static private string CreateAStore(TestProjectFixture testProjectFixture)
        {
            var storeoutputDirectory = Path.Combine(testProjectFixture.TestProject.ProjectDirectory, "store");
            if (!Directory.Exists(storeoutputDirectory))
            {
                Directory.CreateDirectory(storeoutputDirectory);
            }

            testProjectFixture.StoreProject(outputDirectory: storeoutputDirectory);

            return storeoutputDirectory;
        }
    }
}
