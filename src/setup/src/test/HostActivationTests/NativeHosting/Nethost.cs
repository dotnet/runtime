// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class Nethost : IClassFixture<Nethost.SharedTestState>
    {
        private const string GetHostFxrPath = "get_hostfxr_path";
        private const int CoreHostLibMissingFailure = unchecked((int)0x80008083);

        private static readonly string HostFxrName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr");
        private readonly SharedTestState sharedState;

        public Nethost(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void GetHostFxrPath_DotNetRootEnvironment(bool useAssemblyPath, bool isValid)
        {
            string dotNetRoot = isValid ? Path.Combine(sharedState.ValidInstallRoot, "dotnet") : sharedState.InvalidInstallRoot;
            CommandResult result = Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {(useAssemblyPath ? sharedState.TestAssemblyPath : string.Empty)}")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", dotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", dotNetRoot)
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
                    .And.HaveStdOutContaining($"{GetHostFxrPath} failed: 0x{CoreHostLibMissingFailure.ToString("x")}")
                    .And.HaveStdErrContaining($"The required library {HostFxrName} could not be found");
            }
        }

        [Theory]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        public void GetHostFxrPath_GlobalInstallation(bool useAssemblyPath, bool useRegisteredLocation, bool isValid)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // We don't have a good way of hooking into how the product looks for global installations yet.
                return;
            }

            // Overide the registry key for self-registered global installs.
            // If using the registered location, set the install location value to the valid/invalid root.
            // If not using the registered location, do not set the value. When the value does not exist,
            // the product falls back to the default install location.
            CommandResult result;
            string installRoot = Path.Combine(isValid ? sharedState.ValidInstallRoot : sharedState.InvalidInstallRoot);
            using (var regKeyOverride = new RegisteredInstallKeyOverride())
            {
                if (useRegisteredLocation)
                {
                    regKeyOverride.SetInstallLocation(Path.Combine(installRoot, "dotnet"), sharedState.RepoDirectories.BuildArchitecture);
                }

                string programFilesOverride = useRegisteredLocation ? sharedState.InvalidInstallRoot : installRoot;
                result = Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {(useAssemblyPath ? sharedState.TestAssemblyPath : string.Empty)}")
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .EnvironmentVariable("COREHOST_TRACE", "1")
                    .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.RegistryPath, regKeyOverride.KeyPath)
                    .EnvironmentVariable("TEST_OVERRIDE_PROGRAMFILES", programFilesOverride)
                    .Execute();
            }

            result.Should().HaveStdErrContaining("Using global installation location");

            if (isValid)
            {
                result.Should().Pass()
                    .And.HaveStdOutContaining($"hostfxr_path: {sharedState.HostFxrPath}".ToLower());
            }
            else
            {
                result.Should().Fail()
                    .And.ExitWith(1)
                    .And.HaveStdOutContaining($"{GetHostFxrPath} failed: 0x{CoreHostLibMissingFailure.ToString("x")}")
                    .And.HaveStdErrContaining($"The required library {HostFxrName} could not be found");
            }
        }

        [Fact]
        public void GetHostFxrPath_WithAssemblyPath_AppLocalFxr()
        {
            string appLocalFxrDir = Path.Combine(sharedState.BaseDirectory, "appLocalFxr");
            Directory.CreateDirectory(appLocalFxrDir);
            string assemblyPath = Path.Combine(appLocalFxrDir, "AppLocalFxr.dll");
            string hostFxrPath = Path.Combine(appLocalFxrDir, HostFxrName);
            File.WriteAllText(assemblyPath, string.Empty);
            File.WriteAllText(hostFxrPath, string.Empty);

            Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {assemblyPath}")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"hostfxr_path: {hostFxrPath}".ToLower());
        }

        [Fact]
        public void GetHostFxrPath_HostFxrAlreadyLoaded()
        {
            Command.Create(sharedState.NativeHostPath, $"{GetHostFxrPath} {sharedState.TestAssemblyPath} {sharedState.ProductHostFxrPath}")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"hostfxr_path: {sharedState.ProductHostFxrPath}".ToLower())
                .And.HaveStdErrContaining($"Found previously loaded library {HostFxrName}");
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
                File.Copy(Path.Combine(RepoDirectories.CorehostPackages, HostFxrName), ProductHostFxrPath);
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
