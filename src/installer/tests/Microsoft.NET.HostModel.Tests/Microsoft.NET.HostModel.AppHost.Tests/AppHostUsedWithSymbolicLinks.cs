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

        [Fact]
        public void Run_apphost_behind_symbolic_link()
        {
            while (!System.Diagnostics.Debugger.IsAttached)
                System.Threading.Thread.Sleep(5000);

            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();
            
            var appExe = fixture.TestProject.AppExe;

            Command.Create(appExe)
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

                var buildFixture = new TestProjectFixture("StandaloneApp6x", RepoDirectories);
                buildFixture
                    .EnsureRestoredForRid(buildFixture.CurrentRid)
                    .BuildProject(runtime: buildFixture.CurrentRid);

                var publishFixture = new TestProjectFixture("StandaloneApp6x", RepoDirectories);
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