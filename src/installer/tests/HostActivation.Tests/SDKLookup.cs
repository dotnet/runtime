// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            _exeSelectedMessage = $"Using .NET SDK dll=[{_exeSdkBaseDir}";
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
            CopyGlobalJson("SingleDigit-global.json");

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
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version")
                .And.HaveStdErrContaining("It was not possible to find any installed .NET SDKs")
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
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version")
                .And.NotHaveStdErrContaining("It was not possible to find any installed .NET SDKs");

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
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version")
                .And.NotHaveStdErrContaining("It was not possible to find any installed .NET SDKs");

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
            CopyGlobalJson("TwoPart-global.json");

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
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version")
                .And.HaveStdErrContaining("It was not possible to find any installed .NET SDKs");

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
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version")
                .And.NotHaveStdErrContaining("It was not possible to find any installed .NET SDKs");

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
                .And.HaveStdErrContaining("A compatible installed .NET SDK for global.json version")
                .And.NotHaveStdErrContaining("It was not possible to find any installed .NET SDKs");

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
            WriteEmptyGlobalJson();

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
                .And.HaveStdErrContaining("It was not possible to find any installed .NET SDKs")
                .And.HaveStdErrContaining("Install a .NET SDK from");

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
            WriteEmptyGlobalJson();

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

        [Theory]
        [InlineData("diSABle")]
        [InlineData("PaTCh")]
        [InlineData("FeaturE")]
        [InlineData("MINOR")]
        [InlineData("maJor")]
        [InlineData("LatestPatch")]
        [InlineData("Latestfeature")]
        [InlineData("latestMINOR")]
        [InlineData("latESTMajor")]
        public void It_allows_case_insensitive_roll_forward_policy_names(string rollForward)
        {
            const string Requested = "9999.0.100";

            WriteEmptyGlobalJson();

            AddAvailableSdkVersions(_exeSdkBaseDir, Requested);

            WriteGlobalJson(FormatGlobalJson(policy: rollForward, version: Requested));

            DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, Requested, _dotnetSdkDllMessageTerminator));
        }

        [Theory]
        [MemberData(nameof(InvalidGlobalJsonData))]
        public void It_falls_back_to_latest_sdk_for_invalid_global_json(string globalJsonContents, string[] messages)
        {
            AddAvailableSdkVersions(_exeSdkBaseDir, "9999.0.100", "9999.0.300-dummy.9", "9999.1.402");

            WriteGlobalJson(globalJsonContents);

            var expectation = DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeSelectedMessage, "9999.1.402", _dotnetSdkDllMessageTerminator));

            foreach (var message in messages)
            {
                expectation = expectation.And.HaveStdErrContaining(message);
            }
        }

        [Theory]
        [MemberData(nameof(SdkRollForwardData))]
        public void It_rolls_forward_as_expected(string policy, string requested, bool allowPrerelease, string expected, string[] installed)
        {
            AddAvailableSdkVersions(_exeSdkBaseDir, installed);

            WriteGlobalJson(FormatGlobalJson(policy: policy, version: requested, allowPrerelease: allowPrerelease));

            var result = DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute();

            var globalJson = Path.Combine(_currentWorkingDir, "global.json");

            if (expected == null)
            {
                result
                    .Should()
                    .Fail()
                    .And.HaveStdErrContaining($"A compatible installed .NET SDK for global.json version [{requested}] from [{globalJson}] was not found")
                    .And.HaveStdErrContaining($"Install the [{requested}] .NET SDK or update [{globalJson}] with an installed .NET SDK:");
            }
            else
            {
                result
                    .Should()
                    .Pass()
                    .And.HaveStdErrContaining($"SDK path resolved to [{Path.Combine(_exeSdkBaseDir, expected)}]");
            }
        }

        [Fact]
        public void It_uses_latest_stable_sdk_if_allow_prerelease_is_false()
        {
            var installed = new string[] {
                    "9999.1.702",
                    "9999.2.101",
                    "9999.2.203",
                    "9999.2.204-preview1",
                    "10000.0.100-preview3",
                    "10000.0.100-preview7",
                    "10000.0.100",
                    "10000.1.102",
                    "10000.1.106",
                    "10000.0.200-preview5",
                    "10000.1.100-preview3",
                    "10001.0.100-preview3",
                };

            const string ExpectedVersion = "10000.1.106";

            AddAvailableSdkVersions(_exeSdkBaseDir, installed);

            WriteGlobalJson(FormatGlobalJson(allowPrerelease: false));

            var result = DotNet.Exec("help")
                .WorkingDirectory(_currentWorkingDir)
                .WithUserProfile(_userDir)
                .Environment(s_DefaultEnvironment)
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And.HaveStdErrContaining($"SDK path resolved to [{Path.Combine(_exeSdkBaseDir, ExpectedVersion)}]");
        }

        public static IEnumerable<object[]> InvalidGlobalJsonData
        {
            get
            {
                const string IgnoringSDKSettings = "Ignoring SDK settings in global.json: the latest installed .NET SDK (including prereleases) will be used";

                // Use invalid JSON
                yield return new object[] {
                    "{ sdk: { \"version\": \"9999.0.100\" } }",
                    new[] {
                        "A JSON parsing exception occurred",
                        IgnoringSDKSettings
                    }
                };

                // Use something other than a JSON object
                yield return new object[] {
                    "true",
                    new[] {
                        "Expected a JSON object",
                        IgnoringSDKSettings
                    }
                };

                // Use a non-string version
                yield return new object[] {
                    "{ \"sdk\": { \"version\": 1 } }",
                    new[] {
                        "Expected a string for the 'sdk/version' value",
                        IgnoringSDKSettings
                    }
                };

                // Use an invalid version value
                yield return new object[] {
                    FormatGlobalJson(version: "invalid"),
                    new[] {
                        "Version 'invalid' is not valid for the 'sdk/version' value",
                        IgnoringSDKSettings
                    }
                };

                // Use a non-string policy
                yield return new object[] {
                    "{ \"sdk\": { \"rollForward\": true } }",
                    new[] {
                        "Expected a string for the 'sdk/rollForward' value",
                        IgnoringSDKSettings
                    }
                };

                // Use a policy but no version
                yield return new object[] {
                    FormatGlobalJson(policy: "latestPatch"),
                    new[] {
                        "The roll-forward policy 'latestPatch' requires a 'sdk/version' value",
                        IgnoringSDKSettings
                    }
                };

                // Use an invalid policy value
                yield return new object[] {
                    FormatGlobalJson(policy: "invalid"),
                    new[] {
                        "The roll-forward policy 'invalid' is not supported for the 'sdk/rollForward' value",
                        IgnoringSDKSettings
                    }
                };

                // Use a non-boolean allow prerelease
                yield return new object[] {
                    "{ \"sdk\": { \"allowPrerelease\": \"true\" } }",
                    new[] {
                        "Expected a boolean for the 'sdk/allowPrerelease' value",
                        IgnoringSDKSettings
                    }
                };

                // Use a prerelease version and allowPrerelease = false
                yield return new object[] {
                    FormatGlobalJson(version: "9999.1.402-preview1", allowPrerelease: false),
                    new[] { "Ignoring the 'sdk/allowPrerelease' value" }
                };
            }
        }

        public static IEnumerable<object[]> SdkRollForwardData
        {
            get
            {
                const string Requested = "9999.1.501";

                var installed = new string[] {
                    "9999.1.500",
                };

                // Array of (policy, expected) tuples
                var policies = new[] {
                    ((string)null,    (string)null),
                    ("patch",         (string)null),
                    ("feature",       (string)null),
                    ("minor",         (string)null),
                    ("major",         (string)null),
                    ("latestPatch",   (string)null),
                    ("latestFeature", (string)null),
                    ("latestMinor",   (string)null),
                    ("latestMajor",   (string)null),
                    ("disable",       (string)null),
                    ("invalid",       "9999.1.500"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        true,         // allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9999.1.500",
                    "9999.2.100-preview1",
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    ((string)null,    (string)null),
                    ("patch",         (string)null),
                    ("feature",       (string)null),
                    ("minor",         (string)null),
                    ("major",         (string)null),
                    ("latestPatch",   (string)null),
                    ("latestFeature", (string)null),
                    ("latestMinor",   (string)null),
                    ("latestMajor",   (string)null),
                    ("disable",       (string)null),
                    ("invalid",       "9999.2.100-preview1"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        false,        // do not allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9998.0.300",
                    "9999.1.500",
                    "9999.1.501",
                    "9999.1.503-preview5",
                    "9999.1.503",
                    "9999.1.504-preview1",
                    "9999.1.504-preview2",
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    ((string)null,    "9999.1.501"),
                    ("patch",         "9999.1.501"),
                    ("feature",       "9999.1.504-preview2"),
                    ("minor",         "9999.1.504-preview2"),
                    ("major",         "9999.1.504-preview2"),
                    ("latestPatch",   "9999.1.504-preview2"),
                    ("latestFeature", "9999.1.504-preview2"),
                    ("latestMinor",   "9999.1.504-preview2"),
                    ("latestMajor",   "9999.1.504-preview2"),
                    ("disable",       "9999.1.501"),
                    ("invalid",       "9999.1.504-preview2"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        true,         // allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9998.0.300",
                    "9999.1.500",
                    "9999.1.501",
                    "9999.1.503-preview5",
                    "9999.1.503",
                    "9999.1.504-preview1",
                    "9999.1.504-preview2",
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    ((string)null,    "9999.1.501"),
                    ("patch",         "9999.1.501"),
                    ("feature",       "9999.1.503"),
                    ("minor",         "9999.1.503"),
                    ("major",         "9999.1.503"),
                    ("latestPatch",   "9999.1.503"),
                    ("latestFeature", "9999.1.503"),
                    ("latestMinor",   "9999.1.503"),
                    ("latestMajor",   "9999.1.503"),
                    ("disable",       "9999.1.501"),
                    ("invalid",       "9999.1.504-preview2"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        false,        // don't allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9998.0.300",
                    "9999.1.500",
                    "9999.1.503",
                    "9999.1.505-preview2",
                    "9999.1.505",
                    "9999.1.506-preview1",
                    "9999.1.601",
                    "9999.1.608-preview3",
                    "9999.1.609",
                    "9999.2.101",
                    "9999.2.203-preview1",
                    "9999.2.203",
                    "10000.0.100",
                    "10000.1.100-preview1"
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    (null,            "9999.1.506-preview1"),
                    ("patch",         "9999.1.506-preview1"),
                    ("feature",       "9999.1.506-preview1"),
                    ("minor",         "9999.1.506-preview1"),
                    ("major",         "9999.1.506-preview1"),
                    ("latestPatch",   "9999.1.506-preview1"),
                    ("latestFeature", "9999.1.609"),
                    ("latestMinor",   "9999.2.203"),
                    ("latestMajor",   "10000.1.100-preview1"),
                    ("disable",       (string)null),
                    ("invalid",       "10000.1.100-preview1"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        true,         // allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9998.0.300",
                    "9999.1.500",
                    "9999.1.503",
                    "9999.1.505-preview2",
                    "9999.1.505",
                    "9999.1.506-preview1",
                    "9999.1.601",
                    "9999.1.608-preview3",
                    "9999.1.609",
                    "9999.2.101",
                    "9999.2.203-preview1",
                    "9999.2.203",
                    "10000.0.100",
                    "10000.1.100-preview1"
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    (null,            "9999.1.505"),
                    ("patch",         "9999.1.505"),
                    ("feature",       "9999.1.505"),
                    ("minor",         "9999.1.505"),
                    ("major",         "9999.1.505"),
                    ("latestPatch",   "9999.1.505"),
                    ("latestFeature", "9999.1.609"),
                    ("latestMinor",   "9999.2.203"),
                    ("latestMajor",   "10000.0.100"),
                    ("disable",       (string)null),
                    ("invalid",       "10000.1.100-preview1"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        false,        // don't allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9998.0.300",
                    "9999.1.500",
                    "9999.1.601",
                    "9999.1.604-preview3",
                    "9999.1.604",
                    "9999.1.605-preview4",
                    "9999.1.701",
                    "9999.1.702-preview1",
                    "9999.1.702",
                    "9999.2.101",
                    "9999.2.203",
                    "9999.2.204-preview1",
                    "10000.0.100-preview7",
                    "10000.0.100",
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    ((string)null,    (string)null),
                    ("patch",         (string)null),
                    ("feature",       "9999.1.605-preview4"),
                    ("minor",         "9999.1.605-preview4"),
                    ("major",         "9999.1.605-preview4"),
                    ("latestPatch",   (string)null),
                    ("latestFeature", "9999.1.702"),
                    ("latestMinor",   "9999.2.204-preview1"),
                    ("latestMajor",   "10000.0.100"),
                    ("disable",       (string)null),
                    ("invalid",       "10000.0.100"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        true,         // allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9998.0.300",
                    "9999.1.500",
                    "9999.1.601",
                    "9999.1.604-preview3",
                    "9999.1.604",
                    "9999.1.605-preview4",
                    "9999.1.701",
                    "9999.1.702-preview1",
                    "9999.1.702",
                    "9999.2.101",
                    "9999.2.203",
                    "9999.2.204-preview1",
                    "10000.0.100-preview7",
                    "10000.0.100",
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    ((string)null,    (string)null),
                    ("patch",         (string)null),
                    ("feature",       "9999.1.604"),
                    ("minor",         "9999.1.604"),
                    ("major",         "9999.1.604"),
                    ("latestPatch",   (string)null),
                    ("latestFeature", "9999.1.702"),
                    ("latestMinor",   "9999.2.203"),
                    ("latestMajor",   "10000.0.100"),
                    ("disable",       (string)null),
                    ("invalid",       "10000.0.100"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        false,        // don't allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9998.0.300",
                    "9999.1.500",
                    "9999.2.101-preview4",
                    "9999.2.101",
                    "9999.2.102-preview1",
                    "9999.2.203",
                    "9999.3.501",
                    "9999.4.205-preview3",
                    "10000.0.100",
                    "10000.1.100-preview1"
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    ((string)null,    (string)null),
                    ("patch",         (string)null),
                    ("feature",       (string)null),
                    ("minor",         "9999.2.102-preview1"),
                    ("major",         "9999.2.102-preview1"),
                    ("latestPatch",   (string)null),
                    ("latestFeature", (string)null),
                    ("latestMinor",   "9999.4.205-preview3"),
                    ("latestMajor",   "10000.1.100-preview1"),
                    ("disable",       (string)null),
                    ("invalid",       "10000.1.100-preview1"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        true,         // allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9998.0.300",
                    "9999.1.500",
                    "9999.2.101-preview4",
                    "9999.2.101",
                    "9999.2.102-preview1",
                    "9999.2.203",
                    "9999.3.501",
                    "9999.4.205-preview3",
                    "10000.0.100",
                    "10000.1.100-preview1"
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    ((string)null,    (string)null),
                    ("patch",         (string)null),
                    ("feature",       (string)null),
                    ("minor",         "9999.2.101"),
                    ("major",         "9999.2.101"),
                    ("latestPatch",   (string)null),
                    ("latestFeature", (string)null),
                    ("latestMinor",   "9999.3.501"),
                    ("latestMajor",   "10000.0.100"),
                    ("disable",       (string)null),
                    ("invalid",       "10000.1.100-preview1"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        false,        // don't allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9998.0.300",
                    "9999.1.500",
                    "10000.0.100",
                    "10000.0.105-preview1",
                    "10000.0.105",
                    "10000.0.106-preview1",
                    "10000.1.102",
                    "10000.1.107",
                    "10000.3.100",
                    "10000.3.102-preview3",
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    ((string)null,    (string)null),
                    ("patch",         (string)null),
                    ("feature",       (string)null),
                    ("minor",         (string)null),
                    ("major",         "10000.0.106-preview1"),
                    ("latestPatch",   (string)null),
                    ("latestFeature", (string)null),
                    ("latestMinor",   (string)null),
                    ("latestMajor",   "10000.3.102-preview3"),
                    ("disable",       (string)null),
                    ("invalid",       "10000.3.102-preview3"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        true,         // allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }

                installed = new string[] {
                    "9998.0.300",
                    "9999.1.500",
                    "10000.0.100",
                    "10000.0.105-preview1",
                    "10000.0.105",
                    "10000.0.106-preview1",
                    "10000.1.102",
                    "10000.1.107",
                    "10000.3.100",
                    "10000.3.102-preview3",
                };

                // Array of (policy, expected) tuples
                policies = new[] {
                    ((string)null,    (string)null),
                    ("patch",         (string)null),
                    ("feature",       (string)null),
                    ("minor",         (string)null),
                    ("major",         "10000.0.105"),
                    ("latestPatch",   (string)null),
                    ("latestFeature", (string)null),
                    ("latestMinor",   (string)null),
                    ("latestMajor",   "10000.3.100"),
                    ("disable",       (string)null),
                    ("invalid",       "10000.3.102-preview3"),
                };

                foreach (var policy in policies)
                {
                    yield return new object[] {
                        policy.Item1, // policy
                        Requested,    // requested
                        false,        // don't allow prerelease
                        policy.Item2, // expected
                        installed     // installed
                    };
                }
            }
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
        private void CopyGlobalJson(string globalJsonFileName)
        {
            string destFile = Path.Combine(_currentWorkingDir, "global.json");
            string srcFile = Path.Combine(RepoDirectories.TestAssetsFolder, "TestUtils",
                "SDKLookup", globalJsonFileName);

            File.Copy(srcFile, destFile, true);
        }

        private static string FormatGlobalJson(string version = null, string policy = null, bool? allowPrerelease = null)
        {
            version = version == null ? "null" : string.Format("\"{0}\"", version);
            policy = policy == null ? "null" : string.Format("\"{0}\"", policy);
            string allow = allowPrerelease.HasValue ? (allowPrerelease.Value ? "true" : "false") : "null";

            return $@"{{ ""sdk"": {{ ""version"": {version}, ""rollForward"": {policy}, ""allowPrerelease"": {allow} }} }}";
        }

        private void WriteGlobalJson(string contents)
        {
            File.WriteAllText(Path.Combine(_currentWorkingDir, "global.json"), contents);
        }

        private void WriteEmptyGlobalJson() => WriteGlobalJson("{}");
    }
}
