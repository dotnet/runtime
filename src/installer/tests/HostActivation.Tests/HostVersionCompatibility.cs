// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class HostVersionCompatibility : IClassFixture<HostVersionCompatibility.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public HostVersionCompatibility(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void LatestHost_OldRuntime_BackwardsCompatible_60()
        {
            LatestHost_OldRuntime_BackwardsCompatible(sharedTestState.App60);
        }

        private void LatestHost_OldRuntime_BackwardsCompatible(TestApp previousVersionApp)
        {
            TestApp app = previousVersionApp.Copy();
            string appExe = app.AppExe;

            RuntimeConfig appConfig = RuntimeConfig.FromFile(app.RuntimeConfigJson);
            Assert.NotEqual(appConfig.Tfm, TestContext.Tfm);
            Assert.NotEqual(appConfig.GetIncludedFramework(Constants.MicrosoftNETCoreApp).Version, TestContext.MicrosoftNETCoreAppVersion);

            // Use the newer apphost
            // This emulates the case when:
            //  1) Newer runtime installed
            //  2) Newer runtime uninstalled (installer preserves newer apphost)
            app.CreateAppHost();
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {TestContext.MicrosoftNETCoreAppVersion}");

            // Use the newer apphost and hostFxr
            // This emulates the case when:
            //  1) Newer runtime installed
            //  2) A roll-forward to the newer runtime did not occur
            File.Copy(Binaries.HostFxr.FilePath, app.HostFxrDll, true);
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {TestContext.MicrosoftNETCoreAppVersion}");
        }

        [Fact]
        public void OldHost_LatestRuntime_ForwardCompatible_60()
        {
            OldHost_LatestRuntime_ForwardCompatible(sharedTestState.App60);
        }

        private void OldHost_LatestRuntime_ForwardCompatible(TestApp previousVersionApp)
        {
            TestApp app = sharedTestState.AppLatest.Copy();
            string appExe = app.AppExe;

            RuntimeConfig previousAppConfig = RuntimeConfig.FromFile(previousVersionApp.RuntimeConfigJson);
            string previousVersion = previousAppConfig.GetIncludedFramework(Constants.MicrosoftNETCoreApp).Version;
            Assert.NotEqual(TestContext.Tfm, previousAppConfig.Tfm);
            Assert.NotEqual(TestContext.MicrosoftNETCoreAppVersion, previousVersion);

            // Use the older apphost
            // This emulates the case when:
            //  1) Newer runtime installed
            //  2) App rolls forward to newer runtime
            File.Copy(previousVersionApp.AppExe, appExe, true);
            Command.Create(appExe)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"--- Invoked apphost [version: {previousVersion}");

            // Use the older apphost and hostfxr
            // This emulates the case when:
            //  1) One-off deployment of older runtime (not in global location)
            //  2) Older apphost executed, but found newer runtime because of multi-level lookup on Windows
            //     Note that we don't have multi-level on hostfxr so we will always find the older\one-off hostfxr
            if (OperatingSystem.IsWindows())
            {
                File.Copy(previousVersionApp.HostFxrDll, app.HostFxrDll, true);
                Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World")
                    .And.HaveStdErrContaining($"--- Invoked apphost [version: {previousVersion}");
            }
        }

        public class SharedTestState : IDisposable
        {
            public TestApp App60 { get; }
            public TestApp AppLatest { get; }

            private const string AppName = "HelloWorld";

            public SharedTestState()
            {
                App60 = TestApp.CreateFromBuiltAssets(AppName, Path.Combine("SelfContained", "net6.0"));

                AppLatest = TestApp.CreateFromBuiltAssets(AppName);
                AppLatest.PopulateSelfContained(TestApp.MockedComponent.None);
            }

            public void Dispose()
            {
                App60?.Dispose();
                AppLatest?.Dispose();
            }
        }
    }
}
