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
            var arch = TestContext.BuildArchitecture.ToUpper();
            Command.Create(sharedTestState.App.AppExe)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(TestContext.BuiltDotNet.BinPath, arch)
                .Execute()
                .Should().Pass()
                .And.HaveUsedDotNetRootInstallLocation(TestContext.BuiltDotNet.BinPath, TestContext.TargetRID, arch);
        }

        [Fact]
        public void EnvironmentVariable_IfNoArchSpecificEnvVarIsFoundDotnetRootIsUsed()
        {
            var arch = TestContext.BuildArchitecture.ToUpper();
            Command.Create(sharedTestState.App.AppExe)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                .Execute()
                .Should().Pass()
                .And.HaveUsedDotNetRootInstallLocation(TestContext.BuiltDotNet.BinPath, TestContext.TargetRID);
        }

        [Fact]
        public void EnvironmentVariable_ArchSpecificDotnetRootIsUsedOverDotnetRoot()
        {
            var arch = TestContext.BuildArchitecture.ToUpper();
            var dotnet = TestContext.BuiltDotNet.BinPath;
            Command.Create(sharedTestState.App.AppExe)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot("non_existent_path")
                .DotNetRoot(dotnet, arch)
                .Execute()
                .Should().Pass()
                .And.HaveUsedDotNetRootInstallLocation(dotnet, TestContext.TargetRID, arch)
                .And.NotHaveStdErrContaining("Using environment variable DOTNET_ROOT=");
        }

        [Fact]
        public void EnvironmentVariable_DotNetRootIsUsedOverInstallLocationIfSet()
        {
            var app = sharedTestState.App.Copy();
            var appExe = app.AppExe;
            var arch = TestContext.BuildArchitecture.ToUpper();
            var dotnet = TestContext.BuiltDotNet.BinPath;

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(appExe))
            {
                registeredInstallLocationOverride.SetInstallLocation((arch, "some/install/location"));

                Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .DotNetRoot(dotnet, arch)
                    .Execute()
                    .Should().Pass()
                    .And.HaveUsedDotNetRootInstallLocation(dotnet, TestContext.TargetRID, arch)
                    .And.NotHaveStdErrContaining("Using global install location");
            }
        }

        [Fact]
        public void EnvironmentVariable_DotnetRootPathDoesNotExist()
        {
            var app = sharedTestState.App.Copy();
            using (TestOnlyProductBehavior.Enable(app.AppExe))
            {
                Command.Create(app.AppExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot("non_existent_path")
                    .MultilevelLookup(false)
                    .EnvironmentVariable(
                        Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath,
                        TestContext.BuiltDotNet.BinPath)
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdErrContaining("Did not find [DOTNET_ROOT] directory [non_existent_path]")
                    // If DOTNET_ROOT points to a folder that does not exist, we fall back to the global install path.
                    .And.HaveUsedGlobalInstallLocation(TestContext.BuiltDotNet.BinPath)
                    .And.HaveStdOutContaining("Hello World");
            }
        }

        [Fact]
        public void EnvironmentVariable_DotnetRootPathExistsButHasNoHost()
        {
            var app = sharedTestState.App.Copy();
            using (TestOnlyProductBehavior.Enable(app.AppExe))
            {
                Command.Create(app.AppExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(app.Location)
                    .MultilevelLookup(false)
                    .EnvironmentVariable(
                        Constants.TestOnlyEnvironmentVariables.GloballyRegisteredPath,
                        TestContext.BuiltDotNet.BinPath)
                    .Execute()
                    .Should().Fail()
                    .And.HaveUsedDotNetRootInstallLocation(app.Location, TestContext.TargetRID)
                    // If DOTNET_ROOT points to a folder that exists we assume that there's a dotnet installation in it
                    .And.HaveStdErrContaining($"The required library {Binaries.HostFxr.FileName} could not be found.");
            }
        }

        [Fact]
        public void EnvironmentVariable_DotNetInfo_ListEnvironment()
        {
            var command = TestContext.BuiltDotNet.Exec("--info")
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
            var app = sharedTestState.App.Copy();
            var arch1 = "someArch";
            var path1 = "x/y/z";
            var arch2 = TestContext.BuildArchitecture;
            var path2 = "a/b/c";

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(app.AppExe))
            {
                registeredInstallLocationOverride.SetInstallLocation(new (string, string)[] {
                    (string.Empty, path1),
                    (arch1, path1),
                    (arch2, path2)
                });

                CommandResult result = Command.Create(app.AppExe)
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
            var app = sharedTestState.App.Copy();
            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(app.AppExe))
            {
                var reallyLongPath =
                    "reallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreally" +
                    "reallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreallyreally" +
                    "reallyreallyreallyreallyreallyreallyreallyreallyreallyreallylongpath";
                registeredInstallLocationOverride.SetInstallLocation((string.Empty, reallyLongPath));

                Command.Create(app.AppExe)
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
            var app = sharedTestState.App.Copy();
            string testArtifactsPath = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "missingInstallLocation"));
            using (new TestArtifact(testArtifactsPath))
            using (var testOnlyProductBehavior = TestOnlyProductBehavior.Enable(app.AppExe))
            {
                Directory.CreateDirectory(testArtifactsPath);

                string installLocationDirectory = Path.Combine(testArtifactsPath, "installLocationOverride");
                Directory.CreateDirectory(installLocationDirectory);
                string defaultInstallLocation = Path.Combine(testArtifactsPath, "defaultInstallLocation");

                Command.Create(app.AppExe)
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
                var dotnet = new DotNetBuilder(testArtifact.Location, TestContext.BuiltDotNet.BinPath, "exe").Build();
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
                        if (arch == TestContext.BuildArchitecture)
                            continue;

                        result.Should()
                            .HaveStdOutMatching($@"{arch}\s*\[{path}\]\r?$\s*registered at \[{pathOverride}.*{arch}.*\]", System.Text.RegularExpressions.RegexOptions.Multiline);
                    }
                }
            }
        }

        public class SharedTestState : IDisposable
        {
            public TestApp App { get; }

            public SharedTestState()
            {
                App = TestApp.CreateFromBuiltAssets("HelloWorld");
                App.CreateAppHost();
            }

            public void Dispose()
            {
                App?.Dispose();
            }
        }
    }
}
