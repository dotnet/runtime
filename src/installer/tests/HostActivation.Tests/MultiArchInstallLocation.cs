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
        public void EnvironmentVariable_CurrentArchitectureIsUsedIfEnvVarSet()
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
                .And.HaveUsedDotNetRootInstallLocation(fixture.BuiltDotnet.BinPath, fixture.CurrentRid, arch);
        }

        [Fact]
        public void EnvironmentVariable_IfNoArchSpecificEnvVarIsFoundDotnetRootIsUsed()
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
                .And.HaveUsedDotNetRootInstallLocation(fixture.BuiltDotnet.BinPath, fixture.CurrentRid);
        }

        [Fact]
        public void EnvironmentVariable_ArchSpecificDotnetRootIsUsedOverDotnetRoot()
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
                .And.HaveUsedDotNetRootInstallLocation(dotnet, fixture.CurrentRid, arch)
                .And.NotHaveStdErrContaining("Using environment variable DOTNET_ROOT=");
        }

        [Fact]
        public void EnvironmentVariable_DotNetRootIsUsedOverInstallLocationIfSet()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var arch = fixture.RepoDirProvider.BuildArchitecture.ToUpper();
            var dotnet = fixture.BuiltDotnet.BinPath;

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(appExe))
            {
                registeredInstallLocationOverride.SetInstallLocation((arch, "some/install/location"));

                Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .DotNetRoot(dotnet, arch)
                    .Execute()
                    .Should().Pass()
                    .And.HaveUsedDotNetRootInstallLocation(dotnet, fixture.CurrentRid, arch)
                    .And.NotHaveStdErrContaining("Using global install location");
            }
        }

        [Fact]
        public void EnvironmentVariable_DotnetRootPathDoesNotExist()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            using (TestOnlyProductBehavior.Enable(appExe))
            {
                Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot("non_existent_path")
                    .MultilevelLookup(false)
                    .EnvironmentVariable(
                        Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath,
                        sharedTestState.InstallLocation)
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdErrContaining("Did not find [DOTNET_ROOT] directory [non_existent_path]")
                    // If DOTNET_ROOT points to a folder that does not exist, we fall back to the global install path.
                    .And.HaveUsedGlobalInstallLocation(sharedTestState.InstallLocation)
                    .And.HaveStdOutContaining("Hello World");
            }
        }

        [Fact]
        public void EnvironmentVariable_DotnetRootPathExistsButHasNoHost()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var projDir = fixture.TestProject.ProjectDirectory;
            using (TestOnlyProductBehavior.Enable(appExe))
            {
                Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(projDir)
                .MultilevelLookup(false)
                .EnvironmentVariable(
                    Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath,
                    sharedTestState.InstallLocation)
                .Execute()
                .Should().Fail()
                .And.HaveUsedDotNetRootInstallLocation(projDir, fixture.CurrentRid)
                // If DOTNET_ROOT points to a folder that exists we assume that there's a dotnet installation in it
                .And.HaveStdErrContaining($"A fatal error occurred. The required library {RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform ("hostfxr")} could not be found.");
            }
        }

        [Fact]
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

                CommandResult result = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .DotNetRoot(null)
                    .Execute();

                if (!OperatingSystem.IsWindows())
                {
                    result.Should().HaveFoundDefaultInstallLocationInConfigFile(path1)
                        .And.HaveFoundArchSpecificInstallLocationInConfigFile(path1, arch1)
                        .And.HaveFoundArchSpecificInstallLocationInConfigFile(path2, arch2);
                }

                result.Should().HaveUsedGlobalInstallLocation(path2);
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
                    .Should().HaveFoundDefaultInstallLocationInConfigFile("a/b/c")
                    .And.HaveStdErrContaining($"Only the first line in '{registeredInstallLocationOverride.PathValueOverride}' may not have an architecture prefix.")
                    .And.HaveUsedConfigFileInstallLocation("a/b/c");
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "This test targets the install_location config file which is only used on Linux and macOS.")]
        public void InstallLocationFile_ReallyLongInstallPathIsParsedCorrectly()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(appExe))
            {
                var reallyLongPath =
                    "reallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreally" +
                    "reallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreally" +
                    "reallyreallyreallyreallyreallyreallyreallyreallyreallyreallylongpath";
                registeredInstallLocationOverride.SetInstallLocation((string.Empty, reallyLongPath));

                Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .DotNetRoot(null)
                    .Execute()
                    .Should().HaveFoundDefaultInstallLocationInConfigFile(reallyLongPath)
                    .And.HaveUsedConfigFileInstallLocation(reallyLongPath);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "This test targets the install_location config file which is only used on Linux and macOS.")]
        public void InstallLocationFile_MissingFile()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();

            var appExe = fixture.TestProject.AppExe;
            string testArtifactsPath = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "missingInstallLocation"));
            using (new TestArtifact(testArtifactsPath))
            using (var testOnlyProductBehavior = TestOnlyProductBehavior.Enable(appExe))
            {
                Directory.CreateDirectory(testArtifactsPath);

                string directory = Path.Combine(testArtifactsPath, "installLocationOverride");
                Directory.CreateDirectory(directory);
                string nonExistentLocationFile = Path.Combine(directory, "install_location");
                string defaultInstallLocation = Path.Combine(testArtifactsPath, "defaultInstallLocation");

                Command.Create(appExe)
                    .CaptureStdErr()
                    .EnvironmentVariable(
                        Constants.TestOnlyEnvironmentVariables.InstallLocationFilePath,
                        nonExistentLocationFile)
                    .EnvironmentVariable(
                        Constants.TestOnlyEnvironmentVariables.DefaultInstallPath,
                        defaultInstallLocation)
                    .DotNetRoot(null)
                    .Execute()
                    .Should().NotHaveStdErrContaining("The install_location file");
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
                    // App Host generation is turned off by default on macOS
                    .PublishProject(extraArgs: "/p:UseAppHost=true");

                PortableAppFixture = fixture;
                BaseDirectory = Path.GetDirectoryName(PortableAppFixture.SdkDotnet.GreatestVersionHostFxrFilePath);
                InstallLocation = fixture.BuiltDotnet.BinPath;
            }

            public void Dispose()
            {
                PortableAppFixture.Dispose();
            }
        }
    }
}
