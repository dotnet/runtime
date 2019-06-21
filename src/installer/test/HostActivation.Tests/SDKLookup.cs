// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class SDKLookup : IDisposable
    {
        private static readonly IDictionary<string, string> s_DefaultEnvironment = new Dictionary<string, string>()
        {
            {"COREHOST_TRACE", "1" },
            // The SDK being used may be crossgen'd for a different architecture than we are building for.
            // Turn off ready to run, so an x64 crossgen'd SDK can be loaded in an x86 process.
            {"COMPlus_ReadyToRun", "0" },
        };

        private readonly RepoDirectoriesProvider RepoDirectories;
        private readonly DotNetCli DotNet;

        private readonly string _baseDir;
        private readonly string _currentWorkingDir;
        private readonly string _userDir;
        private readonly string _executableDir;
        private readonly string _cwdSdkBaseDir;
        private readonly string _userSdkBaseDir;
        private readonly string _exeSdkBaseDir;
        private readonly string _exeSelectedMessage;

        private const string _dotnetSdkDllMessageTerminator = "dotnet.dll]";

        public SDKLookup()
        {
            // The dotnetSDKLookup dir will contain some folders and files that will be
            // necessary to perform the tests
            string baseDir = Path.Combine(TestArtifact.TestArtifactsPath, "dotnetSDKLookup");
            _baseDir = SharedFramework.CalculateUniqueTestDirectory(baseDir);

            // The three tested locations will be the cwd, the user folder and the exe dir. cwd and user are no longer supported.
            //     All dirs will be placed inside the base folder
            _currentWorkingDir = Path.Combine(_baseDir, "cwd");
            _userDir = Path.Combine(_baseDir, "user");
            _executableDir = Path.Combine(_baseDir, "exe");

            DotNet = new DotNetBuilder(_baseDir, Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"), "exe")
                .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("9999.0.0")
                .Build();

            RepoDirectories = new RepoDirectoriesProvider(builtDotnet: DotNet.BinPath);

            // SdkBaseDirs contain all available version folders
            _cwdSdkBaseDir = Path.Combine(_currentWorkingDir, "sdk");
            _userSdkBaseDir = Path.Combine(_userDir, ".dotnet", RepoDirectories.BuildArchitecture, "sdk");
            _exeSdkBaseDir = Path.Combine(_executableDir, "sdk");

            // Create directories
            Directory.CreateDirectory(_cwdSdkBaseDir);
            Directory.CreateDirectory(_userSdkBaseDir);
            Directory.CreateDirectory(_exeSdkBaseDir);
            
            // Trace messages used to identify from which folder the SDK was picked
            _exeSelectedMessage = $"Using dotnet SDK dll=[{_exeSdkBaseDir}";
        }

        public void Dispose()
        {
            if (!TestArtifact.PreserveTestRuns())
            {
                Directory.Delete(_baseDir, true);
            }
        }

        [Fact]
        public void SdkLookup_Global_Json_Single_Digit_Patch_Rollup()
        {
            // Set specified SDK version = 9999.3.4-global-dummy
            SetGlobalJsonVersion("SingleDigit-global.json");

            // Specified SDK version: 9999.3.4-global-dummy
            // Exe: empty
            // Expected: no compatible version and a specific error messages
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed dotnet SDK for global.json version")
                .And.HaveStdErrContaining("It was not possible to find any installed dotnet SDKs")
                .And.HaveStdErrContaining("aka.ms/dotnet-download")
                .And.NotHaveStdErrContaining("Checking if resolved SDK dir");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.4.1", "9999.3.4-dummy");

            // Specified SDK version: 9999.3.4-global-dummy
            // Exe: 9999.4.1, 9999.3.4-dummy
            // Expected: no compatible version and a specific error message
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed dotnet SDK for global.json version")
                .And.NotHaveStdErrContaining("It was not possible to find any installed dotnet SDKs");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.3");

            // Specified SDK version: 9999.3.4-global-dummy
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.3
            // Expected: no compatible version and a specific error message
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed dotnet SDK for global.json version")
                .And.NotHaveStdErrContaining("It was not possible to find any installed dotnet SDKs");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.4");

            // Specified SDK version: 9999.3.4-global-dummy
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.3, 9999.3.4
            // Expected: 9999.3.4 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.4", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.5-dummy");

            // Specified SDK version: 9999.3.4-global-dummy
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.3, 9999.3.4, 9999.3.5-dummy
            // Expected: 9999.3.5-dummy from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.5-dummy", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.600");

            // Specified SDK version: 9999.3.4-global-dummy
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.3, 9999.3.4, 9999.3.5-dummy, 9999.3.600
            // Expected: 9999.3.5-dummy from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.5-dummy", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.4-global-dummy");

            // Specified SDK version: 9999.3.4-global-dummy
            // Exe: 9999.4.1, 9999.3.4-dummy, 9999.3.3, 9999.3.4, 9999.3.5-dummy, 9999.3.600, 9999.3.4-global-dummy
            // Expected: 9999.3.4-global-dummy from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.4-global-dummy", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected SDK versions
            DotNet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .Execute()
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
        public void SdkLookup_Global_Json_Two_Part_Patch_Rollup()
        {
            // Set specified SDK version = 9999.3.304-global-dummy
            SetGlobalJsonVersion("TwoPart-global.json");

            // Specified SDK version: 9999.3.304-global-dummy
            // Exe: empty
            // Expected: no compatible version and a specific error messages
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed dotnet SDK for global.json version")
                .And.HaveStdErrContaining("It was not possible to find any installed dotnet SDKs");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.57", "9999.3.4-dummy");

            // Specified SDK version: 9999.3.304-global-dummy
            // Exe: 9999.3.57, 9999.3.4-dummy
            // Expected: no compatible version and a specific error message
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed dotnet SDK for global.json version")
                .And.NotHaveStdErrContaining("It was not possible to find any installed dotnet SDKs");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.300", "9999.7.304-global-dummy");

            // Specified SDK version: 9999.3.304-global-dummy
            // Exe: 9999.3.57, 9999.3.4-dummy, 9999.3.300, 9999.7.304-global-dummy
            // Expected: no compatible version and a specific error message
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("A compatible installed dotnet SDK for global.json version")
                .And.NotHaveStdErrContaining("It was not possible to find any installed dotnet SDKs");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.304");

            // Specified SDK version: 9999.3.304-global-dummy
            // Exe: 99999.3.57, 9999.3.4-dummy, 9999.3.300, 9999.7.304-global-dummy, 9999.3.304
            // Expected: 9999.3.304 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.304", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.399", "9999.3.399-dummy", "9999.3.400");

            // Specified SDK version: 9999.3.304-global-dummy
            // Exe: 9999.3.57, 9999.3.4-dummy, 9999.3.300, 9999.7.304-global-dummy, 9999.3.304, 9999.3.399, 9999.3.399-dummy, 9999.3.400
            // Expected: 9999.3.399 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.399", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.2400", "9999.3.3004");

            // Specified SDK version: 9999.3.304-global-dummy
            // Exe: 9999.3.57, 9999.3.4-dummy, 9999.3.300, 9999.7.304-global-dummy, 9999.3.304, 9999.3.399, 9999.3.399-dummy, 9999.3.400, 9999.3.2400, 9999.3.3004
            // Expected: 9999.3.399 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.399", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.3.304-global-dummy");

            // Specified SDK version: 9999.3.304-global-dummy
            // Exe: 9999.3.57, 9999.3.4-dummy, 9999.3.300, 9999.7.304-global-dummy, 9999.3.304, 9999.3.399, 9999.3.399-dummy, 9999.3.400, 9999.3.2400, 9999.3.3004, 9999.3.304-global-dummy
            // Expected: 9999.3.304-global-dummy from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.3.304-global-dummy", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected SDK versions
            DotNet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .Execute()
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
        public void SdkLookup_Negative_Version()
        {
            // Add a negative SDK version
            AddAvailableSdkVersions(_exeSdkBaseDir, "-1.-1.-1");

            // Specified SDK version: none
            // Exe: -1.-1.-1
            // Expected: no compatible version and a specific error messages
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("It was not possible to find any installed dotnet SDKs")
                .And.HaveStdErrContaining("Did you mean to run dotnet SDK commands? Please install dotnet SDK from");

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.4");

            // Specified SDK version: none
            // Exe: -1.-1.-1, 9999.0.4
            // Expected: 9999.0.4 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.4", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected SDK versions
            DotNet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("9999.0.4");
        }

        [Fact]
        public void SdkLookup_Must_Pick_The_Highest_Semantic_Version()
        {
            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.0", "9999.0.3-dummy.9", "9999.0.3-dummy.10");

            // Specified SDK version: none
            // Cwd: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.3-dummy.9, 9999.0.3-dummy.10
            // Expected: 9999.0.3-dummy.10 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.3-dummy.10", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.3");

            // Specified SDK version: none
            // Cwd: empty
            // User: empty
            // Exe: 9999.0.0, 9999.0.3-dummy.9, 9999.0.3-dummy.10, 9999.0.3
            // Expected: 9999.0.3 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.3", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_userSdkBaseDir, "9999.0.200");
            AddAvailableSdkVersions(_cwdSdkBaseDir, "10000.0.0");
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.100");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.0, 9999.0.3-dummy.9, 9999.0.3-dummy.10, 9999.0.3, 9999.0.100
            // Expected: 9999.0.100 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.100", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.80");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.0, 9999.0.3-dummy.9, 9999.0.3-dummy.10, 9999.0.3, 9999.0.100, 9999.0.80
            // Expected: 9999.0.100 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.100", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.5500000");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.0, 9999.0.3-dummy.9, 9999.0.3-dummy.10, 9999.0.3, 9999.0.100, 9999.0.80, 9999.0.5500000
            // Expected: 9999.0.5500000 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.5500000", _dotnetSdkDllMessageTerminator));

            // Add SDK versions
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.52000000");

            // Specified SDK version: none
            // Cwd: 10000.0.0                 --> should not be picked
            // User: 9999.0.200               --> should not be picked
            // Exe: 9999.0.0, 9999.0.3-dummy.9, 9999.0.3-dummy.10, 9999.0.3, 9999.0.100, 9999.0.80, 9999.0.5500000, 9999.0.52000000
            // Expected: 9999.0.52000000 from exe dir
            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.0.52000000", _dotnetSdkDllMessageTerminator));

            // Verify we have the expected SDK versions
            DotNet.Exec("--list-sdks")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("9999.0.0")
                .And.HaveStdOutContaining("9999.0.3-dummy.9")
                .And.HaveStdOutContaining("9999.0.3-dummy.10")
                .And.HaveStdOutContaining("9999.0.3")
                .And.HaveStdOutContaining("9999.0.100")
                .And.HaveStdOutContaining("9999.0.80")
                .And.HaveStdOutContaining("9999.0.5500000")
                .And.HaveStdOutContaining("9999.0.52000000");
        }

        // This method adds a list of new sdk version folders in the specified directory.
        // The actual contents are 'fake' and the mininum required for SDK discovery.
        // The dotnet.runtimeconfig.json created uses a dummy framework version (9999.0.0)
        private void AddAvailableSdkVersions(string sdkBaseDir, params string[] availableVersions)
        {
            string dummyRuntimeConfig = Path.Combine(RepoDirectories.RepoRoot, "src", "test", "Assets", "TestUtils",
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
            string srcFile = Path.Combine(RepoDirectories.RepoRoot, "src", "test", "Assets", "TestUtils",
                "SDKLookup", globalJsonFileName);

            File.Copy(srcFile, destFile, true);
        }
    }
}
