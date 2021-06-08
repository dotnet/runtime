// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
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
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var arch = fixture.RepoDirProvider.BuildArchitecture.ToUpper();
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(fixture.BuiltDotnet.BinPath, arch)
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Using environment variable DOTNET_ROOT_{arch}");
        }

        [Fact]
        public void GlobalInstallation_IfNoArchSpecificEnvVarIsFoundDotnetRootIsUed()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var arch = fixture.RepoDirProvider.BuildArchitecture.ToUpper();
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(fixture.BuiltDotnet.BinPath)
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Using environment variable DOTNET_ROOT=");
        }

        [Fact]
        public void GlobalInstallation_ArchSpecificDotnetRootIsUsedOverDotnetRoot()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var arch = fixture.RepoDirProvider.BuildArchitecture.ToUpper();
            var dotnet = fixture.BuiltDotnet.BinPath;
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot("non_existent_path")
                .DotNetRoot(dotnet, arch)
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Using environment variable DOTNET_ROOT_{arch}=[{dotnet}] as runtime location.")
                .And.NotHaveStdErrContaining("Using environment variable DOTNET_ROOT=");
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(false)]
        [InlineData(true)]
        public void GlobalInstallation_WindowsX86(bool setArchSpecificDotnetRoot)
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            if (RuntimeInformation.OSArchitecture != Architecture.X86)
                return;

            var appExe = fixture.TestProject.AppExe;
            var arch = fixture.RepoDirProvider.BuildArchitecture.ToUpper();
            var dotnet = fixture.BuiltDotnet.BinPath;
            var command = Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(dotnet);

            if (setArchSpecificDotnetRoot)
                command.DotNetRoot(dotnet, arch);

            var result = command.Execute();
            result.Should().Pass();

            if (setArchSpecificDotnetRoot)
                result.Should().HaveStdErrContaining($"Using environment variable DOTNET_ROOT_X86=[{dotnet}] as runtime location.");
            else
                result.Should().HaveStdErrContaining($"Using environment variable DOTNET_ROOT(x86)=[{dotnet}] as runtime location.");
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "This test targets the install_location config file which is only used on Linux and macOS.")]
        public void InstallLocationFile_ArchSpecificLocationIsPickedFirst()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var arch1 = "someArch";
            var path1 = "x/y/z";
            var arch2 = fixture.RepoDirProvider.BuildArchitecture;
            var path2 = "a/b/c";

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
                    .And.HaveStdErrContaining($"Found architecture-specific install location path: '{path2}' ('{arch2}').")
                    .And.HaveStdErrContaining($"Found architecture-specific install location path matching the current OS architecture ('{arch2}'): '{path2}'.")
                    .And.HaveStdErrContaining($"Using global installation location [{path2}] as runtime location.");
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "This test targets the install_location config file which is only used on Linux and macOS.")]
        public void InstallLocationFile_OnlyFirstLineMayNotSpecifyArchitecture()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

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
            public TestProjectFixture PortableAppFixture { get; }
            public RepoDirectoriesProvider RepoDirectories { get; }
            public string InstallLocation { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();
                var fixture = new TestProjectFixture("PortableApp", RepoDirectories);
                fixture
                    .EnsureRestored()
                    .PublishProject();

                PortableAppFixture = fixture;
                BaseDirectory = Path.GetDirectoryName(PortableAppFixture.SdkDotnet.GreatestVersionHostFxrFilePath);
            }

            public void Dispose()
            {
                PortableAppFixture.Dispose();
            }
        }
    }
}
