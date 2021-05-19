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

        public AppHostUsedWithSymbolicLinks(AppHostUsedWithSymbolicLinks.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        [InlineData ("a/b/SymlinkToApphost")]
        [InlineData ("a/SymlinkToApphost")]
        public void Run_apphost_behind_symlink(string symlinkRelativePath)
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var testDir = Directory.GetParent(fixture.TestProject.Location).ToString();
            Directory.CreateDirectory(Path.Combine(testDir, Path.GetDirectoryName(symlinkRelativePath)));
            var symlinkFullPath = Path.Combine(testDir, symlinkRelativePath);

            using var symlink = new SymLink(symlinkFullPath, appExe);
            Command.Create(symlinkFullPath)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        [InlineData ("a/b/FirstSymlink", "c/d/SecondSymlink")]
        [InlineData ("a/b/FirstSymlink", "c/SecondSymlink")]
        [InlineData ("a/FirstSymlink", "c/d/SecondSymlink")]
        [InlineData ("a/FirstSymlink", "c/SecondSymlink")]
        public void Run_apphost_behind_transitive_symlinks(string firstSymlinkRelativePath, string secondSymlinkRelativePath)
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var testDir = Directory.GetParent(fixture.TestProject.Location).ToString();

            // second symlink -> apphost
            string symlink2Path = Path.Combine(testDir, secondSymlinkRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(symlink2Path));
            using var symlink2 = new SymLink(symlink2Path, appExe);

            // first symlink -> second symlink
            string symlink1Path = Path.Combine(testDir, firstSymlinkRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(symlink1Path));
            using var symlink1 = new SymLink(symlink1Path, symlink2Path);

            Command.Create(symlink1.SrcPath)
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
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Run_framework_dependent_app_behind_symlink(/*string symlinkRelativePath*/)
        {
            var symlinkRelativePath = string.Empty;

            var fixture = sharedTestState.FrameworkDependentAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var builtDotnet = fixture.BuiltDotnet.BinPath;
            var testDir = Directory.GetParent(fixture.TestProject.Location).ToString();
            Directory.CreateDirectory(Path.Combine(testDir, Path.GetDirectoryName(symlinkRelativePath)));

            using var symlink = new SymLink(Path.Combine(testDir, symlinkRelativePath), appExe);
            Command.Create(symlink.SrcPath)
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
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Run_framework_dependent_app_with_runtime_behind_symlink()
        {
            var fixture = sharedTestState.FrameworkDependentAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var testDir = Directory.GetParent(fixture.TestProject.Location).ToString();
            var dotnetSymlink = Path.Combine(testDir, "dotnet");
            var dotnetDir = fixture.BuiltDotnet.BinPath;

            using var symlink = new SymLink(dotnetSymlink, dotnetDir);
            Command.Create(appExe)
                .EnvironmentVariable("DOTNET_ROOT", symlink.SrcPath)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Put_app_directory_behind_symlink()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var binDir = fixture.TestProject.OutputDirectory;
            var binDirNewPath = Path.Combine(Directory.GetParent(fixture.TestProject.Location).ToString(), "PutTheBinDirSomewhereElse");
            Directory.Move(binDir, binDirNewPath);

            using var symlink = new SymLink(binDir, binDirNewPath);
            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Put_dotnet_behind_symlink()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appDll = fixture.TestProject.AppDll;
            var dotnetExe = fixture.BuiltDotnet.DotnetExecutablePath;
            var testDir = Directory.GetParent(fixture.TestProject.Location).ToString();
            var dotnetSymlink = Path.Combine(testDir, "dotnet");

            using var symlink = new SymLink(dotnetSymlink, dotnetExe);
            Command.Create(symlink.SrcPath, fixture.TestProject.AppDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Put_app_directory_behind_symlink_and_use_dotnet()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var binDir = fixture.TestProject.OutputDirectory;
            var binDirNewPath = Path.Combine(Directory.GetParent(fixture.TestProject.Location).ToString(), "PutTheBinDirSomewhereElse");
            Directory.Move(binDir, binDirNewPath);

            using var symlink = new SymLink(binDir, binDirNewPath);
            dotnet.Exec(fixture.TestProject.AppDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Put_app_directory_behind_symlink_and_use_dotnet_run()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var dotnet = fixture.SdkDotnet;
            var binDir = fixture.TestProject.OutputDirectory;
            var binDirNewPath = Path.Combine(Directory.GetParent(fixture.TestProject.Location).ToString(), "PutTheBinDirSomewhereElse");
            Directory.Move(binDir, binDirNewPath);

            using var symlink = new SymLink(binDir, binDirNewPath);
            dotnet.Exec("run")
                .WorkingDirectory(fixture.TestProject.Location)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        // If enabled, this tests will need to set the console code page to output unicode characters: Command.Create("chcp 65001").Execute();
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Put_satellite_assembly_behind_symlink()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Localized
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var binDir = fixture.TestProject.OutputDirectory;
            var satellitesDir = Path.Combine(Directory.GetParent(fixture.TestProject.Location).ToString(), "PutSatellitesSomewhereElse");
            Directory.CreateDirectory(satellitesDir);

            var firstSatelliteDir = Directory.GetDirectories(binDir).Single(dir => dir.Contains("kn-IN"));
            var firstSatelliteNewDir = Path.Combine(satellitesDir, "kn-IN");
            Directory.Move(firstSatelliteDir, firstSatelliteNewDir);
            using var symlink1 = new SymLink(firstSatelliteDir, firstSatelliteNewDir);

            var secondSatelliteDir = Directory.GetDirectories(binDir).Single(dir => dir.Contains("ta-IN"));
            var secondSatelliteNewDir = Path.Combine(satellitesDir, "ta-IN");
            Directory.Move(secondSatelliteDir, secondSatelliteNewDir);
            using var symlink2 = new SymLink(secondSatelliteDir, secondSatelliteNewDir);

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
