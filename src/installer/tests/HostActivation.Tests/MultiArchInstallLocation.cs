// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.CoreSetup.Test.HostActivation;
using Xunit;

namespace HostActivation.Tests
{
    public class MultiArchInstallLocation : IClassFixture<MultiArchInstallLocation.SharedTestState>
    {
        private string InstallLocationFile => Path.Combine(sharedTestState.InstallLocation, "install_location");

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
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable($"DOTNET_ROOT_{arch}", fixture.SdkDotnet.BinPath)
                .CaptureStdErr()
                .Execute()
                .Should().HaveStdErrContaining($"DOTNET_ROOT_{arch}");
        }

        [Fact]
        public void GlobalInstallation_IfNoArchSpecificEnvVarIsFoundDotnetRootIsUed()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().HaveStdErrContaining($"DOTNET_ROOT");
        }

        [Fact]
        public void GlobalInstallation_ArchSpecificDotnetRootIsUsedOverDotnetRoot()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var arch = fixture.RepoDirProvider.BuildArchitecture.ToUpper();
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable("DOTNET_ROOT", "non_existent_path")
                .EnvironmentVariable($"DOTNET_ROOT_{arch}", fixture.SdkDotnet.BinPath)
                .Execute()
                .Should().HaveStdErrContaining($"DOTNET_ROOT_{arch}")
                .And.NotHaveStdErrContaining("[DOTNET_ROOT] directory");
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "This test targets the install_location config file which is only used on Linux and macOS.")]
        public void InstallLocationFile_ArchSpecificLocationIsPickedFirst()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(InstallLocationFile))
            {
                registeredInstallLocationOverride.SetInstallLocation(("x/y/z", fixture.RepoDirProvider.BuildArchitecture));
                dotnet.Exec(appDll)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.DefaultInstallPath, InstallLocationFile)
                    .DotNetRoot(null)
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdErrContaining("");
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
                    .BuildProject();

                PortableAppFixture = fixture;
                BaseDirectory = Path.GetDirectoryName(PortableAppFixture.SdkDotnet.GreatestVersionHostFxrFilePath);
                var install_location_dir = Path.Combine(fixture.TestProject.Location, "install_location");
                Directory.CreateDirectory(install_location_dir);
                InstallLocation = install_location_dir;
            }

            public void Dispose()
            {
                PortableAppFixture.Dispose();
            }
        }
    }
}
