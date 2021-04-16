// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.NET.HostModel.Tests
{
    public class AppHostUsedWithSymbolicLinks : IClassFixture<AppHostUsedWithSymbolicLinks.SharedTestState>
    {
        private SharedTestState sharedTestState;

        private void CreateSymbolicLink(string source, string target)
        {
            if (!SymbolicLinking.MakeSymbolicLink(source, target, out var errorString))
                throw new Exception($"Failed to create symbolic link '{source}' targeting: '{target}': {errorString}");
        }

        public AppHostUsedWithSymbolicLinks(AppHostUsedWithSymbolicLinks.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Theory]
        [InlineData ("a/b/SymlinkToApphost")]
        [InlineData ("a/SymlinkToApphost")]
        public void Run_apphost_behind_symlink(string symlinkRelativePath)
        {
            // Creating symbolic links requires administrative privilege on Windows, so skip test.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var testDir = Directory.GetParent(fixture.TestProject.Location).ToString();
            Directory.CreateDirectory(Path.Combine(testDir, Path.GetDirectoryName(symlinkRelativePath)));
            var symlinkFullPath = Path.Combine(testDir, symlinkRelativePath);

            CreateSymbolicLink(symlinkFullPath, appExe);
            Command.Create(symlinkFullPath)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Theory]
        [InlineData ("a/b/FirstSymlink", "c/d/SecondSymlink")]
        [InlineData ("a/b/FirstSymlink", "c/SecondSymlink")]
        [InlineData ("a/FirstSymlink", "c/d/SecondSymlink")]
        [InlineData ("a/FirstSymlink", "c/SecondSymlink")]
        public void Run_apphost_behind_transitive_symlinks(string firstSymlinkRelativePath, string secondSymlinkRelativePath)
        {
            // Creating symbolic links requires administrative privilege on Windows, so skip test.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();
            
            var appExe = fixture.TestProject.AppExe;
            var testDir = Directory.GetParent(fixture.TestProject.Location).ToString();

            // second symlink -> apphost
            string secondSymbolicLink = Path.Combine(testDir, secondSymlinkRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(secondSymbolicLink));
            CreateSymbolicLink(secondSymbolicLink, appExe);

            // first symlink -> second symlink
            string firstSymbolicLink = Path.Combine(testDir, firstSymlinkRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(firstSymbolicLink));
            CreateSymbolicLink(firstSymbolicLink, secondSymbolicLink);

            Command.Create(firstSymbolicLink)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        //[Theory]
        //[InlineData("a/b/SymlinkToFrameworkDependentApp")]
        //[InlineData("a/SymlinkToFrameworkDependentApp")]
        [Fact(Skip = "Currently failing in OSX with \"No such file or directory\" when running Command.Create. " +
            "CI failing to use stat on symbolic links on Linux (permission denied).")]
        public void Run_framework_dependent_app_behind_symlink(/* string symlinkRelativePath */)
        {
            // Creating symbolic links requires administrative privilege on Windows, so skip test.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            string symlinkRelativePath = string.Empty;

            var fixture = sharedTestState.FrameworkDependentAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var builtDotnet = fixture.BuiltDotnet.BinPath;
            var testDir = Directory.GetParent(fixture.TestProject.Location).ToString();
            Directory.CreateDirectory(Path.Combine(testDir, Path.GetDirectoryName(symlinkRelativePath)));
            var symlinkFullPath = Path.Combine(testDir, symlinkRelativePath);

            CreateSymbolicLink(symlinkFullPath, appExe);
            Command.Create(symlinkFullPath)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("DOTNET_ROOT", builtDotnet)
                .EnvironmentVariable("DOTNET_ROOT(x86)", builtDotnet)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact(Skip = "Currently failing in OSX with \"No such file or directory\" when running Command.Create. " +
            "CI failing to use stat on symbolic links on Linux (permission denied).")]
        public void Run_framework_dependent_app_with_runtime_behind_symlink()
        {
            // Creating symbolic links requires administrative privilege on Windows, so skip test.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var fixture = sharedTestState.FrameworkDependentAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var testDir = Directory.GetParent(fixture.TestProject.Location).ToString();
            var dotnetSymlink = Path.Combine(testDir, "dotnet");
            var dotnetDir = fixture.BuiltDotnet.BinPath;

            CreateSymbolicLink(dotnetSymlink, dotnetDir);
            Command.Create(appExe)
                .EnvironmentVariable("DOTNET_ROOT", dotnetSymlink)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Put_app_directory_behind_symlink()
        {
            // Creating symbolic links requires administrative privilege on Windows, so skip test.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var binDir = fixture.TestProject.OutputDirectory;
            var binDirNewPath = Path.Combine(Directory.GetParent(fixture.TestProject.Location).ToString(), "PutTheBinDirSomewhereElse");
            Directory.Move(binDir, binDirNewPath);

            CreateSymbolicLink(binDir, binDirNewPath);
            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Put_dotnet_behind_symlink()
        {
            // Creating symbolic links requires administrative privilege on Windows, so skip test.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appDll = fixture.TestProject.AppDll;
            var dotnetExe = fixture.BuiltDotnet.DotnetExecutablePath;
            var testDir = Directory.GetParent(fixture.TestProject.Location).ToString();
            var dotnetSymlink = Path.Combine(testDir, "dotnet");

            CreateSymbolicLink(dotnetSymlink, dotnetExe);
            Command.Create(dotnetSymlink, fixture.TestProject.AppDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Put_app_directory_behind_symlink_and_use_dotnet()
        {
            // Creating symbolic links requires administrative privilege on Windows, so skip test.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var binDir = fixture.TestProject.OutputDirectory;
            var binDirNewPath = Path.Combine(Directory.GetParent(fixture.TestProject.Location).ToString(), "PutTheBinDirSomewhereElse");
            Directory.Move(binDir, binDirNewPath);

            CreateSymbolicLink(binDir, binDirNewPath);
            dotnet.Exec(fixture.TestProject.AppDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Put_app_directory_behind_symlink_and_use_dotnet_run()
        {
            // Creating symbolic links requires administrative privilege on Windows, so skip test.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var dotnet = fixture.SdkDotnet;
            var binDir = fixture.TestProject.OutputDirectory;
            var binDirNewPath = Path.Combine(Directory.GetParent(fixture.TestProject.Location).ToString(), "PutTheBinDirSomewhereElse"); 
            Directory.Move(binDir, binDirNewPath);

            CreateSymbolicLink(binDir, binDirNewPath);
            dotnet.Exec("run")
                .WorkingDirectory(fixture.TestProject.Location)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Put_satellite_assembly_behind_symlink()
        {
            // Creating symbolic links requires administrative privilege on Windows, so skip test.
            // If enabled, this tests will need to set the console code page to output unicode characters:
            // Command.Create("chcp 65001").Execute();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var fixture = sharedTestState.StandaloneAppFixture_Localized
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var binDir = fixture.TestProject.OutputDirectory;
            var satellitesDir = Path.Combine(Directory.GetParent(fixture.TestProject.Location).ToString(), "PutSatellitesSomewhereElse");
            Directory.CreateDirectory(satellitesDir);

            var firstSatelliteDir = Directory.GetDirectories(binDir).Single(dir => dir.Contains("kn-IN"));
            var firstSatelliteNewDir = Path.Combine(satellitesDir, "kn-IN");
            Directory.Move(firstSatelliteDir, firstSatelliteNewDir);
            CreateSymbolicLink(firstSatelliteDir, firstSatelliteNewDir);

            var secondSatelliteDir = Directory.GetDirectories(binDir).Single(dir => dir.Contains("ta-IN"));
            var secondSatelliteNewDir = Path.Combine(satellitesDir, "ta-IN");
            Directory.Move(secondSatelliteDir, secondSatelliteNewDir);
            CreateSymbolicLink(secondSatelliteDir, secondSatelliteNewDir);

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("ನಮಸ್ಕಾರ! வணக்கம்! Hello!");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture StandaloneAppFixture_Localized { get; }
            public TestProjectFixture StandaloneAppFixture_Published { get; }
            public TestProjectFixture FrameworkDependentAppFixture_Published { get; }
            public RepoDirectoriesProvider RepoDirectories { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                var localizedFixture = new TestProjectFixture("LocalizedApp", RepoDirectories);
                localizedFixture
                    .EnsureRestoredForRid(localizedFixture.CurrentRid)
                    .PublishProject(runtime: localizedFixture.CurrentRid);

                var publishFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
                publishFixture
                    .EnsureRestoredForRid(publishFixture.CurrentRid)
                    .PublishProject(runtime: publishFixture.CurrentRid);

                var fwPublishedFixture = new TestProjectFixture("PortableApp", RepoDirectories);
                fwPublishedFixture
                    .EnsureRestored()
                    .PublishProject();

                StandaloneAppFixture_Localized = localizedFixture;
                StandaloneAppFixture_Published = publishFixture;
                FrameworkDependentAppFixture_Published = fwPublishedFixture;
            }

            public void Dispose()
            {
                StandaloneAppFixture_Localized.Dispose();
                StandaloneAppFixture_Published.Dispose();
                FrameworkDependentAppFixture_Published.Dispose();
            }
        }
    }
}
