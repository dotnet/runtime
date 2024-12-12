// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.CoreSetup.Test.HostActivation;
using Microsoft.NET.HostModel.AppHost;
using Xunit;
using static Microsoft.NET.HostModel.AppHost.HostWriter.DotNetSearchOptions;

namespace HostActivation.Tests
{
    public class InstallLocation : IClassFixture<InstallLocation.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public InstallLocation(SharedTestState fixture)
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
                .And.HaveUsedDotNetRootInstallLocation(TestContext.BuiltDotNet.BinPath, TestContext.BuildRID, arch);
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
                .And.HaveUsedDotNetRootInstallLocation(TestContext.BuiltDotNet.BinPath, TestContext.BuildRID);
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
                .And.HaveUsedDotNetRootInstallLocation(dotnet, TestContext.BuildRID, arch)
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
                    .And.HaveUsedDotNetRootInstallLocation(dotnet, TestContext.BuildRID, arch)
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
                    .And.HaveUsedDotNetRootInstallLocation(app.Location, TestContext.BuildRID)
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
            using (var testArtifact = TestArtifact.Create("missingInstallLocation"))
            using (var testOnlyProductBehavior = TestOnlyProductBehavior.Enable(app.AppExe))
            {
                string installLocationDirectory = Path.Combine(testArtifact.Location, "installLocationOverride");
                Directory.CreateDirectory(installLocationDirectory);
                string defaultInstallLocation = Path.Combine(testArtifact.Location, "defaultInstallLocation");

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
            using (var testArtifact = TestArtifact.Create("listOtherArchs"))
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

        [Theory]
        [InlineData(SearchLocation.AppLocal)]
        [InlineData(SearchLocation.AppRelative)]
        [InlineData(SearchLocation.EnvironmentVariable)]
        [InlineData(SearchLocation.Global)]
        public void SearchOptions(SearchLocation searchLocation)
        {
            TestApp app = sharedTestState.App.Copy();

            if (searchLocation == SearchLocation.AppLocal)
            {
                // Copy a mock hostfxr to imitate an app-local install
                File.Copy(Binaries.HostFxr.FilePath, Path.Combine(app.Location, Binaries.HostFxr.FileName));
            }

            // Create directories for all install locations
            string appLocalLocation = $"{app.Location}{Path.DirectorySeparatorChar}";
            string appRelativeLocation = Directory.CreateDirectory(Path.Combine(app.Location, "rel")).FullName;
            string envLocation = Directory.CreateDirectory(Path.Combine(app.Location, "env")).FullName;
            string globalLocation = Directory.CreateDirectory(Path.Combine(app.Location, "global")).FullName;

            app.CreateAppHost(dotNetRootOptions: new HostWriter.DotNetSearchOptions()
            {
                Location = searchLocation,
                AppRelativeDotNet = Path.GetRelativePath(app.Location, appRelativeLocation)
            });
            CommandResult result;
            using (var installOverride = new RegisteredInstallLocationOverride(app.AppExe))
            {
                installOverride.SetInstallLocation([(TestContext.BuildArchitecture, globalLocation)]);
                result = Command.Create(app.AppExe)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(installOverride)
                    .DotNetRoot(envLocation)
                    .Execute();
            }

            switch (searchLocation)
            {
                case SearchLocation.AppLocal:
                    result.Should().HaveUsedAppLocalInstallLocation(appLocalLocation);
                    break;
                case SearchLocation.AppRelative:
                    result.Should().HaveUsedAppRelativeInstallLocation(appRelativeLocation);
                    break;
                case SearchLocation.EnvironmentVariable:
                    result.Should().HaveUsedDotNetRootInstallLocation(envLocation, TestContext.BuildRID);
                    break;
                case SearchLocation.Global:
                    result.Should().HaveUsedGlobalInstallLocation(globalLocation);
                    break;
            }
        }

        [Fact]
        public void AppHost_AppRelative_MissingPath()
        {
            TestApp app = sharedTestState.App.Copy();
            app.CreateAppHost(dotNetRootOptions: new HostWriter.DotNetSearchOptions()
            {
                Location = SearchLocation.AppRelative
            });
            Command.Create(app.AppExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("The app-relative .NET path is not embedded.")
                .And.ExitWith(Constants.ErrorCode.AppHostExeNotBoundFailure);
        }

        [Theory]
        [InlineData("./dir")]
        [InlineData("../dir")]
        [InlineData("..\\dir")]
        [InlineData("dir1/dir2")]
        [InlineData("dir1\\dir2")]
        public void SearchOptions_AppRelative_PathVariations(string relativePath)
        {
            TestApp app = sharedTestState.App.Copy();
            string installLocation = Path.Combine(app.Location, relativePath);
            Directory.CreateDirectory(installLocation);
            using (var testArtifact = new TestArtifact(installLocation))
            {
                app.CreateAppHost(dotNetRootOptions: new HostWriter.DotNetSearchOptions()
                {
                    Location = HostWriter.DotNetSearchOptions.SearchLocation.AppRelative,
                    AppRelativeDotNet = relativePath
                });
                Command.Create(app.AppExe)
                    .EnableTracingAndCaptureOutputs()
                    .Execute()
                    .Should().HaveUsedAppRelativeInstallLocation(Path.GetFullPath(installLocation));
            }
        }

        [Theory]
        [InlineData(SearchLocation.AppLocal)]
        [InlineData(SearchLocation.AppRelative)]
        [InlineData(SearchLocation.EnvironmentVariable)]
        [InlineData(SearchLocation.Global)]
        public void SearchOptions_Precedence(SearchLocation expectedResult)
        {
            TestApp app = sharedTestState.App.Copy();

            // Create directories for the install locations for the expected result and for those that should
            // have a lower precedence than the expected result (they should not be used)
            string appLocalLocation = $"{app.Location}{Path.DirectorySeparatorChar}";
            if (expectedResult == SearchLocation.AppLocal)
                File.Copy(Binaries.HostFxr.FilePath, Path.Combine(app.Location, Binaries.HostFxr.FileName));

            string appRelativeLocation = Path.Combine(app.Location, "rel");
            if (expectedResult <= SearchLocation.AppRelative)
                Directory.CreateDirectory(appRelativeLocation);

            string envLocation = Path.Combine(app.Location, "env");
            if (expectedResult <= SearchLocation.EnvironmentVariable)
                Directory.CreateDirectory(envLocation);

            string globalLocation = Path.Combine(app.Location, "global");
            if (expectedResult <= SearchLocation.Global)
                Directory.CreateDirectory(globalLocation);

            app.CreateAppHost(dotNetRootOptions: new HostWriter.DotNetSearchOptions()
            {
                // Search all locations
                Location = SearchLocation.AppLocal | SearchLocation.AppRelative | SearchLocation.EnvironmentVariable | SearchLocation.Global,
                AppRelativeDotNet = Path.GetRelativePath(app.Location, appRelativeLocation)
            });
            CommandResult result;
            using (var installOverride = new RegisteredInstallLocationOverride(app.AppExe))
            {
                installOverride.SetInstallLocation([(TestContext.BuildArchitecture, globalLocation)]);
                result = Command.Create(app.AppExe)
                    .EnableTracingAndCaptureOutputs()
                    .ApplyRegisteredInstallLocationOverride(installOverride)
                    .DotNetRoot(envLocation)
                    .Execute();
            }

            switch (expectedResult)
            {
                case SearchLocation.AppLocal:
                    result.Should().HaveUsedAppLocalInstallLocation(appLocalLocation);
                    break;
                case SearchLocation.AppRelative:
                    result.Should().HaveUsedAppRelativeInstallLocation(appRelativeLocation);
                    break;
                case SearchLocation.EnvironmentVariable:
                    result.Should().HaveUsedDotNetRootInstallLocation(envLocation, TestContext.BuildRID);
                    break;
                case SearchLocation.Global:
                    result.Should().HaveUsedGlobalInstallLocation(globalLocation);
                    break;
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
