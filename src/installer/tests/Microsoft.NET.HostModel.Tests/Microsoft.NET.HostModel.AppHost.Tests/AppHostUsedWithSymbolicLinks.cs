// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using System;
using System.IO;
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
        [InlineData ("../../SymlinkToApphost")]
        [InlineData ("../SymlinkToApphost")]
        public void Run_apphost_behind_symlink(string symlinkRelativePath)
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();
            
            var appExe = fixture.TestProject.AppExe;
            string symbolicLink = Path.GetFullPath(Path.Combine(appExe, symlinkRelativePath));
            string targetFileName = appExe;
            if (!SymbolicLinking.MakeSymbolicLink (symbolicLink, targetFileName, out var errorString))
                throw new Exception($"Failed to create symbolic link '{symbolicLink}' targeting '{targetFileName}': {errorString}");

            Command.Create(symbolicLink)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Theory]
        [InlineData ("../../FirstSymlink", "../../SecondSymlink")]
        [InlineData ("../../FirstSymlink", "../SecondSymlink")]
        [InlineData ("../FirstSymlink", "../../SecondSymlink")]
        [InlineData ("../FirstSymlink", "../SecondSymlink")]
        public void Run_apphost_behind_transitive_symlinks(string firstSymlinkRelativePath, string secondSymlinkRelativePath)
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();
            
            var appExe = fixture.TestProject.AppExe;
            // second symlink -> apphost
            string secondSymbolicLink = Path.GetFullPath(Path.Combine(appExe, secondSymlinkRelativePath));
            string targetFileName = appExe;
            if (!SymbolicLinking.MakeSymbolicLink (secondSymbolicLink, targetFileName, out var errorString))
                throw new Exception($"Failed to create symbolic link '{secondSymbolicLink}' targeting '{targetFileName}': {errorString}");

            // first symlink -> second symlink
            string firstSymbolicLink = Path.GetFullPath(Path.Combine(appExe, firstSymlinkRelativePath));
            if (!SymbolicLinking.MakeSymbolicLink (firstSymbolicLink, secondSymbolicLink, out errorString))
                throw new Exception($"Failed to create symbolic link '{firstSymbolicLink}' targeting '{secondSymbolicLink}': {errorString}");

            Command.Create(firstSymbolicLink)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture StandaloneAppFixture_Built { get; }
            public TestProjectFixture StandaloneAppFixture_Published { get; }
            public RepoDirectoriesProvider RepoDirectories { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                var buildFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
                buildFixture
                    .EnsureRestoredForRid(buildFixture.CurrentRid)
                    .BuildProject(runtime: buildFixture.CurrentRid);

                var publishFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
                publishFixture
                    .EnsureRestoredForRid(publishFixture.CurrentRid)
                    .PublishProject(runtime: publishFixture.CurrentRid);

                StandaloneAppFixture_Built = buildFixture;
                StandaloneAppFixture_Published = publishFixture;
            }

            public void Dispose()
            {
                StandaloneAppFixture_Built.Dispose();
                StandaloneAppFixture_Published.Dispose();
            }
        }
    }
}