// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.Bundle;
using Microsoft.DotNet.CoreSetup.Test;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundleLocalizedApp : IClassFixture<BundleLocalizedApp.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleLocalizedApp(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Bundled_Localized_App_Run_Succeeds()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Set code page to output unicode characters.
                Command.Create("chcp 65001").Execute();
            }

            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("ನಮಸ್ಕಾರ! வணக்கம்! Hello!");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("LocalizedApp", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestFixture));
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
