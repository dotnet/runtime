// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
        [InlineData("a/b/SymlinkToFrameworkDependentApp")]
        [InlineData("a/SymlinkToFrameworkDependentApp")]
        public void Symlink_all_files_fx(string symlinkRelativePath)
        {
            using var testDir = TestArtifact.Create("symlink");
            Directory.CreateDirectory(Path.Combine(testDir.Location, Path.GetDirectoryName(symlinkRelativePath)));

            // Symlink every file in the app directory
            var symlinks = new List<SymLink>();
            try
            {
                foreach (var file in Directory.EnumerateFiles(sharedTestState.FrameworkDependentApp.Location))
                {
                    var fileName = Path.GetFileName(file);
                    var symlinkPath = Path.Combine(testDir.Location, symlinkRelativePath, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(symlinkPath));
                    symlinks.Add(new SymLink(symlinkPath, file));
                }

                var result = Command.Create(Path.Combine(testDir.Location, symlinkRelativePath, Path.GetFileName(sharedTestState.FrameworkDependentApp.AppExe)))
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                    .Execute();

                // This should succeed on all platforms, but for different reasons:
                // * Windows: The apphost will look next to the symlink for the app dll and find the symlinked dll
                // * Unix: The apphost will look next to the resolved apphost for the app dll and find the real thing
                result
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
            finally
            {
                foreach (var symlink in symlinks)
                {
                    symlink.Dispose();
                }
            }
        }

        [Theory]
        [InlineData("a/b/SymlinkToFrameworkDependentApp")]
        [InlineData("a/SymlinkToFrameworkDependentApp")]
        public void Symlink_split_files_fx(string symlinkRelativePath)
        {
            using var testDir = TestArtifact.Create("symlink");

            // Split the app into two directories, one for the apphost and one for the rest of the files
            var appHostDir = Path.Combine(testDir.Location, "apphost");
            var appFilesDir = Path.Combine(testDir.Location, "appfiles");
            Directory.CreateDirectory(appHostDir);
            Directory.CreateDirectory(appFilesDir);

            var appHostName = Path.GetFileName(sharedTestState.FrameworkDependentApp.AppExe);

            File.Copy(
                sharedTestState.FrameworkDependentApp.AppExe,
                Path.Combine(appHostDir, appHostName));

            foreach (var file in Directory.EnumerateFiles(sharedTestState.FrameworkDependentApp.Location))
            {
                var fileName = Path.GetFileName(file);
                if (fileName != appHostName)
                {
                    File.Copy(file, Path.Combine(appFilesDir, fileName));
                }
            }

            // Symlink all of the above into a single directory
            var targetPath = Path.Combine(testDir.Location, symlinkRelativePath);
            Directory.CreateDirectory(targetPath);
            var symlinks = new List<SymLink>();
            try
            {
                foreach (var file in Directory.EnumerateFiles(appFilesDir))
                {
                    var fileName = Path.GetFileName(file);
                    var symlinkPath = Path.Combine(targetPath, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(symlinkPath));
                    symlinks.Add(new SymLink(symlinkPath, file));
                }
                symlinks.Add(new SymLink(
                    Path.Combine(targetPath, appHostName),
                    Path.Combine(appHostDir, appHostName)));

                var result = Command.Create(Path.Combine(targetPath, appHostName))
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                    .Execute();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // On Windows, the apphost will look next to the symlink for the app dll and find the symlinks
                    result
                        .Should().Pass()
                        .And.HaveStdOutContaining("Hello World");
                }
                else
                {
                    // On Unix, the apphost will not find the app files next to the symlink
                    result
                        .Should().Fail()
                        .And.HaveStdErrContaining("The application to execute does not exist");
                }
            }
            finally
            {
                foreach (var symlink in symlinks)
                {
                    symlink.Dispose();
                }
            }
        }

        [Theory]
        [InlineData("a/b/SymlinkToFrameworkDependentApp")]
        [InlineData("a/SymlinkToFrameworkDependentApp")]
        public void Symlink_all_files_self_contained(string symlinkRelativePath)
        {
            using var testDir = TestArtifact.Create("symlink");
            Directory.CreateDirectory(Path.Combine(testDir.Location, Path.GetDirectoryName(symlinkRelativePath)));

            // Symlink every file in the app directory
            var symlinks = new List<SymLink>();
            try
            {
                foreach (var file in Directory.EnumerateFiles(sharedTestState.SelfContainedApp.Location))
                {
                    var fileName = Path.GetFileName(file);
                    var symlinkPath = Path.Combine(testDir.Location, symlinkRelativePath, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(symlinkPath));
                    symlinks.Add(new SymLink(symlinkPath, file));
                }

                var result = Command.Create(Path.Combine(testDir.Location, symlinkRelativePath, Path.GetFileName(sharedTestState.FrameworkDependentApp.AppExe)))
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                    .Execute();

                // This should succeed on all platforms, but for different reasons:
                // * Windows: The apphost will look next to the symlink for the files and find the symlinks
                // * Unix: The apphost will look next to the resolved apphost for the files and find the real thing
                result
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
            finally
            {
                foreach (var symlink in symlinks)
                {
                    symlink.Dispose();
                }
            }
        }

        [Theory]
        [InlineData ("a/b/SymlinkToApphost")]
        [InlineData ("a/SymlinkToApphost")]
        public void Run_apphost_behind_symlink(string symlinkRelativePath)
        {
            symlinkRelativePath = Binaries.GetExeName(symlinkRelativePath);
            using (var testDir = TestArtifact.Create("symlink"))
            {
                Directory.CreateDirectory(Path.Combine(testDir.Location, Path.GetDirectoryName(symlinkRelativePath)));
                var symlinkFullPath = Path.Combine(testDir.Location, symlinkRelativePath);

                using var symlink = new SymLink(symlinkFullPath, sharedTestState.SelfContainedApp.AppExe);
                var result = Command.Create(symlinkFullPath)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    result
                        .Should().Fail()
                        .And.HaveStdErrContaining("The application to execute does not exist");
                }
                else
                {
                    result
                        .Should().Pass()
                        .And.HaveStdOutContaining("Hello World");
                }
            }
        }

        [Theory]
        [InlineData ("a/b/FirstSymlink", "c/d/SecondSymlink")]
        [InlineData ("a/b/FirstSymlink", "c/SecondSymlink")]
        [InlineData ("a/FirstSymlink", "c/d/SecondSymlink")]
        [InlineData ("a/FirstSymlink", "c/SecondSymlink")]
        public void Run_apphost_behind_transitive_symlinks(string firstSymlinkRelativePath, string secondSymlinkRelativePath)
        {
            firstSymlinkRelativePath = Binaries.GetExeName(firstSymlinkRelativePath);
            secondSymlinkRelativePath = Binaries.GetExeName(secondSymlinkRelativePath);
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

                var result = Command.Create(symlink1.SrcPath)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    result
                        .Should().Fail()
                        .And.HaveStdErrContaining("The application to execute does not exist");
                }
                else
                {
                    result
                        .Should().Pass()
                        .And.HaveStdOutContaining("Hello World");
                }
            }
        }

        [Theory]
        [InlineData("a/b/SymlinkToFrameworkDependentApp")]
        [InlineData("a/SymlinkToFrameworkDependentApp")]
        [SkipOnPlatform(TestPlatforms.OSX, "Currently failing in OSX with \"No such file or directory\" when running Command.Create. " +
            "CI failing to use stat on symbolic links on Linux (permission denied).")]
        public void Run_framework_dependent_app_behind_symlink(string symlinkRelativePath)
        {
            symlinkRelativePath = Binaries.GetExeName(symlinkRelativePath);

            using (var testDir = TestArtifact.Create("symlink"))
            {
                Directory.CreateDirectory(Path.Combine(testDir.Location, Path.GetDirectoryName(symlinkRelativePath)));

                using var symlink = new SymLink(Path.Combine(testDir.Location, symlinkRelativePath), sharedTestState.FrameworkDependentApp.AppExe);
                var result = Command.Create(symlink.SrcPath)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                    .Execute();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    result
                        .Should().Fail()
                        .And.HaveStdErrContaining("The application to execute does not exist");
                }
                else
                {
                    result
                        .Should().Pass()
                        .And.HaveStdOutContaining("Hello World");
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX, "Currently failing in OSX with \"No such file or directory\" when running Command.Create. " +
            "CI failing to use stat on symbolic links on Linux (permission denied).")]
        public void Run_framework_dependent_app_with_runtime_behind_symlink()
        {
            using (var testDir = TestArtifact.Create("symlink"))
            {
                var dotnetSymlink = Path.Combine(testDir.Location, Binaries.GetExeName("dotnet"));

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
        public void Put_dotnet_behind_symlink()
        {
            using (var testDir = TestArtifact.Create("symlink"))
            {
                var dotnetSymlink = Path.Combine(testDir.Location, Binaries.DotNet.FileName);

                using var symlink = new SymLink(dotnetSymlink, TestContext.BuiltDotNet.DotnetExecutablePath);
                var result = Command.Create(symlink.SrcPath, sharedTestState.SelfContainedApp.AppDll)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    result
                        .Should().Fail()
                        .And.HaveStdErrContaining($"[{Path.Combine(testDir.Location, "host", "fxr")}] does not exist");
                }
                else
                {
                    result
                        .Should().Pass()
                        .And.HaveStdOutContaining("Hello World");
                }
            }
        }

        [Fact]
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
