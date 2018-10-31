using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.WindowsOsShims
{
    public class GivenThatICareAboutWindowsOsShims : IClassFixture<GivenThatICareAboutWindowsOsShims.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public GivenThatICareAboutWindowsOsShims(SharedTestState fixture)
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

            TestProjectFixture portableAppFixture = sharedTestState.PortableTestWindowsOsShimsAppFixture.Copy();

            portableAppFixture.BuiltDotnet.Exec(portableAppFixture.TestProject.AppDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Reported OS version is newer or equal to the true OS version - no shims.");
        }

        // Testing the standalone version (apphost) would require to make a copy of the entire SDK
        // and overwrite the apphost.exe in it. Currently this is just too expensive for one test (160MB of data).

        public class SharedTestState : IDisposable
        {
            private static RepoDirectoriesProvider RepoDirectories { get; set; }

            public TestProjectFixture PortableTestWindowsOsShimsAppFixture { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PortableTestWindowsOsShimsAppFixture = new TestProjectFixture("TestWindowsOsShimsApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
            }

            public void Dispose()
            {
                PortableTestWindowsOsShimsAppFixture.Dispose();
            }
        }
    }
}
