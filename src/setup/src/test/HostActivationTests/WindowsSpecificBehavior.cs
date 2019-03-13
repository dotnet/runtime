// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public void MuxerRunsPortableAppWithoutWindowsOsShims()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Manifests are only supported on Windows OSes.
                return;
            }

            TestProjectFixture portableAppFixture = sharedTestState.TestWindowsOsShimsAppFixture.Copy();

            portableAppFixture.BuiltDotnet.Exec(portableAppFixture.TestProject.AppDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Reported OS version is newer or equal to the true OS version - no shims.");
        }

        [Fact]
        public void FrameworkDependent_DLL_LongPath_Succeeds()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var fixture = sharedTestState.PortableAppWithLongPathFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
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
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                TestWindowsOsShimsAppFixture = new TestProjectFixture("TestWindowsOsShimsApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
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
