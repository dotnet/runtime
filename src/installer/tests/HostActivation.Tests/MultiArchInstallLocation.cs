// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build;
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
                    .And.HaveStdErrContaining($"The required library {RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr")} could not be found.");
            }
        }

        [Fact]
        public void EnvironmentVariable_DotNetInfo_ListEnvironment()
        {
            var dotnet = new DotNetCli(sharedTestState.RepoDirectories.BuiltDotnet);

            var command = dotnet.Exec("--info")
                .CaptureStdOut();

            var envVars = new (string Architecture, string Path)[] {
                ("arm64", "/arm64/dotnet/root"),
                ("x64", "/x64/dotnet/root"),
                ("x86", "/x86/dotnet/root")
            };
            foreach(var envVar in envVars)
            {
                command = command.DotNetRoot(envVar.Path, envVar.Architecture);
            }

            string dotnetRootNoArch = "/dotnet/root";
            command = command.DotNetRoot(dotnetRootNoArch);

            (string Architecture, string Path) unknownEnvVar = ("unknown", "/unknown/dotnet/root");
            command = command.DotNetRoot(unknownEnvVar.Path, unknownEnvVar.Architecture);

            var result = command.Execute();
            result.Should().Pass()
                .And.HaveStdOutContaining("Environment variables:")
                .And.HaveStdOutMatching($@"{Constants.DotnetRoot.EnvironmentVariable}\s*\[{dotnetRootNoArch}\]")
                .And.NotHaveStdOutContaining($"{Constants.DotnetRoot.ArchitectureEnvironmentVariablePrefix}{unknownEnvVar.Architecture.ToUpper()}")
                .And.NotHaveStdOutContaining($"[{unknownEnvVar.Path}]");

            foreach ((string architecture, string path) in envVars)
            {
                result.Should()
                    .HaveStdOutMatching($@"{Constants.DotnetRoot.ArchitectureEnvironmentVariablePrefix}{architecture.ToUpper()}\s*\[{path}\]");
            }
        }

        [Fact]
        public void RegisteredInstallLocation_ArchSpecificLocationIsPickedFirst()
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
                    result.Should()
                        .HaveLookedForArchitectureSpecificInstallLocation(registeredInstallLocationOverride.PathValueOverride, arch2);
                }

                result.Should()
                    .HaveUsedRegisteredInstallLocation(path2)
                    .And.HaveUsedGlobalInstallLocation(path2);
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
                    .Should().HaveLookedForDefaultInstallLocation(registeredInstallLocationOverride.PathValueOverride)
                    .And.HaveUsedRegisteredInstallLocation(reallyLongPath);
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

                string installLocationDirectory = Path.Combine(testArtifactsPath, "installLocationOverride");
                Directory.CreateDirectory(installLocationDirectory);
                string defaultInstallLocation = Path.Combine(testArtifactsPath, "defaultInstallLocation");

                Command.Create(appExe)
                    .CaptureStdErr()
                    .EnvironmentVariable(
                        Constants.TestOnlyEnvironmentVariables.InstallLocationPath,
                        installLocationDirectory)
                    .EnvironmentVariable(
                        Constants.TestOnlyEnvironmentVariables.DefaultInstallPath,
                        defaultInstallLocation)
                    .DotNetRoot(null)
                    .Execute()
                    .Should().NotHaveStdErrContaining("The install_location file");
            }
        }

        [Fact]
        public void RegisteredInstallLocation_DotNetInfo_ListOtherArchitectures()
        {
            using (var testArtifact = new TestArtifact(SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "listOtherArchs"))))
            {
                var dotnet = new DotNetBuilder(testArtifact.Location, sharedTestState.RepoDirectories.BuiltDotnet, "exe").Build();
                using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(dotnet.GreatestVersionHostFxrFilePath))
                {
                    var installLocations = new (string, string)[] {
                        ("arm64", "/arm64/install/path"),
                        ("x64", "/x64/install/path"),
                        ("x86", "/x86/install/path")
                    };
                    (string Architecture, string Path) unknownArchInstall = ("unknown", "/unknown/install/path");
                    registeredInstallLocationOverride.SetInstallLocation(installLocations);
                    registeredInstallLocationOverride.SetInstallLocation(unknownArchInstall);

                    var result = dotnet.Exec("--info")
                        .CaptureStdOut()
                        .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                        .Execute();

                    result.Should().Pass()
                        .And.HaveStdOutContaining("Other architectures found:")
                        .And.NotHaveStdOutContaining(unknownArchInstall.Architecture)
                        .And.NotHaveStdOutContaining($"[{unknownArchInstall.Path}]");

                    string pathOverride = OperatingSystem.IsWindows() // Host uses short form of base key for Windows
                        ? registeredInstallLocationOverride.PathValueOverride.Replace(Microsoft.Win32.Registry.CurrentUser.Name, "HKCU")
                        : registeredInstallLocationOverride.PathValueOverride;
                    pathOverride = System.Text.RegularExpressions.Regex.Escape(pathOverride);
                    foreach ((string arch, string path) in installLocations)
                    {
                        if (arch == sharedTestState.RepoDirectories.BuildArchitecture)
                            continue;

                        result.Should()
                            .HaveStdOutMatching($@"{arch}\s*\[{path}\]\r?$\s*registered at \[{pathOverride}.*{arch}.*\]", System.Text.RegularExpressions.RegexOptions.Multiline);
                    }
                }
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
