// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;

using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace HostActivation.Tests
{
    public class SymbolicLinks : IClassFixture<SymbolicLinks.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public SymbolicLinks(SymbolicLinks.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        [InlineData ("a/b/SymlinkToApphost")]
        [InlineData ("a/SymlinkToApphost")]
        public void Run_apphost_behind_symlink(string symlinkRelativePath)
        {
            using (var testDir = TestArtifact.Create("symlink"))
            {
                Directory.CreateDirectory(Path.Combine(testDir.Location, Path.GetDirectoryName(symlinkRelativePath)));
                var symlinkFullPath = Path.Combine(testDir.Location, symlinkRelativePath);

                using var symlink = new SymLink(symlinkFullPath, sharedTestState.SelfContainedApp.AppExe);
                Command.Create(symlinkFullPath)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        [InlineData ("a/b/FirstSymlink", "c/d/SecondSymlink")]
        [InlineData ("a/b/FirstSymlink", "c/SecondSymlink")]
        [InlineData ("a/FirstSymlink", "c/d/SecondSymlink")]
        [InlineData ("a/FirstSymlink", "c/SecondSymlink")]
        public void Run_apphost_behind_transitive_symlinks(string firstSymlinkRelativePath, string secondSymlinkRelativePath)
        {
            using (var testDir = TestArtifact.Create("symlink"))
            {
                // second symlink -> apphost
                string symlink2Path = Path.Combine(testDir.Location, secondSymlinkRelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(symlink2Path));
                using var symlink2 = new SymLink(symlink2Path, sharedTestState.SelfContainedApp.AppExe);

                // first symlink -> second symlink
                string symlink1Path = Path.Combine(testDir.Location, firstSymlinkRelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(symlink1Path));
                using var symlink1 = new SymLink(symlink1Path, symlink2Path);

                Command.Create(symlink1.SrcPath)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
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

            using (var testDir = TestArtifact.Create("symlink"))
            {
                Directory.CreateDirectory(Path.Combine(testDir.Location, Path.GetDirectoryName(symlinkRelativePath)));

                using var symlink = new SymLink(Path.Combine(testDir.Location, symlinkRelativePath), sharedTestState.FrameworkDependentApp.AppExe);
                Command.Create(symlink.SrcPath)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
        }

        [Fact(Skip = "Currently failing in OSX with \"No such file or directory\" when running Command.Create. " +
                     "CI failing to use stat on symbolic links on Linux (permission denied).")]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Run_framework_dependent_app_with_runtime_behind_symlink()
        {
            using (var testDir = TestArtifact.Create("symlink"))
            {
                var dotnetSymlink = Path.Combine(testDir.Location, "dotnet");

                using var symlink = new SymLink(dotnetSymlink, TestContext.BuiltDotNet.BinPath);
                Command.Create(sharedTestState.FrameworkDependentApp.AppExe)
                    .EnvironmentVariable("DOTNET_ROOT", symlink.SrcPath)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Put_app_directory_behind_symlink()
        {
            var app = sharedTestState.SelfContainedApp.Copy();

            using (var newAppDir = TestArtifact.Create("PutTheBinDirSomewhereElse"))
            {
                Directory.Delete(newAppDir.Location);
                Directory.Move(app.Location, newAppDir.Location);

                using var symlink = new SymLink(app.Location, newAppDir.Location);
                Command.Create(app.AppExe)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Put_dotnet_behind_symlink()
        {
            using (var testDir = TestArtifact.Create("symlink"))
            {
                var dotnetSymlink = Path.Combine(testDir.Location, Binaries.DotNet.FileName);

                using var symlink = new SymLink(dotnetSymlink, TestContext.BuiltDotNet.DotnetExecutablePath);
                Command.Create(symlink.SrcPath, sharedTestState.SelfContainedApp.AppDll)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Put_app_directory_behind_symlink_and_use_dotnet()
        {
            var app = sharedTestState.SelfContainedApp.Copy();

            using (var newAppDir = TestArtifact.Create("PutTheBinDirSomewhereElse"))
            {
                Directory.Delete(newAppDir.Location);
                Directory.Move(app.Location, newAppDir.Location);

                using var symlink = new SymLink(app.Location, newAppDir.Location);
                TestContext.BuiltDotNet.Exec(app.AppDll)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Creating symbolic links requires administrative privilege on Windows, so skip test.")]
        public void Put_satellite_assembly_behind_symlink()
        {
            var app = sharedTestState.LocalizedApp.Copy();

            using (var satellitesDir = TestArtifact.Create("PutSatellitesSomewhereElse"))
            {
                var firstSatelliteDir = Directory.GetDirectories(app.Location).Single(dir => dir.Contains("kn-IN"));
                var firstSatelliteNewDir = Path.Combine(satellitesDir.Location, "kn-IN");
                Directory.Move(firstSatelliteDir, firstSatelliteNewDir);
                using var symlink1 = new SymLink(firstSatelliteDir, firstSatelliteNewDir);

                var secondSatelliteDir = Directory.GetDirectories(app.Location).Single(dir => dir.Contains("ta-IN"));
                var secondSatelliteNewDir = Path.Combine(satellitesDir.Location, "ta-IN");
                Directory.Move(secondSatelliteDir, secondSatelliteNewDir);
                using var symlink2 = new SymLink(secondSatelliteDir, secondSatelliteNewDir);

                Command.Create(app.AppExe)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("[kn-IN]! [ta-IN]! [default]!");
            }
        }

        public class SharedTestState : IDisposable
        {
            public TestApp FrameworkDependentApp { get; }
            public TestApp SelfContainedApp { get; }
            public TestApp LocalizedApp { get; }

            public SharedTestState()
            {
                FrameworkDependentApp = TestApp.CreateFromBuiltAssets("HelloWorld");
                FrameworkDependentApp.CreateAppHost();

                SelfContainedApp = TestApp.CreateFromBuiltAssets("HelloWorld");
                SelfContainedApp.PopulateSelfContained(TestApp.MockedComponent.None);
                SelfContainedApp.CreateAppHost();

                LocalizedApp = TestApp.CreateFromBuiltAssets("LocalizedApp");
                LocalizedApp.PopulateSelfContained(TestApp.MockedComponent.None);
                LocalizedApp.CreateAppHost();
            }

            public void Dispose()
            {
                FrameworkDependentApp?.Dispose();
                SelfContainedApp?.Dispose();
                LocalizedApp?.Dispose();
            }
        }
    }
}
