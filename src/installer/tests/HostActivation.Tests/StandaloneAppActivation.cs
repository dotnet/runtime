// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.AppHost;
using Xunit;

namespace HostActivation.Tests
{
    public class StandaloneAppActivation : IClassFixture<StandaloneAppActivation.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public StandaloneAppActivation(StandaloneAppActivation.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Default()
        {
            string appExe = sharedTestState.App.AppExe;
            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(TestContext.MicrosoftNETCoreAppVersion);

            if (OperatingSystem.IsWindows())
            {
                // App sets FileVersion to NETCoreApp version. On Windows, this should be copied to app resources.
                string expectedVersion = TestContext.MicrosoftNETCoreAppVersion.Contains('-')
                    ? TestContext.MicrosoftNETCoreAppVersion[..TestContext.MicrosoftNETCoreAppVersion.IndexOf('-')]
                    : TestContext.MicrosoftNETCoreAppVersion;
                Assert.Equal(expectedVersion, System.Diagnostics.FileVersionInfo.GetVersionInfo(appExe).FileVersion);
            }
        }

        [Fact]
        public void NoDepsJson_NoRuntimeConfig()
        {
            var app = sharedTestState.App.Copy();

            File.Delete(app.RuntimeConfigJson);
            File.Delete(app.DepsJson);

            // Make sure normal run succeeds and doesn't print any errors
            Command.Create(app.AppExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                // Note that this is an exact match - we don't expect any output from the host itself
                .And.HaveStdOut($"Hello World!{Environment.NewLine}{Environment.NewLine}.NET {TestContext.MicrosoftNETCoreAppVersion}{Environment.NewLine}")
                .And.NotHaveStdErr();

            // Make sure tracing indicates there is no runtime config and no deps json
            Command.Create(app.AppExe)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOut($"Hello World!{Environment.NewLine}{Environment.NewLine}.NET {TestContext.MicrosoftNETCoreAppVersion}{Environment.NewLine}")
                .And.HaveStdErrContaining($"Runtime config does not exist at [{app.RuntimeConfigJson}]")
                .And.HaveStdErrContaining($"Dependencies manifest does not exist at [{app.DepsJson}]");
        }

        [Fact]
        public void RenameApphost()
        {
            var app = sharedTestState.App.Copy();

            var renamedAppExe = app.AppExe + Binaries.GetExeFileNameForCurrentPlatform("renamed");
            File.Move(app.AppExe, renamedAppExe, true);

            Command.Create(renamedAppExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(TestContext.MicrosoftNETCoreAppVersion);
        }

        [Fact]
        public void RelativeEmbeddedPath()
        {
            var app = sharedTestState.App.Copy();

            // Delete the apphost
            File.Delete(app.AppExe);

            // Create an apphost in a subdirectory pointing at the app using relative path
            string subDir = Path.Combine(app.Location, "sub");
            Directory.CreateDirectory(subDir);
            string appExe = Path.Combine(subDir, Path.GetFileName(app.AppExe));
            HostWriter.CreateAppHost(
                Binaries.AppHost.FilePath,
                appExe,
                Path.GetRelativePath(subDir, app.AppDll));

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(TestContext.MicrosoftNETCoreAppVersion);
        }

        [Fact]
        public void DotNetRoot_IncorrectLayout_Fails()
        {
            var app = sharedTestState.App.Copy();

            // Move the apphost exe and app dll to a subdirectory
            string subDir = Path.Combine(app.Location, "sub");
            Directory.CreateDirectory(subDir);
            string appExe = Path.Combine(subDir, Path.GetFileName(app.AppExe));
            File.Move(app.AppExe, appExe);
            File.Move(app.AppDll, Path.Combine(subDir, Path.GetFileName(app.AppDll)));

            // This verifies a self-contained apphost cannot use DOTNET_ROOT to reference a flat
            // self-contained layout since a flat layout of the shared framework is not supported.
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(app.Location)
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveUsedDotNetRootInstallLocation(Path.GetFullPath(app.Location), TestContext.TargetRID)
                .And.HaveStdErrContaining($"The required library {Binaries.HostFxr.FileName} could not be found.");
        }

        public class SharedTestState : IDisposable
        {
            public TestApp App { get; }

            public SharedTestState()
            {
                App = TestApp.CreateFromBuiltAssets("HelloWorld");
                App.PopulateSelfContained(TestApp.MockedComponent.None);
                App.CreateAppHost();
            }

            public void Dispose()
            {
                App.Dispose();
            }
        }
    }
}

