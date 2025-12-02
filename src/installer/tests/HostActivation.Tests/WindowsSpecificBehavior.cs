// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class WindowsSpecificBehavior : IClassFixture<WindowsSpecificBehavior.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public WindowsSpecificBehavior(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void DotNet_NoCompatShims()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll, "compat_shims")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Reported OS version is newer or equal to the true OS version - no shims.");
        }

        [Fact]
        public void AppHost_NoManifest_HasCompatShims()
        {
            Command.Create(sharedTestState.App.AppExe, "compat_shims")
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Reported OS version is lower than the true OS version - shims in use.");
        }

        // Long paths must also be enabled via a machine-wide setting. Only run the test if it is enabled.
        private static bool LongPathsEnabled()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem"))
            {
                if (key == null)
                    return false;

                object longPathsSetting = key.GetValue("LongPathsEnabled", null);
                return longPathsSetting != null && longPathsSetting is int && (int)longPathsSetting != 0;
            }
        }

        [ConditionalFact(nameof(LongPathsEnabled))]
        public void DotNet_LongPath_Succeeds()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll, "long_path", sharedTestState.App.Location)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("CreateDirectoryW with long path succeeded");
        }

        public class SharedTestState : IDisposable
        {
            public TestApp App { get; }

            public SharedTestState()
            {
                App = TestApp.CreateFromBuiltAssets("WindowsSpecific");
                App.CreateAppHost();
            }

            public void Dispose()
            {
                App?.Dispose();
            }
        }
    }
}
