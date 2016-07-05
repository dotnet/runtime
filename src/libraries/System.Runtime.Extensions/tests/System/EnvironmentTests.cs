// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public class EnvironmentTests : RemoteExecutorTestBase
    {
        [Fact]
        public void CurrentDirectory_Null_Path_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("value", () => Environment.CurrentDirectory = null);
        }

        [Fact]
        public void CurrentDirectory_Empty_Path_Throws_ArgumentException()
        {
            Assert.Throws<ArgumentException>("value", () => Environment.CurrentDirectory = string.Empty);
        }

        [Fact]
        public void CurrentDirectory_SetToNonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(() => Environment.CurrentDirectory = GetTestFilePath());
        }

        [Fact]
        public void CurrentDirectory_SetToValidOtherDirectory()
        {
            RemoteInvoke(() =>
            {
                Environment.CurrentDirectory = TestDirectory;
                Assert.Equal(Directory.GetCurrentDirectory(), Environment.CurrentDirectory);

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // On OSX, the temp directory /tmp/ is a symlink to /private/tmp, so setting the current
                    // directory to a symlinked path will result in GetCurrentDirectory returning the absolute
                    // path that followed the symlink.
                    Assert.Equal(TestDirectory, Directory.GetCurrentDirectory());
                }

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void CurrentManagedThreadId_Idempotent()
        {
            Assert.Equal(Environment.CurrentManagedThreadId, Environment.CurrentManagedThreadId);
        }

        [Fact]
        public void CurrentManagedThreadId_DifferentForActiveThreads()
        {
            var ids = new HashSet<int>();
            Barrier b = new Barrier(10);
            Task.WaitAll((from i in Enumerable.Range(0, b.ParticipantCount)
                          select Task.Factory.StartNew(() =>
                          {
                              b.SignalAndWait();
                              lock (ids) ids.Add(Environment.CurrentManagedThreadId);
                              b.SignalAndWait();
                          }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());
            Assert.Equal(b.ParticipantCount, ids.Count);
        }

        [Fact]
        public void HasShutdownStarted_FalseWhileExecuting()
        {
            Assert.False(Environment.HasShutdownStarted);
        }

        [Fact]
        public void Is64BitProcess_MatchesIntPtrSize()
        {
            Assert.Equal(IntPtr.Size == 8, Environment.Is64BitProcess);
        }

        [Fact]
        public void Is64BitOperatingSystem_TrueIf64BitProcess()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(Environment.Is64BitOperatingSystem);
            }
        }

        [Fact]
        [PlatformSpecific(Xunit.PlatformID.AnyUnix)]
        public void Is64BitOperatingSystem_Unix_TrueIff64BitProcess()
        {
            Assert.Equal(Environment.Is64BitProcess, Environment.Is64BitOperatingSystem);
        }

        [Fact]
        public void OSVersion_Idempotent()
        {
            Assert.Same(Environment.OSVersion, Environment.OSVersion);
        }

        [Fact]
        public void OSVersion_MatchesPlatform()
        {
            PlatformID id = Environment.OSVersion.Platform;
            Assert.Equal(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PlatformID.Win32NT : PlatformID.Unix,
                id);
        }

        [Fact]
        public void OSVersion_ValidVersion()
        {
            Version version = Environment.OSVersion.Version;
            string versionString = Environment.OSVersion.VersionString;

            Assert.False(string.IsNullOrWhiteSpace(versionString), "Expected non-empty version string");
            Assert.True(version.Major > 0);

            Assert.Contains(version.ToString(2), versionString);
            Assert.Contains(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Unix", versionString);
        }

        [Fact]
        public void SystemPageSize_Valid()
        {
            int pageSize = Environment.SystemPageSize;
            Assert.Equal(pageSize, Environment.SystemPageSize);

            Assert.True(pageSize > 0, "Expected positive page size");
            Assert.True((pageSize & (pageSize - 1)) == 0, "Expected power-of-2 page size");
        }

        [Fact]
        public void UserInteractive_True()
        {
            Assert.True(Environment.UserInteractive);
        }

        [Fact]
        public void UserName_Valid()
        {
            Assert.False(string.IsNullOrWhiteSpace(Environment.UserName));
        }

        [Fact]
        public void UserDomainName_Valid()
        {
            Assert.False(string.IsNullOrWhiteSpace(Environment.UserDomainName));
        }

        [Fact]
        [PlatformSpecific(Xunit.PlatformID.AnyUnix)]
        public void UserDomainName_Unix_MatchesMachineName()
        {
            Assert.Equal(Environment.MachineName, Environment.UserDomainName);
        }

        [Fact]
        public void Version_MatchesFixedVersion()
        {
            Assert.Equal(new Version(4, 0, 30319, 42000), Environment.Version);
        }

        [Fact]
        public void WorkingSet_Valid()
        {
            Assert.True(Environment.WorkingSet > 0, "Expected positive WorkingSet value");
        }

        [Fact]
        public void FailFast_ExpectFailureExitCode()
        {
            using (Process p = RemoteInvoke(() => { Environment.FailFast("message"); return SuccessExitCode; }).Process)
            {
                p.WaitForExit();
                Assert.NotEqual(SuccessExitCode, p.ExitCode);
            }

            using (Process p = RemoteInvoke(() => { Environment.FailFast("message", new Exception("uh oh")); return SuccessExitCode; }).Process)
            {
                p.WaitForExit();
                Assert.NotEqual(SuccessExitCode, p.ExitCode);
            }
        }

        [Fact]
        [PlatformSpecific(Xunit.PlatformID.AnyUnix)]
        public void GetFolderPath_Unix_PersonalIsHome()
        {
            Assert.Equal(Environment.GetEnvironmentVariable("HOME"), Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            Assert.Equal(Environment.GetEnvironmentVariable("HOME"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        [Fact]
        [PlatformSpecific(Xunit.PlatformID.AnyUnix)]
        public void GetLogicalDrives_Unix_AtLeastOneIsRoot()
        {
            string[] drives = Environment.GetLogicalDrives();
            Assert.NotNull(drives);
            Assert.True(drives.Length > 0, "Expected at least one drive");
            Assert.All(drives, d => Assert.NotNull(d));
            Assert.Contains(drives, d => d == "/");
        }

        [Fact]
        [PlatformSpecific(Xunit.PlatformID.Windows)]
        public void GetLogicalDrives_Windows_MatchesExpectedLetters()
        {
            string[] drives = Environment.GetLogicalDrives();

            uint mask = (uint)GetLogicalDrives();
            var bits = new BitArray(new[] { (int)mask });

            Assert.Equal(bits.Cast<bool>().Count(b => b), drives.Length);
            for (int i = 0; i < drives.Length; i++)
            {
                if (bits[i])
                {
                    Assert.Contains((char)('A' + i), drives[i]);
                }
            }
        }

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal static extern int GetLogicalDrives();
    }
}
