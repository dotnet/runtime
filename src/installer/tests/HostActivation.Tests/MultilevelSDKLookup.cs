// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class MultilevelSDKLookup : IDisposable
    {
        private readonly RepoDirectoriesProvider RepoDirectories;
        private readonly DotNetCli DotNet;

        private readonly string _currentWorkingDir;
        private readonly string _exeDir;
        private readonly string _regDir;
        private readonly string _cwdSdkBaseDir;
        private readonly string _exeSdkBaseDir;
        private readonly string _regSdkBaseDir;
        private readonly string _exeSelectedMessage;
        private readonly string _regSelectedMessage;
        private readonly string _multilevelDir;

        private const string _dotnetSdkDllMessageTerminator = "dotnet.dll]";

        private readonly IDisposable _testOnlyProductBehaviorMarker;

        public MultilevelSDKLookup()
        {
            // The dotnetMultilevelSDKLookup dir will contain some folders and files that will be
            // necessary to perform the tests
            string baseMultilevelDir = Path.Combine(TestArtifact.TestArtifactsPath, "dotnetMultilevelSDKLookup");
            _multilevelDir = SharedFramework.CalculateUniqueTestDirectory(baseMultilevelDir);

            // The tested locations will be the cwd, exe dir, and registered directory. cwd is no longer supported.
            //     All dirs will be placed inside the multilevel folder
            _currentWorkingDir = Path.Combine(_multilevelDir, "cwd");
            _exeDir = Path.Combine(_multilevelDir, "exe");
            _regDir = Path.Combine(_multilevelDir, "reg");

            DotNet = new DotNetBuilder(_multilevelDir, Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"), "exe")
                .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("9999.0.0")
                .Build();

            RepoDirectories = new RepoDirectoriesProvider(builtDotnet: DotNet.BinPath);

            // SdkBaseDirs contain all available version folders
            _cwdSdkBaseDir = Path.Combine(_currentWorkingDir, "sdk");
            _exeSdkBaseDir = Path.Combine(_exeDir, "sdk");
            _regSdkBaseDir = Path.Combine(_regDir, "sdk");

            // Create directories
            Directory.CreateDirectory(_cwdSdkBaseDir);
            Directory.CreateDirectory(_exeSdkBaseDir);
            Directory.CreateDirectory(_regSdkBaseDir);

            // Trace messages used to identify from which folder the SDK was picked
            _exeSelectedMessage = $"Using .NET SDK dll=[{_exeSdkBaseDir}";
            _regSelectedMessage = $"Using .NET SDK dll=[{_regSdkBaseDir}";

            _testOnlyProductBehaviorMarker = TestOnlyProductBehavior.Enable(DotNet.GreatestVersionHostFxrFilePath);
        }

        public void Dispose()
        {
            _testOnlyProductBehaviorMarker?.Dispose();

            if (!TestArtifact.PreserveTestRuns())
            {
                Directory.Delete(_multilevelDir, true);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Multi-level lookup is only supported on Windows.
        public void SdkMultilevelLookup_Global_Json_Single_Digit_Patch_Rollup()
        {
            // Set specified SDK version = 9999.3.4-global-dummy
            SetGlobalJsonVersion("SingleDigit-global.json");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // Exe: empty
            // Reg: empty
            // Expected: no compatible version and a specific error messages
            RunTest()
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.4.1", "9999.3.4-dummy");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // Exe: 9999.4.1, 9999.3.4-dummy
            // Reg: empty
            // Expected: no compatible version and a specific error message
            RunTest()
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.3");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // Exe: 9999.4.1, 9999.3.4-dummy
            // Reg: 9999.3.3
            // Expected: no compatible version and a specific error message
            RunTest()
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.4");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.4
            // Reg: 9999.3.3
            // Expected: 9999.3.4 from exe dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.4", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.5-dummy");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.4
            // Reg: 9999.3.3, 9999.3.5-dummy
            // Expected: 9999.3.5-dummy from reg dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.3.5-dummy", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.600");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.4, 9999.3.600
            // Reg: 9999.3.3, 9999.3.5-dummy
            // Expected: 9999.3.5-dummy from reg dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.3.5-dummy", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.4-global-dummy");

            // Specified SDK version: 9999.3.4-global-dummy
            // Cwd: empty
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.4, 9999.3.600, 9999.3.4-global-dummy
            // Reg: 9999.3.3, 9999.3.5-dummy
            // Expected: 9999.3.4-global-dummy from exe dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.4-global-dummy", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected SDK versions
            RunTest("--list-sdks")
                .Should().Pass()
                .And.HaveStdOutContaining("9999.3.4-dummy")
                .And.HaveStdOutContaining("9999.3.4-global-dummy")
                .And.HaveStdOutContaining("9999.4.1")
                .And.HaveStdOutContaining("9999.3.3")
                .And.HaveStdOutContaining("9999.3.4")
                .And.HaveStdOutContaining("9999.3.600")
                .And.HaveStdOutContaining("9999.3.5-dummy");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Multi-level lookup is only supported on Windows.
        public void SdkMultilevelLookup_Global_Json_Two_Part_Patch_Rollup()
        {
            // Set specified SDK version = 9999.3.304-global-dummy
            SetGlobalJsonVersion("TwoPart-global.json");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // Exe: empty
            // Reg: empty
            // Expected: no compatible version and a specific error messages
            RunTest()
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.57", "9999.3.4-dummy");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // Exe: empty
            // Reg: 9999.3.57, 9999.3.4-dummy
            // Expected: no compatible version and a specific error message
            RunTest()
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.300", "9999.7.304-global-dummy");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // Exe: 9999.3.300, 9999.7.304-global-dummy
            // Reg: 9999.3.57, 9999.3.4-dummy
            // Expected: no compatible version and a specific error message
            RunTest()
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version");

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.304");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // Exe: 9999.3.300, 9999.7.304-global-dummy
            // Reg: 9999.3.57, 9999.3.4-dummy, 9999.3.304
            // Expected: 9999.3.304 from reg dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.3.304", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.399", "9999.3.399-dummy", "9999.3.400");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // Exe: 9999.3.300, 9999.7.304-global-dummy, 9999.3.399, 9999.3.399-dummy, 9999.3.400
            // Reg: 9999.3.57, 9999.3.4-dummy, 9999.3.304
            // Expected: 9999.3.399 from exe dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.399", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.2400", "9999.3.3004");
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.2400", "9999.3.3004");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // Exe: 9999.3.300, 9999.7.304-global-dummy, 9999.3.399, 9999.3.399-dummy, 9999.3.400, 9999.3.2400, 9999.3.3004
            // Reg: 9999.3.57, 9999.3.4-dummy, 9999.3.304, 9999.3.2400, 9999.3.3004
            // Expected: 9999.3.399 from exe dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.399", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.3.304-global-dummy");

            // Specified SDK version: 9999.3.304-global-dummy
            // Cwd: empty
            // Exe: 9999.3.300, 9999.7.304-global-dummy, 9999.3.399, 9999.3.399-dummy, 9999.3.400, 9999.3.2400, 9999.3.3004
            // Reg: 9999.3.57, 9999.3.4-dummy, 9999.3.304, 9999.3.2400, 9999.3.3004, 9999.3.304-global-dummy
            // Expected: 9999.3.304-global-dummy from reg dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.3.304-global-dummy", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected SDK versions
            RunTest("--list-sdks")
                .Should().Pass()
                .And.HaveStdOutContaining("9999.3.57")
                .And.HaveStdOutContaining("9999.3.4-dummy")
                .And.HaveStdOutContaining("9999.3.300")
                .And.HaveStdOutContaining("9999.7.304-global-dummy")
                .And.HaveStdOutContaining("9999.3.399")
                .And.HaveStdOutContaining("9999.3.399-dummy")
                .And.HaveStdOutContaining("9999.3.400")
                .And.HaveStdOutContaining("9999.3.2400")
                .And.HaveStdOutContaining("9999.3.3004")
                .And.HaveStdOutContaining("9999.3.304")
                .And.HaveStdOutContaining("9999.3.304-global-dummy");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Multi-level lookup is only supported on Windows.
        public void SdkMultilevelLookup_Precedential_Order()
        {
            WriteEmptyGlobalJson();

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.0.4");

            // Specified SDK version: none
            // Cwd: empty
            // Exe: empty
            // Reg: 9999.0.4
            // Expected: 9999.0.4 from reg dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.4", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.4");

            // Specified SDK version: none
            // Cwd: empty
            // Exe: 9999.0.4
            // Reg: 9999.0.4
            // Expected: 9999.0.4 from exe dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.4", _dotnetSdkDllMessageTerminator));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Multi-level lookup is only supported on Windows.
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

            WriteEmptyGlobalJson();

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(DotNet.GreatestVersionHostFxrFilePath))
            {
                registeredInstallLocationOverride.SetInstallLocation(new (string, string)[] { (RepoDirectories.BuildArchitecture, _regDir) });

                // Add SDK versions
                AddAvailableSdkVersions(_regSdkBaseDir, "9999.0.4");

                // Specified SDK version: none
                // Cwd: empty
                // Exe: empty
                // Reg: 9999.0.4
                // Expected: 9999.0.4 from reg dir
                DotNet.Exec("help")
                    .WorkingDirectory(_currentWorkingDir)
                    .MultilevelLookup(true)
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnableTracingAndCaptureOutputs()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.4", _dotnetSdkDllMessageTerminator));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Multi-level lookup is only supported on Windows.
        public void SdkMultilevelLookup_Must_Pick_The_Highest_Semantic_Version()
        {
            WriteEmptyGlobalJson();

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.0.0", "9999.0.3-dummy");

            // Specified SDK version: none
            // Cwd: empty
            // Exe: empty
            // Reg: 9999.0.0, 9999.0.3-dummy
            // Expected: 9999.0.3-dummy from reg dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.3-dummy", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.3");

            // Specified SDK version: none
            // Cwd: empty
            // Exe: 9999.0.3
            // Reg: 9999.0.0, 9999.0.3-dummy
            // Expected: 9999.0.3 from exe dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.3", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_cwdSdkBaseDir, "10000.0.0");
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.0.100");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // Exe: 9999.0.3
            // Reg: 9999.0.0, 9999.0.3-dummy, 9999.0.100
            // Expected: 9999.0.100 from reg dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.100", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.80");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // Exe: 9999.0.3, 9999.0.80
            // Reg: 9999.0.0, 9999.0.3-dummy, 9999.0.100
            // Expected: 9999.0.100 from reg dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.100", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.5500000");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // Exe: 9999.0.3, 9999.0.80, 9999.0.5500000
            // Reg: 9999.0.0, 9999.0.3-dummy, 9999.0.100
            // Expected: 9999.0.5500000 from exe dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.5500000", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_regSdkBaseDir, "9999.0.52000000");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // Exe: 9999.0.3, 9999.0.80, 9999.0.5500000
            // Reg: 9999.0.0, 9999.0.3-dummy, 9999.0.100, 9999.0.52000000
            // Expected: 9999.0.52000000 from reg dir
            RunTest()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_regSelectedMessage, "9999.0.52000000", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected SDK versions
            RunTest("--list-sdks")
                .Should().Pass()
                .And.HaveStdOutContaining("9999.0.0")
                .And.HaveStdOutContaining("9999.0.3-dummy")
                .And.HaveStdOutContaining("9999.0.3")
                .And.HaveStdOutContaining("9999.0.100")
                .And.HaveStdOutContaining("9999.0.80")
                .And.HaveStdOutContaining("9999.0.5500000")
                .And.HaveStdOutContaining("9999.0.52000000");
        }

        private List<(string version, string rootPath)> AddSdkVersionsAndGetExpectedList(bool? multiLevelLookup)
        {
            AddAvailableSdkVersions(_exeSdkBaseDir, "5.0.2");
            AddAvailableSdkVersions(_exeSdkBaseDir, "6.1.1");
            AddAvailableSdkVersions(_exeSdkBaseDir, "7.1.2");
            AddAvailableSdkVersions(_regSdkBaseDir, "6.2.0");
            AddAvailableSdkVersions(_regSdkBaseDir, "7.0.1");

            // The SDKs should be ordered by version number
            List<(string version, string rootPath)> expectedList = new();
            expectedList.Add(("5.0.2", _exeSdkBaseDir));
            expectedList.Add(("6.1.1", _exeSdkBaseDir));
            expectedList.Add(("7.1.2", _exeSdkBaseDir));
            if (multiLevelLookup is null || multiLevelLookup == true)
            {
                expectedList.Add(("6.2.0", _regSdkBaseDir));
                expectedList.Add(("7.0.1", _regSdkBaseDir));
            }
            expectedList.Sort((a, b) =>
            {
                if (!Version.TryParse(a.version, out var aVersion))
                    return -1;

                if (!Version.TryParse(b.version, out var bVersion))
                    return 1;

                return aVersion.CompareTo(bVersion);
            });
            return expectedList;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(null)]
        [InlineData(false)]
        public void ListSdks(bool? multiLevelLookup)
        {
            // Multi-level lookup is only supported on Windows.
            if (!OperatingSystem.IsWindows() && multiLevelLookup != false)
                return;

            var expectedList = AddSdkVersionsAndGetExpectedList(multiLevelLookup);
            string expectedOutput = string.Join(string.Empty, expectedList.Select(t => $"{t.version} [{t.rootPath}]{Environment.NewLine}"));

            // !!IMPORTANT!!: This test verifies the exact match of the entire output of the command (not a substring!)
            // This is important as the output of --list-sdks is considered machine readable and thus must not change even in a minor way (unintentionally)
            RunTest("--list-sdks", multiLevelLookup)
                .Should().Pass()
                .And.HaveStdOut(expectedOutput);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(null)]
        [InlineData(false)]
        public void SdkResolutionError(bool? multiLevelLookup)
        {
            // Multi-level lookup is only supported on Windows.
            if (!OperatingSystem.IsWindows() && multiLevelLookup != false)
                return;

            // Set specified SDK version = 9999.3.4-global-dummy - such SDK doesn't exist
            SetGlobalJsonVersion("SingleDigit-global.json");

            // When we fail to resolve SDK version, we print out all available SDKs
            var expectedList = AddSdkVersionsAndGetExpectedList(multiLevelLookup);
            string expectedOutput = string.Join(string.Empty, expectedList.Select(t => $"        {t.version} [{t.rootPath}]{Environment.NewLine}"));

            RunTest("help", multiLevelLookup)
                .Should().Fail()
                .And.HaveStdOutContaining(expectedOutput);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(null)]
        [InlineData(false)]
        public void DotnetInfo(bool? multiLevelLookup)
        {
            // Multi-level lookup is only supported on Windows.
            if (!OperatingSystem.IsWindows() && multiLevelLookup != false)
                return;

            var expectedList = AddSdkVersionsAndGetExpectedList(multiLevelLookup);
            string expectedOutput =
                $".NET SDKs installed:{Environment.NewLine}" +
                string.Join(string.Empty, expectedList.Select(t => $"  {t.version} [{t.rootPath}]{Environment.NewLine}"));

            RunTest("--info", multiLevelLookup)
                .Should().Pass()
                .And.HaveStdOutContaining(expectedOutput);
        }

        private CommandResult RunTest() => RunTest("help");

        private CommandResult RunTest(string command, bool? multiLevelLookup = true)
        {
            return DotNet.Exec(command)
                .WorkingDirectory(_currentWorkingDir)
                .MultilevelLookup(multiLevelLookup)
                .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath, _regDir)
                .EnvironmentVariable( // Redirect the default install location to an invalid location so that a machine-wide install is not used
                    Constants.TestOnlyEnvironmentVariables.DefaultInstallPath,
                    System.IO.Path.Combine(_currentWorkingDir, "invalid"))
                .EnableTracingAndCaptureOutputs()
                .Execute();
        }

        // This method adds a list of new sdk version folders in the specified directory.
        // The actual contents are 'fake' and the mininum required for SDK discovery.
        // The dotnet.runtimeconfig.json created uses a dummy framework version (9999.0.0)
        private void AddAvailableSdkVersions(string sdkBaseDir, params string[] availableVersions)
        {
            string dummyRuntimeConfig = Path.Combine(RepoDirectories.TestAssetsFolder, "TestUtils",
                "SDKLookup", "dotnet.runtimeconfig.json");

            foreach (string version in availableVersions)
            {
                string newSdkDir = Path.Combine(sdkBaseDir, version);
                Directory.CreateDirectory(newSdkDir);

                // ./dotnet.dll
                File.WriteAllText(Path.Combine(newSdkDir, "dotnet.dll"), string.Empty);

                // ./dotnet.runtimeconfig.json
                string runtimeConfig = Path.Combine(newSdkDir, "dotnet.runtimeconfig.json");
                File.Copy(dummyRuntimeConfig, runtimeConfig, true);
            }
        }

        // Put a global.json file in the cwd in order to specify a CLI
        private void SetGlobalJsonVersion(string globalJsonFileName)
        {
            string destFile = Path.Combine(_currentWorkingDir, "global.json");
            string srcFile = Path.Combine(RepoDirectories.TestAssetsFolder, "TestUtils",
                "SDKLookup", globalJsonFileName);

            File.Copy(srcFile, destFile, true);
        }

        private void WriteGlobalJson(string contents)
        {
            File.WriteAllText(Path.Combine(_currentWorkingDir, "global.json"), contents);
        }

        private void WriteEmptyGlobalJson() => WriteGlobalJson("{}");
    }
}
