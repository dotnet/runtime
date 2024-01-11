// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundledAppWithSubDirs : IClassFixture<BundledAppWithSubDirs.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundledAppWithSubDirs(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private void RunTheApp(string path, bool selfContained)
        {
            RunTheApp(path, selfContained ? null : TestContext.BuiltDotNet.BinPath)
                .Should().Pass()
                .And.HaveStdOutContaining("Wow! We now say hello to the big world and you.");
        }

        private CommandResult RunTheApp(string path, string dotnetRoot)
        {
            return Command.Create(path)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(dotnetRoot)
                .MultilevelLookup(false)
                .Execute();
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void FrameworkDependent(BundleOptions options)
        {
            var singleFile = sharedTestState.FrameworkDependentApp.Bundle(options);

            // Run the bundled app
            RunTheApp(singleFile, selfContained: false);

            if (options.HasFlag(BundleOptions.BundleAllContent))
            {
                // Run the bundled app again (reuse extracted files)
                RunTheApp(singleFile, selfContained: false);
            }
        }

        [Fact]
        public void FrameworkDependent_NoBundleEntryPoint()
        {
            var singleFile = sharedTestState.FrameworkDependentApp.Bundle(BundleOptions.None);

            string dotnetWithMockHostFxr = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "guiErrors"));
            using (new TestArtifact(dotnetWithMockHostFxr))
            {
                Directory.CreateDirectory(dotnetWithMockHostFxr);
                var dotnetBuilder = new DotNetBuilder(dotnetWithMockHostFxr, TestContext.BuiltDotNet.BinPath, "mockhostfxrFrameworkMissingFailure")
                    .RemoveHostFxr()
                    .AddMockHostFxr(new Version(2, 2, 0));
                var dotnet = dotnetBuilder.Build();

                // Run the bundled app (extract files)
                RunTheApp(singleFile, dotnet.BinPath)
                    .Should()
                    .Fail()
                    .And.HaveStdErrContaining("You must install or update .NET to run this application.")
                    .And.HaveStdErrContaining("App host version:")
                    .And.HaveStdErrContaining("apphost_version=");
            }
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void FrameworkDependent_GUI_DownlevelHostFxr_ErrorDialog(BundleOptions options)
        {
            var singleFile = sharedTestState.FrameworkDependentApp.Bundle(options);
            PEUtils.SetWindowsGraphicalUserInterfaceBit(singleFile);

            string dotnetWithMockHostFxr = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "bundleErrors"));
            using (new TestArtifact(dotnetWithMockHostFxr))
            {
                Directory.CreateDirectory(dotnetWithMockHostFxr);
                string expectedErrorCode = Constants.ErrorCode.BundleExtractionFailure.ToString("x");

                var dotnetBuilder = new DotNetBuilder(dotnetWithMockHostFxr, TestContext.BuiltDotNet.BinPath, "mockhostfxrBundleVersionFailure")
                    .RemoveHostFxr()
                    .AddMockHostFxr(new Version(5, 0, 0));
                var dotnet = dotnetBuilder.Build();

                Command command = Command.Create(singleFile)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(dotnet.BinPath, TestContext.BuildArchitecture)
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
        [InlineData(BundleOptions.BundleAllContent)]
        [InlineData(BundleOptions.EnableCompression)]
        [InlineData(BundleOptions.BundleAllContent | BundleOptions.EnableCompression)]
        [Theory]
        public void SelfContained(BundleOptions options)
        {
            var singleFile = sharedTestState.SelfContainedApp.Bundle(options);

            // Run the bundled app
            RunTheApp(singleFile, selfContained: true);

            if (options.HasFlag(BundleOptions.BundleAllContent))
            {
                // Run the bundled app again (reuse extracted files)
                RunTheApp(singleFile, selfContained: true);
            }
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void SelfContained_Targeting50(BundleOptions options)
        {
            var singleFile = sharedTestState.SelfContainedApp.Bundle(options, new Version(5, 0));

            // Run the bundled app
            RunTheApp(singleFile, selfContained: true);

            if (options.HasFlag(BundleOptions.BundleAllContent))
            {
                // Run the bundled app again (reuse extracted files)
                RunTheApp(singleFile, selfContained: true);
            }
        }

        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void FrameworkDependent_Targeting50(BundleOptions options)
        {
            var singleFile = sharedTestState.FrameworkDependentApp.Bundle(options, new Version(5, 0));

            // Run the bundled app
            RunTheApp(singleFile, selfContained: false);

            if (options.HasFlag(BundleOptions.BundleAllContent))
            {
                // Run the bundled app again (reuse extracted files)
                RunTheApp(singleFile, selfContained: false);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54234")]
        // NOTE: when enabling this test take a look at commented code marked by "ACTIVE ISSUE:" in SharedTestState
        public void SelfContained_R2R_Composite()
        {
            var singleFile = sharedTestState.SelfContainedCompositeApp.Bundle(BundleOptions.None);

            // Run the app
            RunTheApp(singleFile, selfContained: true);
        }

        public class SharedTestState : IDisposable
        {
            public SingleFileTestApp FrameworkDependentApp { get; }
            public SingleFileTestApp SelfContainedApp { get; }
            public SingleFileTestApp SelfContainedCompositeApp { get; }

            public SharedTestState()
            {
                FrameworkDependentApp = SingleFileTestApp.CreateFrameworkDependent("AppWithSubDirs");
                AddLongNameContent(FrameworkDependentApp.NonBundledLocation);

                SelfContainedApp = SingleFileTestApp.CreateSelfContained("AppWithSubDirs");
                AddLongNameContent(SelfContainedApp.NonBundledLocation);

                // ACTIVE ISSUE: https://github.com/dotnet/runtime/issues/54234
                //               This should be an app built with the equivalent of PublishReadyToRun=true and PublishReadyToRunComposite=true
                SelfContainedCompositeApp = SingleFileTestApp.CreateSelfContained("AppWithSubDirs");
                AddLongNameContent(SelfContainedCompositeApp.NonBundledLocation);
            }

            public void Dispose()
            {
                FrameworkDependentApp.Dispose();
                SelfContainedApp.Dispose();
                SelfContainedCompositeApp.Dispose();
            }

            public static void AddLongNameContent(string directory)
            {
                // For tests using the AppWithSubDirs, One of the sub-directories with a really long name
                // is generated during test-runs rather than being checked in as a test asset.
                // This prevents git-clone of the repo from failing if long-file-name support is not enabled on windows.
                var longDirName = "This is a really, really, really, really, really, really, really, really, really, really, really, really, really, really long file name for punctuation";
                var longDirPath = Path.Combine(directory, "Sentence", longDirName);
                Directory.CreateDirectory(longDirPath);
                using (var writer = File.CreateText(Path.Combine(longDirPath, "word")))
                {
                    writer.Write(".");
                }
            }
        }
    }
}
