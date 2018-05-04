using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.DotNet.InternalAbstractions;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.MultilevelSDKLookup
{
    public class GivenThatICareAboutMultilevelSDKLookup
    {
        private static readonly Mutex id_mutex = new Mutex();
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

            // The three tested locations will be the cwd, the user folder and the exe dir. cwd and user are no longer supported.
            //     All dirs will be placed inside the multilevel folder

            _currentWorkingDir = Path.Combine(multilevelDir, "cwd");
            _userDir = Path.Combine(multilevelDir, "user");
            _executableDir = Path.Combine(multilevelDir, "exe");

            // It's necessary to copy the entire publish folder to the exe dir because
            // we'll need to build from it. The CopyDirectory method automatically creates the dest dir
            CopyDirectory(builtDotnet, _executableDir);

            RepoDirectories = new RepoDirectoriesProvider(builtDotnet: _executableDir);

            // SdkBaseDirs contain all available version folders
            _cwdSdkBaseDir = Path.Combine(_currentWorkingDir, "sdk");
            _userSdkBaseDir = Path.Combine(_userDir, ".dotnet", RepoDirectories.BuildArchitecture, "sdk");
            _exeSdkBaseDir = Path.Combine(_executableDir, "sdk");

            // Create directories
            Directory.CreateDirectory(_cwdSdkBaseDir);
            Directory.CreateDirectory(_userSdkBaseDir);
            Directory.CreateDirectory(_exeSdkBaseDir);

            // Restore and build PortableApp from exe dir
            PreviouslyBuiltAndRestoredPortableTestProjectFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .BuildProject();
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture;

            // Set a dummy framework version (9999.0.0) in the exe sharedFx location. We will
            // always pick the framework from this to avoid interference with the sharedFxLookup
            string exeDirDummyFxVersion = Path.Combine(_executableDir, "shared", "Microsoft.NETCore.App", "9999.0.0");
            string builtSharedFxDir = fixture.BuiltDotnet.GreatestVersionSharedFxPath;
            CopyDirectory(builtSharedFxDir, exeDirDummyFxVersion);

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
        }
        
        [Fact]
        public void SdkLookup_Global_Json_Single_Digit_Patch_Rollup()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Set specified CLI version = 9999.3.4-global-dummy
            SetGlobalJsonVersion("SingleDigit-global.json");

            // Add some dummy versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.4.1", "9999.3.4-dummy");

            // Specified CLI version: 9999.3.4-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy
            // Expected: no compatible version and a specific error message
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("A compatible SDK version for global.json version");

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.3");

            // Specified CLI version: 9999.3.4-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.3
            // Expected: no compatible version and a specific error message
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("A compatible SDK version for global.json version");

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.4");

            // Specified CLI version: 9999.3.4-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.3, 9999.3.4
            // Expected: 9999.3.4 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.4", _dotnetSdkDllMessageTerminator));

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.5-dummy");

            // Specified CLI version: 9999.3.4-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.3, 9999.3.4, 9999.3.5-dummy
            // Expected: 9999.3.5-dummy from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.5-dummy", _dotnetSdkDllMessageTerminator));

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.600");

            // Specified CLI version: 9999.3.4-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.3, 9999.3.4, 9999.3.5-dummy, 9999.3.600
            // Expected: 9999.3.5-dummy from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.5-dummy", _dotnetSdkDllMessageTerminator));

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.4-global-dummy");

            // Specified CLI version: 9999.3.4-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.3, 9999.3.4, 9999.3.5-dummy, 9999.3.600, 9999.3.4-global-dummy
            // Expected: 9999.3.4-global-dummy from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.4-global-dummy", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected sdk versions
            dotnet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
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
        public void SdkLookup_Global_Json_Two_Part_Patch_Rollup()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Set specified CLI version = 9999.3.304-global-dummy
            SetGlobalJsonVersion("TwoPart-global.json");

            // Add some dummy versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.57", "9999.3.4-dummy");

            // Specified CLI version: 9999.3.304-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.3.57, 9999.3.4-dummy
            // Expected: no compatible version and a specific error message
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("A compatible SDK version for global.json version");

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.300", "9999.7.304-global-dummy");

            // Specified CLI version: 9999.3.304-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.3.57, 9999.3.4-dummy, 9999.3.300, 9999.7.304-global-dummy
            // Expected: no compatible version and a specific error message
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("A compatible SDK version for global.json version");

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.304");

            // Specified CLI version: 9999.3.304-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 99999.3.57, 9999.3.4-dummy, 9999.3.300, 9999.7.304-global-dummy, 9999.3.304
            // Expected: 9999.3.304 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.304", _dotnetSdkDllMessageTerminator));

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.399", "9999.3.399-dummy", "9999.3.400");

            // Specified CLI version: 9999.3.304-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.3.57, 9999.3.4-dummy, 9999.3.300, 9999.7.304-global-dummy, 9999.3.304, 9999.3.399, 9999.3.399-dummy, 9999.3.400
            // Expected: 9999.3.399 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.399", _dotnetSdkDllMessageTerminator));

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.2400, 9999.3.3004");

            // Specified CLI version: 9999.3.304-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.3.57, 9999.3.4-dummy, 9999.3.300, 9999.7.304-global-dummy, 9999.3.304, 9999.3.399, 9999.3.399-dummy, 9999.3.400, 9999.3.2400, 9999.3.3004
            // Expected: 9999.3.399 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.399", _dotnetSdkDllMessageTerminator));

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.304-global-dummy");

            // Specified CLI version: 9999.3.304-global-dummy
            // CWD: empty
            // User: empty
            // Exe: 9999.3.57, 9999.3.4-dummy, 9999.3.300, 9999.7.304-global-dummy, 9999.3.304, 9999.3.399, 9999.3.399-dummy, 9999.3.400, 9999.3.2400, 9999.3.3004, 9999.3.304-global-dummy
            // Expected: 9999.3.304-global-dummy from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.304-global-dummy", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected sdk versions
            dotnet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
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
        public void SdkLookup_Negative_Version()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Add a negative CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "-1.-1.-1");

            // Specified CLI version: none
            // CWD: empty
            // User: empty
            // Exe: -1.-1.-1
            // Expected: no compatible version and a specific error message
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("It was not possible to find any SDK version");

            // Add specified CLI version
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.4");

            // Specified CLI version: none
            // CWD: empty
            // User: empty
            // Exe: -1.-1.-1, 9999.0.4
            // Expected: 9999.0.4 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.4", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected sdk versions
            dotnet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("9999.0.4");
        }

        [Fact]
        public void SdkLookup_Must_Pick_The_Highest_Semantic_Version()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Add dummy versions in the exe dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.0", "9999.0.3-dummy");

            // Specified CLI version: none
            // CWD: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.3-dummy
            // Expected: 9999.0.3-dummy from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.3-dummy", _dotnetSdkDllMessageTerminator));

            // Add dummy versions in the exe dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.3");

            // Specified CLI version: none
            // CWD: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.3-dummy, 9999.0.3
            // Expected: 9999.0.3 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.3", _dotnetSdkDllMessageTerminator));

            // Add dummy versions
            AddAvailableSdkVersions(_userSdkBaseDir, "9999.0.200");
            AddAvailableSdkVersions(_cwdSdkBaseDir, "10000.0.0");
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.100");

            // Specified CLI version: none
            // CWD: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.0, 9999.0.3-dummy, 9999.0.3, 9999.0.100
            // Expected: 9999.0.100 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.100", _dotnetSdkDllMessageTerminator));

            // Add a dummy version in the exe dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.80");

            // Specified CLI version: none
            // CWD: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.0, 9999.0.3-dummy, 9999.0.3, 9999.0.100, 9999.0.80
            // Expected: 9999.0.100 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.100", _dotnetSdkDllMessageTerminator));

            // Add a dummy version in the user dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.5500000");

            // Specified CLI version: none
            // CWD: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.0, 9999.0.3-dummy, 9999.0.3, 9999.0.100, 9999.0.80, 9999.0.5500000
            // Expected: 9999.0.5500000 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.5500000", _dotnetSdkDllMessageTerminator));

            // Add a dummy version in the user dir
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.52000000");

            // Specified CLI version: none
            // CWD: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.0, 9999.0.3-dummy, 9999.0.3, 9999.0.100, 9999.0.80, 9999.0.5500000, 9999.0.52000000
            // Expected: 9999.0.5500000 from exe dir
            dotnet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.52000000", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected sdk versions
            dotnet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
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
        public void SetGlobalJsonVersion(string globalJsonFileName)
        {
            string destFile = Path.Combine(_currentWorkingDir, "global.json");
            string srcFile = Path.Combine(RepoDirectories.RepoRoot, "src", "test", "Assets", "TestUtils",
                "SDKLookup", globalJsonFileName);

            File.Copy(srcFile, destFile, true);
        }

        // MultilevelDirectory is %TEST_ARTIFACTS%\dotnetMultilevelSDKLookup\id.
        // We must locate the first non existing id.
        private string CalculateMultilevelDirectory(string baseMultilevelDir)
        {
            id_mutex.WaitOne();

            int count = 0;
            string multilevelDir;

            do
            {
                multilevelDir = Path.Combine(baseMultilevelDir, count.ToString());
                count++;
            } while (Directory.Exists(multilevelDir));

            id_mutex.ReleaseMutex();
        
            return multilevelDir;
        }
    }
}
