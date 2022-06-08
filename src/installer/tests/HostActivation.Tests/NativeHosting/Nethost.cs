// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using HostActivation.Tests;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class Nethost : IClassFixture<Nethost.SharedTestState>
    {
        private const string GetHostFxrPath = "get_hostfxr_path";

        private static readonly string HostFxrName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr");
        private readonly SharedTestState sharedState;

        public Nethost(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        public void GetHostFxrPath_DotNetRootEnvironment(bool explicitLoad, bool useAssemblyPath, bool isValid)
        {
            string dotNetRoot = isValid ? Path.Combine(sharedState.ValidInstallRoot, "dotnet") : sharedState.InvalidInstallRoot;
            CommandResult result = Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {explicitLoad} {(useAssemblyPath ? sharedState.TestAssemblyPath : string.Empty)}")
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(dotNetRoot)
                .Execute();

            result.Should().HaveStdErrContaining("Using environment variable");

            if (isValid)
            {
                result.Should().Pass()
                    .And.HaveStdOutContaining($"hostfxr_path: {sharedState.HostFxrPath}".ToLower());
            }
            else
            {
                result.Should().Fail()
                    .And.ExitWith(1)
                    .And.HaveStdOutContaining($"{GetHostFxrPath} failed: 0x{Constants.ErrorCode.CoreHostLibMissingFailure.ToString("x")}")
                    .And.HaveStdErrContaining($"The required library {HostFxrName} could not be found");
            }
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        public void GetHostFxrPath_DotNetRootParameter(bool explicitLoad, bool useAssemblyPath, bool isValid)
        {
            string dotNetRoot = isValid ? Path.Combine(sharedState.ValidInstallRoot, "dotnet") : sharedState.InvalidInstallRoot;
            CommandResult result = Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {explicitLoad} {(useAssemblyPath ? sharedState.TestAssemblyPath : "nullptr")} {dotNetRoot}")
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(null)
                .Execute();

            result.Should().HaveStdErrContaining("Using dotnet root parameter");

            if (isValid)
            {
                result.Should().Pass()
                    .And.HaveStdOutContaining($"hostfxr_path: {sharedState.HostFxrPath}".ToLower());
            }
            else
            {
                result.Should().Fail()
                    .And.ExitWith(1)
                    .And.HaveStdOutContaining($"{GetHostFxrPath} failed: 0x{Constants.ErrorCode.CoreHostLibMissingFailure.ToString("x")}")
                    .And.HaveStdErrContaining($"The folder [{Path.Combine(dotNetRoot, "host", "fxr")}] does not exist");
            }
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/61131", TestPlatforms.OSX)]
        [InlineData(true, false, true, false)]
        [InlineData(true, false, true, true)]
        [InlineData(true, false, false, false)]
        [InlineData(true, false, false, true)]
        [InlineData(true, true, true, false)]
        [InlineData(true, true, true, true)]
        [InlineData(true, true, false, false)]
        [InlineData(true, true, false, true)]
        [InlineData(false, false, true, false)]
        [InlineData(false, false, true, true)]
        [InlineData(false, false, false, false)]
        [InlineData(false, false, false, true)]
        [InlineData(false, true, true, false)]
        [InlineData(false, true, true, true)]
        [InlineData(false, true, false, false)]
        [InlineData(false, true, false, true)]
        public void GetHostFxrPath_GlobalInstallation(bool explicitLoad, bool useAssemblyPath, bool useRegisteredLocation, bool isValid)
        {
            // Override the registry key for self-registered global installs.
            // If using the registered location, set the install location value to the valid/invalid root.
            // If not using the registered location, do not set the value. When the value does not exist,
            // the product falls back to the default install location.
            CommandResult result;
            string installLocation = Path.Combine(isValid ? sharedState.ValidInstallRoot : sharedState.InvalidInstallRoot, "dotnet");
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(sharedState.NethostPath))
            {
                if (useRegisteredLocation)
                {
                    registeredInstallLocationOverride.SetInstallLocation((sharedState.RepoDirectories.BuildArchitecture, installLocation));
                }

                result = Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {explicitLoad} {(useAssemblyPath ? sharedState.TestAssemblyPath : string.Empty)}")
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnvironmentVariable( // Redirect the default install location to a test directory
                        Constants.TestOnlyEnvironmentVariables.DefaultInstallPath,
                        useRegisteredLocation ? sharedState.InvalidInstallRoot : installLocation)
                    .DotNetRoot(null)
                    .Execute();
            }

            result.Should().HaveUsedGlobalInstallLocation(installLocation);
            if (useRegisteredLocation)
                result.Should().HaveUsedRegisteredInstallLocation(installLocation);

            if (isValid)
            {
                result.Should().Pass()
                    .And.HaveStdOutContaining($"hostfxr_path: {sharedState.HostFxrPath}".ToLower());
            }
            else
            {
                result.Should().Fail()
                    .And.ExitWith(1)
                    .And.HaveStdOutContaining($"{GetHostFxrPath} failed: 0x{Constants.ErrorCode.CoreHostLibMissingFailure.ToString("x")}")
                    .And.HaveStdErrContaining($"The required library {HostFxrName} could not be found");
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void GetHostFxrPath_WithAssemblyPath_AppLocalFxr(bool explicitLoad, bool useDotNetRoot)
        {
            string appLocalFxrDir = Path.Combine(sharedState.BaseDirectory, "appLocalFxr");
            Directory.CreateDirectory(appLocalFxrDir);
            string assemblyPath = Path.Combine(appLocalFxrDir, "AppLocalFxr.dll");
            string hostFxrPath = Path.Combine(appLocalFxrDir, HostFxrName);
            File.WriteAllText(assemblyPath, string.Empty);
            File.WriteAllText(hostFxrPath, string.Empty);

            string dotNetRoot = useDotNetRoot ? Path.Combine(sharedState.ValidInstallRoot, "dotnet") : string.Empty;
            Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {explicitLoad} {assemblyPath} {dotNetRoot}")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"hostfxr_path: {(useDotNetRoot ? sharedState.HostFxrPath : hostFxrPath)}".ToLower());
        }

        [Fact]
        public void GetHostFxrPath_HostFxrAlreadyLoaded()
        {
            Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} false {sharedState.TestAssemblyPath} nullptr {sharedState.ProductHostFxrPath}")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"hostfxr_path: {sharedState.ProductHostFxrPath}".ToLower())
                .And.HaveStdErrContaining($"Found previously loaded library {HostFxrName}");
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/61131", TestPlatforms.OSX)]
        [SkipOnPlatform(TestPlatforms.Windows, "This test targets the install_location config file which is only used on Linux and macOS.")]
        [InlineData("{0}", false, true)]
        [InlineData("{0}\n", false, true)]
        [InlineData("{0}\nSome other text", false, true)]
        [InlineData("", false, false)]
        [InlineData("\n{0}", false, false)]
        [InlineData(" {0}", false, false)]
        [InlineData("{0} \n", false, false)]
        [InlineData("{0} ", false, false)]
        [InlineData("{0}", true, true)]
        [InlineData("{0}\n", true, true)]
        [InlineData("{0}\nSome other text", true, true)]
        [InlineData("", true, false)]
        [InlineData("\n{0}", true, false)]
        [InlineData(" {0}", true, false)]
        [InlineData("{0} \n", true, false)]
        [InlineData("{0} ", true, false)]
        public void GetHostFxrPath_InstallLocationFile(string value, bool shouldUseArchSpecificInstallLocation, bool shouldPass)
        {
            string installLocation = Path.Combine(sharedState.ValidInstallRoot, "dotnet");

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(sharedState.NethostPath))
            {
                if (shouldUseArchSpecificInstallLocation)
                    registeredInstallLocationOverride.SetInstallLocation((sharedState.RepoDirectories.BuildArchitecture, string.Format(value, installLocation)));
                else
                    registeredInstallLocationOverride.SetInstallLocation((string.Empty, string.Format(value, installLocation)));

                CommandResult result = Command.Create(sharedState.NativeHostPath, GetHostFxrPath)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnvironmentVariable( // Redirect the default install location to an invalid location so that it doesn't cause the test to pass
                        Constants.TestOnlyEnvironmentVariables.DefaultInstallPath,
                        sharedState.InvalidInstallRoot)
                    .DotNetRoot(null)
                    .Execute();

                if (shouldUseArchSpecificInstallLocation)
                {
                    result.Should().HaveLookedForArchitectureSpecificInstallLocation(
                        registeredInstallLocationOverride.PathValueOverride,
                        sharedState.RepoDirectories.BuildArchitecture);
                }
                else
                {
                    result.Should().HaveLookedForDefaultInstallLocation(registeredInstallLocationOverride.PathValueOverride);
                }

                if (shouldPass)
                {
                    result.Should().Pass()
                        .And.HaveUsedRegisteredInstallLocation(installLocation)
                        .And.HaveStdOutContaining($"hostfxr_path: {sharedState.HostFxrPath}".ToLower());
                }
                else
                {
                    result.Should().Fail()
                        .And.ExitWith(1)
                        .And.HaveStdOutContaining($"{GetHostFxrPath} failed: 0x{Constants.ErrorCode.CoreHostLibMissingFailure.ToString("x")}")
                        .And.HaveStdErrContaining($"The required library {HostFxrName} could not be found");
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/61131", TestPlatforms.OSX)]
        [SkipOnPlatform(TestPlatforms.Windows, "This test targets the install_location config file which is only used on Linux and macOS.")]
        public void GetHostFxrPath_GlobalInstallation_HasNoDefaultInstallationPath()
        {
            string installLocation = Path.Combine(sharedState.ValidInstallRoot, "dotnet");
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(sharedState.NethostPath))
            {
                registeredInstallLocationOverride.SetInstallLocation(new (string, string)[] {
                    (sharedState.RepoDirectories.BuildArchitecture, installLocation),
                    ("someOtherArch", $"{installLocation}/invalid")
                });

                CommandResult result = Command.Create(sharedState.NativeHostPath, GetHostFxrPath)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnvironmentVariable( // Redirect the default install location to an invalid location so that it doesn't cause the test to pass
                        Constants.TestOnlyEnvironmentVariables.DefaultInstallPath,
                        sharedState.InvalidInstallRoot)
                    .DotNetRoot(null)
                    .Execute();

                result.Should().Pass()
                    .And.HaveLookedForArchitectureSpecificInstallLocation(
                        registeredInstallLocationOverride.PathValueOverride,
                        sharedState.RepoDirectories.BuildArchitecture)
                    .And.HaveUsedRegisteredInstallLocation(installLocation)
                    .And.HaveStdOutContaining($"hostfxr_path: {sharedState.HostFxrPath}".ToLower());
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/61131", TestPlatforms.OSX)]
        [SkipOnPlatform(TestPlatforms.Windows, "This test targets the install_location config file which is only used on Linux and macOS.")]
        public void GetHostFxrPath_GlobalInstallation_ArchitectureSpecificPathIsPickedOverDefaultPath()
        {
            string installLocation = Path.Combine(sharedState.ValidInstallRoot, "dotnet");
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(sharedState.NethostPath))
            {
                registeredInstallLocationOverride.SetInstallLocation(new (string, string)[] {
                    (string.Empty, $"{installLocation}/a/b/c"),
                    (sharedState.RepoDirectories.BuildArchitecture, installLocation)
                });

                CommandResult result = Command.Create(sharedState.NativeHostPath, GetHostFxrPath)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnvironmentVariable( // Redirect the default install location to an invalid location so that it doesn't cause the test to pass
                        Constants.TestOnlyEnvironmentVariables.DefaultInstallPath,
                        sharedState.InvalidInstallRoot)
                    .DotNetRoot(null)
                    .Execute();

                result.Should().Pass()
                    .And.HaveLookedForArchitectureSpecificInstallLocation(
                        registeredInstallLocationOverride.PathValueOverride,
                        sharedState.RepoDirectories.BuildArchitecture)
                    .And.HaveUsedRegisteredInstallLocation(installLocation)
                    .And.HaveStdOutContaining($"hostfxr_path: {sharedState.HostFxrPath}".ToLower());
            }
        }

        [Fact]
        public void GetHostFxrPath_InvalidParameters()
        {
            Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} false [error]")
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(null)
                .Execute()
                .Should().Fail()
                .And.HaveStdOutContaining($"{GetHostFxrPath} failed: 0x{Constants.ErrorCode.InvalidArgFailure.ToString("x")}")
                .And.HaveStdErrContaining("Invalid size for get_hostfxr_parameters");
        }

        [Fact]
        public void TracingNotBufferedByDefault()
        {
            string traceFilePath;
            CommandResult result = Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} false nullptr x")
                .EnableHostTracingToFile(out traceFilePath)
                .MultilevelLookup(true)
                .DotNetRoot(null)
                .Execute();

            result.Should().Fail()
                .And.FileExists(traceFilePath)
                .And.FileContains(traceFilePath, "Tracing enabled");

            FileUtils.DeleteFileIfPossible(traceFilePath);
        }

        [Fact]
        public void TestOnlyDisabledByDefault()
        {
            // Intentionally not enabling test-only behavior. This test validates that even if the test-only env. variable is set
            // it will not take effect on its own by default.
            // To make sure the test is reliable, copy the product binary again into the test folder where we run it from.
            // This is to make sure that we're using the unmodified product binary. If some previous test
            // enabled test-only product behavior on the binary and didn't correctly cleanup, this test would fail.
            File.Copy(
                Path.Combine(sharedState.RepoDirectories.HostArtifacts, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("nethost")),
                sharedState.NethostPath,
                overwrite: true);

            Command.Create(sharedState.NativeHostPath, GetHostFxrPath)
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath, sharedState.ValidInstallRoot)
                .DotNetRoot(null)
                .Execute()
                .Should().NotHaveStdErrContaining($"Using global installation location [{sharedState.ValidInstallRoot}] as runtime location.");
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public string InvalidInstallRoot { get; }
            public string ValidInstallRoot { get; }

            public string TestAssemblyPath { get; }

            public string ProductHostFxrPath { get; }

            public SharedTestState()
            {
                InvalidInstallRoot = Path.Combine(BaseDirectory, "invalid");
                Directory.CreateDirectory(InvalidInstallRoot);

                ValidInstallRoot = Path.Combine(BaseDirectory, "valid");
                HostFxrPath = CreateHostFxr(Path.Combine(ValidInstallRoot, "dotnet"));

                string appDir = Path.Combine(BaseDirectory, "app");
                Directory.CreateDirectory(appDir);
                string assemblyPath = Path.Combine(appDir, "App.dll");
                File.WriteAllText(assemblyPath, string.Empty);
                TestAssemblyPath = assemblyPath;

                string productDir = Path.Combine(BaseDirectory, "product");
                Directory.CreateDirectory(productDir);
                ProductHostFxrPath = Path.Combine(productDir, HostFxrName);
                File.Copy(Path.Combine(RepoDirectories.HostArtifacts, HostFxrName), ProductHostFxrPath);
            }

            private string CreateHostFxr(string destinationDirectory)
            {
                string fxrRoot = Path.Combine(destinationDirectory, "host", "fxr");
                Directory.CreateDirectory(fxrRoot);

                string[] versions = new string[] { "1.1.0", "2.2.1", "2.3.0" };
                foreach (string version in versions)
                {
                    string versionDirectory = Path.Combine(fxrRoot, version);
                    Directory.CreateDirectory(versionDirectory);
                    File.WriteAllText(Path.Combine(versionDirectory, HostFxrName), string.Empty);
                }

                return Path.Combine(fxrRoot, "2.3.0", HostFxrName);
            }
        }
    }
}
