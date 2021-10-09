// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class WindowsSpecificBehavior : IClassFixture<WindowsSpecificBehavior.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public WindowsSpecificBehavior(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Manifests are only supported on Windows OSes.
        public void MuxerRunsPortableAppWithoutWindowsOsShims()
        {
            TestProjectFixture portableAppFixture = sharedTestState.TestWindowsOsShimsAppFixture.Copy();

            portableAppFixture.BuiltDotnet.Exec(portableAppFixture.TestProject.AppDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Reported OS version is newer or equal to the true OS version - no shims.");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FrameworkDependent_DLL_LongPath_Succeeds()
        {
            // Long paths must also be enabled via a machine-wide setting. Only run the test if it is enabled.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem"))
            {
                if (key == null)
                {
                    return;
                }

                object longPathsSetting = key.GetValue("LongPathsEnabled", null);
                if (longPathsSetting == null || !(longPathsSetting is int) || (int)longPathsSetting == 0)
                {
                    return;
                }
            }

            var fixture = sharedTestState.PortableAppWithLongPathFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll, fixture.TestProject.Location)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining("CreateDirectoryW with long path succeeded");
        }

        // Testing the standalone version (apphost) would require to make a copy of the entire SDK
        // and overwrite the apphost.exe in it. Currently this is just too expensive for one test (160MB of data).

        public class SharedTestState : IDisposable
        {
            private static RepoDirectoriesProvider RepoDirectories { get; set; }

            public TestProjectFixture PortableAppWithLongPathFixture { get; }
            public TestProjectFixture TestWindowsOsShimsAppFixture { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PortableAppWithLongPathFixture = new TestProjectFixture("PortableAppWithLongPath", RepoDirectories)
                    .EnsureRestored()
                    .BuildProject();

                TestWindowsOsShimsAppFixture = new TestProjectFixture("TestWindowsOsShimsApp", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
            }

            public void Dispose()
            {
                PortableAppWithLongPathFixture.Dispose();
                TestWindowsOsShimsAppFixture.Dispose();
            }
        }
    }
}
