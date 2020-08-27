// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            var fixture = sharedTestState.TestFixtureBundlerAPI.Copy();
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

        [Fact]
        public void Bundled_Localized_App_Using_SDK_Run_Succeeds()
        {
            var fixture = sharedTestState.TestFixtureSDK.Copy();
            var appExe = fixture.TestProject.AppExe;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Set code page to output unicode characters.
                Command.Create("chcp 65001").Execute();
            }

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And.HaveStdOutContaining("ನಮಸ್ಕಾರ! வணக்கம்! Hello!");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixtureBundlerAPI { get; set; }
            public TestProjectFixture TestFixtureSDK { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixtureBundlerAPI = new TestProjectFixture("LocalizedApp", RepoDirectories);
                TestFixtureBundlerAPI
                    .EnsureRestoredForRid(TestFixtureBundlerAPI.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixtureBundlerAPI.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestFixtureBundlerAPI));

                TestFixtureSDK = new TestProjectFixture("LocalizedApp", RepoDirectories);
                TestFixtureSDK
                    .EnsureRestoredForRid(TestFixtureSDK.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixtureSDK.CurrentRid,
                                    singleFile: true);
            }

            public void Dispose()
            {
                TestFixtureBundlerAPI.Dispose();
                TestFixtureSDK.Dispose();
            }
        }
    }
}
