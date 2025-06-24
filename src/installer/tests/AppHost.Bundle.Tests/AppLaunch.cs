// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class AppLaunch : IClassFixture<AppLaunch.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public AppLaunch(AppLaunch.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private void RunTheApp(string path, bool selfContained)
        {
            Command.Create(path)
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(selfContained ? null : TestContext.BuiltDotNet.BinPath)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World!");
        }

        private string MakeUniversalBinary(string path, Architecture architecture)
        {
            string fatApp = path + ".fat";
            string arch = architecture == Architecture.Arm64 ? "arm64" : "x86_64";

            // We will create a universal binary with just one arch slice and run it.
            // It is enough for testing purposes. The code that finds the releavant slice
            // would work the same regardless if there is 1, 2, 3 or more slices.
            Command.Create("lipo", $"-create -arch {arch} {path} -output {fatApp}")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass();

            return fatApp;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        private void RunApp(bool selfContained)
        {
            // Bundle to a single-file
            string singleFile = selfContained
                ? sharedTestState.SelfContainedApp.Bundle()
                : sharedTestState.FrameworkDependentApp.Bundle();

            // Run the bundled app
            RunTheApp(singleFile, selfContained);

            if (OperatingSystem.IsMacOS())
            {
                string fatApp = MakeUniversalBinary(singleFile, RuntimeInformation.OSArchitecture);

                // Run the fat app
                RunTheApp(fatApp, selfContained);
            }

            if (OperatingSystem.IsWindows())
            {
                // StandaloneApp sets FileVersion to NETCoreApp version. On Windows, this should be copied to singlefilehost resources.
                string expectedVersion = TestContext.MicrosoftNETCoreAppVersion.Contains('-')
                    ? TestContext.MicrosoftNETCoreAppVersion[..TestContext.MicrosoftNETCoreAppVersion.IndexOf('-')]
                    : TestContext.MicrosoftNETCoreAppVersion;
                Assert.Equal(expectedVersion, System.Diagnostics.FileVersionInfo.GetVersionInfo(singleFile).FileVersion);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        private void OverwritingExistingBundleClearsMacOsSignatureCache()
        {
            // Bundle to a single-file and ensure it is signed
            string singleFile = sharedTestState.SelfContainedApp.Bundle();
            Assert.True(Codesign.Run("-v", singleFile).ExitCode == 0);
            var firstls = Command.Create("/bin/ls", "-li", singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute();
            firstls.Should().Pass();
            var firstInode = firstls.StdOut.Split(' ')[0];

            // Rebundle to the same location.
            // Bundler should create a new inode for the bundle which should clear the MacOS signature cache.
            string oldFile = singleFile;
            string dir = Path.GetDirectoryName(singleFile);
            singleFile = sharedTestState.SelfContainedApp.ReBundle(dir, BundleOptions.BundleAllContent, out var _, new Version(5, 0));
            Assert.True(singleFile == oldFile, "Rebundled app should have a different path than the original single-file app.");
            var secondls = Command.Create("/bin/ls", "-li", singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute();
            secondls.Should().Pass();
            var secondInode = secondls.StdOut.Split(' ')[0];
            Assert.False(firstInode == secondInode, "not a different inode after rebundle");
            // Ensure the MacOS signature cache is cleared
            Assert.True(Codesign.Run("-v", singleFile).ExitCode == 0);
        }

        [ConditionalTheory(typeof(Binaries.CetCompat), nameof(Binaries.CetCompat.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void DisableCetCompat(bool selfContained)
        {
            SingleFileTestApp app = selfContained
                ? sharedTestState.SelfContainedApp.Copy()
                : sharedTestState.FrameworkDependentApp.Copy();
            app.CreateAppHost(disableCetCompat: true);

            string singleFile = app.Bundle();
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(TestContext.BuiltDotNet.BinPath, TestContext.BuildArchitecture)
                .MultilevelLookup(false)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(TestContext.MicrosoftNETCoreAppVersion);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AdditionalContentAfterBundleMetadata(bool selfContained)
        {
            string singleFile = selfContained
                ? sharedTestState.SelfContainedApp.Bundle()
                : sharedTestState.FrameworkDependentApp.Bundle();

            using (var file = File.OpenWrite(singleFile))
            {
                file.Position = file.Length;
                var blob = Encoding.UTF8.GetBytes("Mock signature at the end of the bundle");
                file.Write(blob, 0, blob.Length);
            }

            RunTheApp(singleFile, selfContained);
        }

        [Fact]
        public void FrameworkDependent_NoBundleEntryPoint()
        {
            var singleFile = sharedTestState.FrameworkDependentApp.Bundle();

            using (var dotnetWithMockHostFxr = TestArtifact.Create("mockhostfxrFrameworkMissingFailure"))
            {
                var dotnet = new DotNetBuilder(dotnetWithMockHostFxr.Location, TestContext.BuiltDotNet.BinPath, null)
                    .RemoveHostFxr()
                    .AddMockHostFxr(new Version(2, 2, 0))
                    .Build();

                // Run the bundled app
                Command.Create(singleFile)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .DotNetRoot(dotnet.BinPath)
                    .Execute()
                    .Should().Fail()
                    .And.HaveStdErrContaining("You must install or update .NET to run this application.")
                    .And.HaveStdErrContaining("App host version:")
                    .And.HaveStdErrContaining("apphost_version=");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void FrameworkDependent_GUI_DownlevelHostFxr_ErrorDialog()
        {
            var singleFile = sharedTestState.FrameworkDependentApp.Bundle();
            Microsoft.NET.HostModel.AppHost.PEUtils.SetWindowsGraphicalUserInterfaceBit(singleFile);

            // The mockhostfxrBundleVersionFailure folder name is used by mock hostfxr to return the appropriate error code
            using (var dotnetWithMockHostFxr = TestArtifact.Create("mockhostfxrBundleVersionFailure"))
            {
                string expectedErrorCode = Constants.ErrorCode.BundleExtractionFailure.ToString("x");

                var dotnet = new DotNetBuilder(dotnetWithMockHostFxr.Location, TestContext.BuiltDotNet.BinPath, null)
                    .RemoveHostFxr()
                    .AddMockHostFxr(new Version(5, 0, 0))
                    .Build();

                Command command = Command.Create(singleFile)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(dotnet.BinPath, TestContext.BuildArchitecture)
                    .Start();

                WindowsUtils.WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                command
                    .WaitForExit()
                    .Should().Fail()
                    .And.HaveStdErrContaining("Bundle header version compatibility check failed.")
                    .And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(singleFile)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining("apphost_version=");
            }
        }

        public class SharedTestState : IDisposable
        {
            public SingleFileTestApp FrameworkDependentApp { get; }
            public SingleFileTestApp SelfContainedApp { get; }

            public SharedTestState()
            {
                FrameworkDependentApp = SingleFileTestApp.CreateFrameworkDependent("HelloWorld");
                SelfContainedApp = SingleFileTestApp.CreateSelfContained("HelloWorld");
            }

            public void Dispose()
            {
                FrameworkDependentApp.Dispose();
                SelfContainedApp.Dispose();
            }
        }
    }
}
