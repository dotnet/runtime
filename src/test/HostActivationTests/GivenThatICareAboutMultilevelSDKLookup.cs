using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.MultilevelSDKLookup
{
    public class GivenThatICareAboutMultilevelSDKLookup : IDisposable
    {
        private static IDictionary<string, string> s_DefaultEnvironment = new Dictionary<string, string>()
        {
            {"COREHOST_TRACE", "1" },
            // The SDK being used may be crossgen'd for a different architecture than we are building for.
            // Turn off ready to run, so an x64 crossgen'd SDK can be loaded in an x86 process.
            {"COMPlus_ReadyToRun", "0" },
        };

        private RepoDirectoriesProvider RepoDirectories;
        private TestProjectFixture PreviouslyBuiltAndRestoredPortableTestProjectFixture;

        private string _currentWorkingDir;
        private string _userDir;
        private string _exeDir;
        private string _regDir;
        private string _cwdSdkBaseDir;
        private string _userSdkBaseDir;
        private string _exeSdkBaseDir;
        private string _regSdkBaseDir;
        private string _cwdSelectedMessage;
        private string _userSelectedMessage;
        private string _exeSelectedMessage;
        private string _regSelectedMessage;
        private string _sdkDir;
        private string _multilevelDir;

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
            _multilevelDir = SharedFramework.CalculateUniqueTestDirectory(baseMultilevelDir);

            // The three tested locations will be the cwd, the user folder and the exe dir. cwd and user are no longer supported.
            //     All dirs will be placed inside the multilevel folder

            _currentWorkingDir = Path.Combine(_multilevelDir, "cwd");
            _userDir = Path.Combine(_multilevelDir, "user");
            _exeDir = Path.Combine(_multilevelDir, "exe");
            _regDir = Path.Combine(_multilevelDir, "reg");

            // It's necessary to copy the entire publish folder to the exe dir because
            // we'll need to build from it. The CopyDirectory method automatically creates the dest dir
            SharedFramework.CopyDirectory(builtDotnet, _exeDir);

            RepoDirectories = new RepoDirectoriesProvider(builtDotnet: _exeDir);

            // SdkBaseDirs contain all available version folders
            _cwdSdkBaseDir = Path.Combine(_currentWorkingDir, "sdk");
            _userSdkBaseDir = Path.Combine(_userDir, ".dotnet", RepoDirectories.BuildArchitecture, "sdk");
            _exeSdkBaseDir = Path.Combine(_exeDir, "sdk");
            _regSdkBaseDir = Path.Combine(_regDir, "sdk");

            // Create directories
            Directory.CreateDirectory(_cwdSdkBaseDir);
            Directory.CreateDirectory(_userSdkBaseDir);
            Directory.CreateDirectory(_exeSdkBaseDir);
            Directory.CreateDirectory(_regSdkBaseDir);

            // Restore and build PortableApp from exe dir
            PreviouslyBuiltAndRestoredPortableTestProjectFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .BuildProject();
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture;

            // Set a dummy framework version (9999.0.0) in the exe sharedFx location. We will
            // always pick the framework from this to avoid interference with the sharedFxLookup
            string exeDirDummyFxVersion = Path.Combine(_exeDir, "shared", "Microsoft.NETCore.App", "9999.0.0");
            string builtSharedFxDir = fixture.BuiltDotnet.GreatestVersionSharedFxPath;
            SharedFramework.CopyDirectory(builtSharedFxDir, exeDirDummyFxVersion);

            // The actual SDK version can be obtained from the built fixture. We'll use it to
            // locate the sdkDir from which we can get the files contained in the version folder
            string sdkBaseDir = Path.Combine(fixture.SdkDotnet.BinPath, "sdk");

            var sdkVersionDirs = Directory.EnumerateDirectories(sdkBaseDir)
                .Select(p => Path.GetFileName(p));

            string greatestVersionSdk = sdkVersionDirs
                .Where(p => !string.Equals(p, "NuGetFallbackFolder", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.ToLower())
                .First();

            _sdkDir = Path.Combine(sdkBaseDir, greatestVersionSdk);

            // Trace messages used to identify from which folder the SDK was picked
            _cwdSelectedMessage = $"Using dotnet SDK dll=[{_cwdSdkBaseDir}";
            _userSelectedMessage = $"Using dotnet SDK dll=[{_userSdkBaseDir}";
            _exeSelectedMessage = $"Using dotnet SDK dll=[{_exeSdkBaseDir}";
            _regSelectedMessage = $"Using dotnet SDK dll=[{_regSdkBaseDir}";
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
        public void SdkMultilevelLookup_Global_Json_Single_Digit_Patch_Rollup()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Multi-level lookup is only supported on Windows.
                return;
            }

            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Set specified SDK version = 9999.3.4-global-dummy
            SetGlobalJsonVersion("SingleDigit-global.json");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: empty
            // Reg: empty
            // Expected: no compatible version and a specific error messages
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("A compatible installed dotnet SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.4.1", "9999.3.4-dummy");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy
            // Reg: empty
            // Expected: no compatible version and a specific error message
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                 .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("A compatible installed dotnet SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.3");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy
            // Reg: 9999.3.3
            // Expected: no compatible version and a specific error message
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("A compatible installed dotnet SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.4");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.4
            // Reg: 9999.3.3
            // Expected: 9999.3.4 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.4", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.5-dummy");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.4
            // Reg: 9999.3.3, 9999.3.5-dummy
            // Expected: 9999.3.5-dummy from reg dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.3.5-dummy", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.600");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.4, 9999.3.600
            // Reg: 9999.3.3, 9999.3.5-dummy
            // Expected: 9999.3.5-dummy from reg dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.3.5-dummy", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.4-global-dummy");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.4, 9999.3.600, 9999.3.4-global-dummy
            // Reg: 9999.3.3, 9999.3.5-dummy
            // Expected: 9999.3.4-global-dummy from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.4-global-dummy", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected SDK versions
            dotnet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("9999.3.4-dummy")
                .And
                .HaveStdOutContaining("9999.3.4-global-dummy")
                .And
                .HaveStdOutContaining("9999.4.1")
                .And
                .HaveStdOutContaining("9999.3.3")
                .And
                .HaveStdOutContaining("9999.3.4")
                .And
                .HaveStdOutContaining("9999.3.600")
                .And
                .HaveStdOutContaining("9999.3.5-dummy");
        }

        [Fact]
        public void SdkMultilevelLookup_Global_Json_Two_Part_Patch_Rollup()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Multi-level lookup is only supported on Windows.
                return;
            }

            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Set specified SDK version = 9999.3.304-global-dummy
            SetGlobalJsonVersion("TwoPart-global.json");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: empty
            // Reg: empty
            // Expected: no compatible version and a specific error messages
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("A compatible installed dotnet SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.57", "9999.3.4-dummy");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: empty
            // Reg: 9999.3.57, 9999.3.4-dummy
            // Expected: no compatible version and a specific error message
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("A compatible installed dotnet SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.300", "9999.7.304-global-dummy");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.3.300, 9999.7.304-global-dummy
            // Reg: 9999.3.57, 9999.3.4-dummy
            // Expected: no compatible version and a specific error message
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("A compatible installed dotnet SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.304");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.3.300, 9999.7.304-global-dummy
            // Reg: 9999.3.57, 9999.3.4-dummy, 9999.3.304
            // Expected: 9999.3.304 from reg dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.3.304", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.399", "9999.3.399-dummy", "9999.3.400");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.3.300, 9999.7.304-global-dummy, 9999.3.399, 9999.3.399-dummy, 9999.3.400
            // Reg: 9999.3.57, 9999.3.4-dummy, 9999.3.304
            // Expected: 9999.3.399 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.399", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.2400, 9999.3.3004");
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.2400, 9999.3.3004");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.3.300, 9999.7.304-global-dummy, 9999.3.399, 9999.3.399-dummy, 9999.3.400, 9999.3.2400, 9999.3.3004
            // Reg: 9999.3.57, 9999.3.4-dummy, 9999.3.304, 9999.3.2400, 9999.3.3004
            // Expected: 9999.3.399 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.399", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.304-global-dummy");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // User: empty
            // Exe: 9999.3.300, 9999.7.304-global-dummy, 9999.3.399, 9999.3.399-dummy, 9999.3.400, 9999.3.2400, 9999.3.3004
            // Reg: 9999.3.57, 9999.3.4-dummy, 9999.3.304, 9999.3.2400, 9999.3.3004, 9999.3.304-global-dummy
            // Expected: 9999.3.304-global-dummy from reg dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.3.304-global-dummy", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected SDK versions
            dotnet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("9999.3.57")
                .And
                .HaveStdOutContaining("9999.3.4-dummy")
                .And
                .HaveStdOutContaining("9999.3.300")
                .And
                .HaveStdOutContaining("9999.7.304-global-dummy")
                .And
                .HaveStdOutContaining("9999.3.399")
                .And
                .HaveStdOutContaining("9999.3.399-dummy")
                .And
                .HaveStdOutContaining("9999.3.400")
                .And
                .HaveStdOutContaining("9999.3.2400")
                .And
                .HaveStdOutContaining("9999.3.3004")
                .And
                .HaveStdOutContaining("9999.3.304")
                .And
                .HaveStdOutContaining("9999.3.304-global-dummy");
        }

        [Fact]
        public void SdkMultilevelLookup_Precedential_Order()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Multi-level lookup is only supported on Windows.
                return;
            }

            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.0.4");

            // Specified SDK version: none
            // Cwd: empty
            // User: empty
            // Exe: empty
            // Reg: 9999.0.4
            // Expected: 9999.0.4 from reg dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.4", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.4");

            // Specified SDK version: none
            // Cwd: empty
            // User: empty
            // Exe: 9999.0.4
            // Reg: 9999.0.4
            // Expected: 9999.0.4 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.4", _dotnetSdkDllMessageTerminator));
        }

        [Fact]
        public void SdkMultilevelLookup_RegistryAccess()
        {
            // The purpose of this test is to verify that the product uses correct code to access
            // the registry to extract the path to search for SDKs.
            // Most of our tests rely on a shortcut which is to set _DOTNET_TEST_SDK_SELF_REGISTERED_DIR env variable
            // which will skip the registry reading code in the product and simply use the specified value.
            // This test is different since it actually runs the registry reading code.
            // Normally the reg key the product uses is in HKEY_LOCAL_MACHINE which is only writable as admin
            // so we would require the tests to run as admin to modify that key (and it may introduce races with other code running on the machine).
            // So instead the tests use _DOTENT_TEST_SDK_REGISTRY_PATH env variable to point to the produce to use
            // different registry key, inside the HKEY_CURRENT_USER hive which is writable without admin.
            // Note that the test creates a unique key (based on PID) for every run, to avoid collisions between parallel running tests.

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Multi-level lookup is only supported on Windows.
                return;
            }

            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // To correctly test the product we need a registry key which is
            // - writable without admin access (so that the tests don't require admin to run)
            // - redirected in WOW64 - so that there are both 32bit and 64bit versions of the key
            //   this is because the product stores the info in the 32bit version only and even 64bit
            //   product must look into the 32bit version.
            //   Without the redirection we would not be able to test that the product always looks
            //   into 32bit only.
            // Per this page https://docs.microsoft.com/en-us/windows/desktop/WinProg64/shared-registry-keys
            // a user writable redirected key is for example HKCU\Software\Classes\Interface
            // so we're going to use that one - it's not super clean as they key stored COM interfaces
            // but we should not corrupt anything by adding a special subkey even if it's left behind.
            //
            // Note: If you want to inspect the values written by the test and/or modify them manually
            //   you have to navigate to HKCU\Software\Classes\Wow6432Node\Interface on a 64bit OS.

            RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            RegistryKey interfaceKey = hkcu.CreateSubKey(@"Software\Classes\Interface");
            string testKeyName = "_DOTNET_Test" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
            RegistryKey testKey = interfaceKey.CreateSubKey(testKeyName);
            try
            {
                string architecture = fixture.CurrentRid.Split('-')[1];
                RegistryKey sdkKey = testKey.CreateSubKey($@"Setup\InstalledVersions\{architecture}\sdk");
                sdkKey.SetValue("InstallLocation", _regDir);

                // Add SDK versions
                AddAvailableSdkVersions(_regSdkBaseDir, "9999.0.4");

                // Specified SDK version: none
                // Cwd: empty
                // User: empty
                // Exe: empty
                // Reg: 9999.0.4
                // Expected: 9999.0.4 from reg dir
                dotnet.Exec("help")
                    .WorkingDirectory(_currentWorkingDir)
                    .WithUserProfile(_userDir)
                    .Environment(s_DefaultEnvironment)
                    .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                    .EnvironmentVariable("_DOTNET_TEST_SDK_REGISTRY_PATH", testKey.Name)
                    .CaptureStdOut()
                    .CaptureStdErr()
                    .Execute()
                    .Should()
                    .Pass()
                    .And
                    .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.4", _dotnetSdkDllMessageTerminator));
            }
            finally
            {
                interfaceKey.DeleteSubKeyTree(testKeyName);
            }
        }

        [Fact]
        public void SdkMultilevelLookup_Must_Pick_The_Highest_Semantic_Version()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Multi-level lookup is only supported on Windows.
                return;
            }

            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.0.0", "9999.0.3-dummy");

            // Specified SDK version: none
            // Cwd: empty
            // User: empty
            // Exe: empty
            // Reg: 9999.0.0, 9999.0.3-dummy
            // Expected: 9999.0.3-dummy from reg dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.3-dummy", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.3");

            // Specified SDK version: none
            // Cwd: empty
            // User: empty
            // Exe: 9999.0.3
            // Reg: 9999.0.0, 9999.0.3-dummy
            // Expected: 9999.0.3 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.3", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_userSdkBaseDir, "9999.0.200");
            AddAvailableSdkVersions(_cwdSdkBaseDir, "10000.0.0");
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.0.100");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.3
            // Reg: 9999.0.0, 9999.0.3-dummy, 9999.0.100
            // Expected: 9999.0.100 from reg dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.100", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.80");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.3, 9999.0.80
            // Reg: 9999.0.0, 9999.0.3-dummy, 9999.0.100
            // Expected: 9999.0.100 from reg dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.100", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.5500000");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.3, 9999.0.80, 9999.0.5500000
            // Reg: 9999.0.0, 9999.0.3-dummy, 9999.0.100
            // Expected: 9999.0.5500000 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.5500000", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.0.52000000");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.3, 9999.0.80, 9999.0.5500000
            // Reg: 9999.0.0, 9999.0.3-dummy, 9999.0.100, 9999.0.52000000
            // Expected: 9999.0.52000000 from reg dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.52000000", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected SDK versions
            dotnet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1")
                .EnvironmentVariable("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR", _regDir)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("9999.0.0")
                .And
                .HaveStdOutContaining("9999.0.3-dummy")
                .And
                .HaveStdOutContaining("9999.0.3")
                .And
                .HaveStdOutContaining("9999.0.100")
                .And
                .HaveStdOutContaining("9999.0.80")
                .And
                .HaveStdOutContaining("9999.0.5500000")
                .And
                .HaveStdOutContaining("9999.0.52000000");
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
                SharedFramework.CopyDirectory(_sdkDir, newSdkDir);

                string runtimeConfig = Path.Combine(newSdkDir, "dotnet.runtimeconfig.json");
                File.Copy(dummyRuntimeConfig, runtimeConfig, true);
            }
        }

        // Put a global.json file in the cwd in order to specify a CLI
        public void SetGlobalJsonVersion(string globalJsonFileName)
        {
            string destFile = Path.Combine(_currentWorkingDir, "global.json");
            string srcFile = Path.Combine(RepoDirectories.RepoRoot, "src", "test", "Assets", "TestUtils",
                "SDKLookup", globalJsonFileName);

            File.Copy(srcFile, destFile, true);
        }
    }
}
