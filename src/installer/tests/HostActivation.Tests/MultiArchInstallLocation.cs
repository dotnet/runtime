// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.CoreSetup.Test.HostActivation;
using Xunit;

namespace HostActivation.Tests
{

    public class MultiArchInstallLocation : IClassFixture<MultiArchInstallLocation.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public MultiArchInstallLocation(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void GlobalInstallation_CurrentArchitectureIsUsedIfEnvVarSet()
        {
            var fixture = sharedTestState.StandaloneAppFixture
                .Copy();

            // Removing hostfxr from the app's folder will force us to get DOTNET_ROOT env.
            File.Delete(fixture.TestProject.HostFxrDll);
            var appExe = fixture.TestProject.AppExe;
            var arch = fixture.RepoDirProvider.BuildArchitecture.ToUpper();
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable($"DOTNET_ROOT_{arch}", fixture.SdkDotnet.BinPath)
                .DotNetRoot(fixture.BuiltDotnet.BinPath, arch)
                .Execute()
                .Should().HaveStdErrContaining($"Using environment variable DOTNET_ROOT_{arch}");
        }

        [Fact]
        public void GlobalInstallation_IfNoArchSpecificEnvVarIsFoundDotnetRootIsUed()
        {
            var fixture = sharedTestState.StandaloneAppFixture
                .Copy();

            // Removing hostfxr from the app's folder will force us to get DOTNET_ROOT env.
            File.Delete(fixture.TestProject.HostFxrDll);
            var appExe = fixture.TestProject.AppExe;
            var arch = fixture.RepoDirProvider.BuildArchitecture.ToUpper();
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(fixture.BuiltDotnet.BinPath)
                .Execute()
                .Should().HaveStdErrContaining($"Using environment variable DOTNET_ROOT");
        }

        [Fact]
        public void GlobalInstallation_ArchSpecificDotnetRootIsUsedOverDotnetRoot()
        {
            var fixture = sharedTestState.StandaloneAppFixture
                .Copy();

            // Removing hostfxr from the app's folder will force us to get DOTNET_ROOT env.
            File.Delete(fixture.TestProject.HostFxrDll);
            var appExe = fixture.TestProject.AppExe;
            var arch = fixture.RepoDirProvider.BuildArchitecture.ToUpper();
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable("DOTNET_ROOT", "non_existent_path")
                .EnvironmentVariable($"DOTNET_ROOT_{arch}", fixture.SdkDotnet.BinPath)
                .Execute()
                .Should().HaveStdErrContaining($"DOTNET_ROOT_{arch}")
                .And.NotHaveStdErrContaining("Using environment variable DOTNET_ROOT=");
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "This test targets the install_location config file which is only used on Linux and macOS.")]
        public void InstallLocationFile_ArchSpecificLocationIsPickedFirst()
        {
            var fixture = sharedTestState.StandaloneAppFixture
                .Copy();

            File.Delete(fixture.TestProject.HostFxrDll);
            var appExe = fixture.TestProject.AppExe;

            var arch1 = fixture.RepoDirProvider.BuildArchitecture;
            var path1 = "a/b/c";
            var arch2 = "someArch";
            var path2 = "x/y/z";

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(appExe))
            {
                registeredInstallLocationOverride.SetInstallLocation(new (string, string)[] {
                    (string.Empty, path1),
                    (arch1, path1),
                    (arch2, path2)
                });
                Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .DotNetRoot(null)
                    .Execute()
                    .Should().HaveStdErrContaining($"Found install location path '{path1}'.")
                    .And.HaveStdErrContaining($"Found architecture-specific install location path: '{path1}' ('{arch1}').")
                    .And.HaveStdErrContaining($"Found architecture-specific install location path matching the current OS architecture ('{arch1}'): '{path1}'.")
                    .And.HaveStdErrContaining($"Found architecture-specific install location path: '{path2}' ('{arch2}').")
                    .And.HaveStdErrContaining($"Using global installation location [{path1}] as runtime location.");
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "This test targets the install_location config file which is only used on Linux and macOS.")]
        public void InstallLocationFile_OnlyFirstLineMayNotSpecifyArchitecture()
        {
            var fixture = sharedTestState.StandaloneAppFixture
                .Copy();

            File.Delete(fixture.TestProject.HostFxrDll);
            var appExe = fixture.TestProject.AppExe;

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(appExe))
            {
                registeredInstallLocationOverride.SetInstallLocation(new (string, string)[] {
                    (string.Empty, "a/b/c"),
                    (string.Empty, "x/y/z"),
                });
                Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .DotNetRoot(null)
                    .Execute()
                    .Should().HaveStdErrContaining($"Found install location path 'a/b/c'.")
                    .And.HaveStdErrContaining($"Only the first line in '{registeredInstallLocationOverride.PathValueOverride}' may not have an architecture prefix.")
                    .And.HaveStdErrContaining($"Using install location 'a/b/c'.");
            }
        }

        public class SharedTestState : IDisposable
        {
            public string BaseDirectory { get; }
            public TestProjectFixture StandaloneAppFixture { get; }
            public RepoDirectoriesProvider RepoDirectories { get; }
            public string InstallLocation { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();
                var fixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
                fixture
                    .EnsureRestored()
                    .BuildProject();

                StandaloneAppFixture = fixture;
                BaseDirectory = Path.GetDirectoryName(StandaloneAppFixture.SdkDotnet.GreatestVersionHostFxrFilePath);
            }

            public void Dispose()
            {
                // StandaloneAppFixture.Dispose();
            }
        }
    }
}
