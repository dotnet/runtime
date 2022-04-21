// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundledAppWithSubDirs : BundleTestBase, IClassFixture<BundledAppWithSubDirs.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundledAppWithSubDirs(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private void RunTheApp(string path, TestProjectFixture fixture)
        {
            RunTheApp(path, fixture.BuiltDotnet)
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Wow! We now say hello to the big world and you.");
        }

        private CommandResult RunTheApp(string path, DotNetCli dotnet)
        {
            return Command.Create(path)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(dotnet.BinPath)
                .MultilevelLookup(false)
                .Execute();
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Framework_dependent_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestFrameworkDependentFixture.Copy();
            UseFrameworkDependentHost(fixture);
            var singleFile = BundleHelper.BundleApp(fixture, options);

            // Run the bundled app (extract files)
            RunTheApp(singleFile, fixture);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, fixture);
        }

        [Fact]
        public void Bundle_Framework_dependent_NoBundleEntryPoint()
        {
            var fixture = sharedTestState.TestFrameworkDependentFixture.Copy();
            UseFrameworkDependentHost(fixture);
            var singleFile = BundleHelper.BundleApp(fixture, BundleOptions.None);

            string dotnetWithMockHostFxr = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "guiErrors"));
            using (new TestArtifact(dotnetWithMockHostFxr))
            {
                Directory.CreateDirectory(dotnetWithMockHostFxr);
                var dotnetBuilder = new DotNetBuilder(dotnetWithMockHostFxr, sharedTestState.RepoDirectories.BuiltDotnet, "mockhostfxrFrameworkMissingFailure")
                    .RemoveHostFxr()
                    .AddMockHostFxr(new Version(2, 2, 0));
                var dotnet = dotnetBuilder.Build();

                // Run the bundled app (extract files)
                RunTheApp(singleFile, dotnet)
                    .Should()
                    .Fail()
                    .And.HaveStdErrContaining("You must install or update .NET to run this application.")
                    .And.HaveStdErrContaining("App host version:")
                    .And.HaveStdErrContaining("apphost_version=");
            }
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void Bundled_Framework_dependent_App_GUI_DownlevelHostFxr_ErrorDialog(BundleOptions options)
        {
            var fixture = sharedTestState.TestFrameworkDependentFixture.Copy();
            UseFrameworkDependentHost(fixture);
            var singleFile = BundleHelper.BundleApp(fixture, options);
            AppHostExtensions.SetWindowsGraphicalUserInterfaceBit(singleFile);

            string dotnetWithMockHostFxr = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "bundleErrors"));
            using (new TestArtifact(dotnetWithMockHostFxr))
            {
                Directory.CreateDirectory(dotnetWithMockHostFxr);
                string expectedErrorCode = Constants.ErrorCode.BundleExtractionFailure.ToString("x");

                var dotnetBuilder = new DotNetBuilder(dotnetWithMockHostFxr, sharedTestState.RepoDirectories.BuiltDotnet, "mockhostfxrBundleVersionFailure")
                    .RemoveHostFxr()
                    .AddMockHostFxr(new Version(5, 0, 0));
                var dotnet = dotnetBuilder.Build();

                Command command = Command.Create(singleFile)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(dotnet.BinPath, sharedTestState.RepoDirectories.BuildArchitecture)
                    .MultilevelLookup(false)
                    .Start();

                WindowsUtils.WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                command
                    .WaitForExit(true)
                    .Should().Fail()
                    .And.HaveStdErrContaining("Bundle header version compatibility check failed.")
                    .And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(singleFile)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining("apphost_version=");
            }
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Self_Contained_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture, options);

            // Run the bundled app (extract files)
            RunTheApp(singleFile, fixture);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, fixture);
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Self_Contained_NoCompression_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture, options, disableCompression: true);

            // Run the bundled app (extract files)
            RunTheApp(singleFile, fixture);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, fixture);
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Self_Contained_Targeting50_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture, options, new Version(5, 0));

            // Run the bundled app (extract files)
            RunTheApp(singleFile, fixture);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, fixture);
        }

        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Framework_dependent_Targeting50_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            UseFrameworkDependentHost(fixture);
            var singleFile = BundleHelper.BundleApp(fixture, options, new Version(5, 0));

            // Run the bundled app (extract files)
            RunTheApp(singleFile, fixture);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, fixture);
        }

        [Fact]
        public void Bundled_Self_Contained_Targeting50_WithCompression_Throws()
        {
            var fixture = sharedTestState.TestSelfContainedFixture.Copy();
            UseSingleFileSelfContainedHost(fixture);
            // compression must be off when targeting 5.0
            var options = BundleOptions.EnableCompression;

            Assert.Throws<ArgumentException>(()=>BundleHelper.BundleApp(fixture, options, new Version(5, 0)));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54234")]
        // NOTE: when enabling this test take a look at commented code maked by "ACTIVE ISSUE:" in SharedTestState
        public void Bundled_Self_Contained_Composite_App_Run_Succeeds()
        {
            var fixture = sharedTestState.TestSelfContainedFixtureComposite.Copy();
            var singleFile = BundleSelfContainedApp(fixture, BundleOptions.None, disableCompression: true);

            // Run the app
            RunTheApp(singleFile, fixture);
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_With_Empty_File_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestAppWithEmptyFileFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture, options);

            // Run the app
            RunTheApp(singleFile, fixture);
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFrameworkDependentFixture { get; set; }
            public TestProjectFixture TestSelfContainedFixture { get; set; }
            public TestProjectFixture TestAppWithEmptyFileFixture { get; set; }
            public TestProjectFixture TestSelfContainedFixtureComposite { get; set; }

            public SharedTestState()
            {
                TestFrameworkDependentFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestFrameworkDependentFixture);
                TestFrameworkDependentFixture
                    .EnsureRestoredForRid(TestFrameworkDependentFixture.CurrentRid)
                    .PublishProject(runtime: TestFrameworkDependentFixture.CurrentRid,
                                    selfContained: false,
                                    outputDirectory: BundleHelper.GetPublishPath(TestFrameworkDependentFixture));

                TestSelfContainedFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestSelfContainedFixture);
                TestSelfContainedFixture
                    .EnsureRestoredForRid(TestSelfContainedFixture.CurrentRid)
                    .PublishProject(runtime: TestSelfContainedFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestSelfContainedFixture));

                TestAppWithEmptyFileFixture = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestAppWithEmptyFileFixture);
                BundleHelper.AddEmptyContentToApp(TestAppWithEmptyFileFixture);
                TestAppWithEmptyFileFixture
                    .EnsureRestoredForRid(TestAppWithEmptyFileFixture.CurrentRid)
                    .PublishProject(runtime: TestAppWithEmptyFileFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestAppWithEmptyFileFixture));

                TestSelfContainedFixtureComposite = new TestProjectFixture("AppWithSubDirs", RepoDirectories);
                BundleHelper.AddLongNameContentToAppWithSubDirs(TestSelfContainedFixtureComposite);
                TestSelfContainedFixtureComposite
                    .EnsureRestoredForRid(TestSelfContainedFixtureComposite.CurrentRid)
                    .PublishProject(runtime: TestSelfContainedFixtureComposite.CurrentRid,
                                    // ACTIVE ISSUE: https://github.com/dotnet/runtime/issues/54234
                                    //               uncomment extraArgs when fixed.
                                    outputDirectory: BundleHelper.GetPublishPath(TestSelfContainedFixtureComposite) /*,
                                    extraArgs: new string[] {
                                       "/p:PublishReadyToRun=true",
                                       "/p:PublishReadyToRunComposite=true" } */);
            }

            public void Dispose()
            {
                TestFrameworkDependentFixture.Dispose();
                TestSelfContainedFixture.Dispose();
                TestAppWithEmptyFileFixture.Dispose();
                TestSelfContainedFixtureComposite.Dispose();
            }
        }
    }
}
