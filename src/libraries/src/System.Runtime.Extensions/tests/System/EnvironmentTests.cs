// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.Tests
{
    public class EnvironmentTests : RemoteExecutorTestBase
    {
        [Fact]
        public void CurrentDirectory_Null_Path_Throws_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => Environment.CurrentDirectory = null);
        }

        [Fact]
        public void CurrentDirectory_Empty_Path_Throws_ArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("value", null, () => Environment.CurrentDirectory = string.Empty);
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
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests OS-specific environment
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
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests OS-specific environment
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

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // fail fast crashes the process
        [OuterLoop]
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
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests OS-specific environment
        public void GetFolderPath_Unix_PersonalIsHomeAndUserProfile()
        {
            Assert.Equal(Environment.GetEnvironmentVariable("HOME"), Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            Assert.Equal(Environment.GetEnvironmentVariable("HOME"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            Assert.Equal(Environment.GetEnvironmentVariable("HOME"), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        [Fact]
        public void GetSystemDirectory()
        {
            if (PlatformDetection.IsWindowsNanoServer)
            {
                // https://github.com/dotnet/corefx/issues/19110
                // On Windows Nano, ShGetKnownFolderPath currently doesn't give
                // the correct result for SystemDirectory.
                // Assert that it's wrong, so that if it's fixed, we don't forget to
                // enable this test for Nano.
                Assert.NotEqual(Environment.GetFolderPath(Environment.SpecialFolder.System), Environment.SystemDirectory);
                return;
            }

            Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.System), Environment.SystemDirectory);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests OS-specific environment
        [InlineData(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.None)]
        [InlineData(Environment.SpecialFolder.Personal, Environment.SpecialFolderOption.None)]
        [InlineData(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.None)]
        [InlineData(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.None)]
        [InlineData(Environment.SpecialFolder.CommonTemplates, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.Desktop, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolderOption.DoNotVerify)]
        // Not set on Unix (amongst others)
        //[InlineData(Environment.SpecialFolder.System, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.Templates, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.MyVideos, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.MyMusic, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.Fonts, Environment.SpecialFolderOption.DoNotVerify)]
        public void GetFolderPath_Unix_NonEmptyFolderPaths(Environment.SpecialFolder folder, Environment.SpecialFolderOption option)
        {
            Assert.NotEmpty(Environment.GetFolderPath(folder, option));
            if (option == Environment.SpecialFolderOption.None)
            {
                Assert.NotEmpty(Environment.GetFolderPath(folder));
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.OSX)]  // Tests OS-specific environment
        [InlineData(Environment.SpecialFolder.Favorites, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.InternetCache, Environment.SpecialFolderOption.DoNotVerify)]
        [InlineData(Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolderOption.None)]
        [InlineData(Environment.SpecialFolder.System, Environment.SpecialFolderOption.None)]
        public void GetFolderPath_OSX_NonEmptyFolderPaths(Environment.SpecialFolder folder, Environment.SpecialFolderOption option)
        {
            Assert.NotEmpty(Environment.GetFolderPath(folder, option));
            if (option == Environment.SpecialFolderOption.None)
            {
                Assert.NotEmpty(Environment.GetFolderPath(folder));
            }
        }

        // The commented out folders aren't set on all systems.
        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))] // https://github.com/dotnet/corefx/issues/19110
        [InlineData(Environment.SpecialFolder.ApplicationData)]
        [InlineData(Environment.SpecialFolder.CommonApplicationData)]
        [InlineData(Environment.SpecialFolder.LocalApplicationData)]
        [InlineData(Environment.SpecialFolder.Cookies)]
        [InlineData(Environment.SpecialFolder.Desktop)]
        [InlineData(Environment.SpecialFolder.Favorites)]
        [InlineData(Environment.SpecialFolder.History)]
        [InlineData(Environment.SpecialFolder.InternetCache)]
        [InlineData(Environment.SpecialFolder.Programs)]
        // [InlineData(Environment.SpecialFolder.MyComputer)]
        [InlineData(Environment.SpecialFolder.MyMusic)]
        [InlineData(Environment.SpecialFolder.MyPictures)]
        [InlineData(Environment.SpecialFolder.MyVideos)]
        [InlineData(Environment.SpecialFolder.Recent)]
        [InlineData(Environment.SpecialFolder.SendTo)]
        [InlineData(Environment.SpecialFolder.StartMenu)]
        [InlineData(Environment.SpecialFolder.Startup)]
        [InlineData(Environment.SpecialFolder.System)]
        [InlineData(Environment.SpecialFolder.Templates)]
        [InlineData(Environment.SpecialFolder.DesktopDirectory)]
        [InlineData(Environment.SpecialFolder.Personal)]
        [InlineData(Environment.SpecialFolder.ProgramFiles)]
        [InlineData(Environment.SpecialFolder.CommonProgramFiles)]
        [InlineData(Environment.SpecialFolder.AdminTools)]
        [InlineData(Environment.SpecialFolder.CDBurning)]
        [InlineData(Environment.SpecialFolder.CommonAdminTools)]
        [InlineData(Environment.SpecialFolder.CommonDocuments)]
        [InlineData(Environment.SpecialFolder.CommonMusic)]
        // [InlineData(Environment.SpecialFolder.CommonOemLinks)]
        [InlineData(Environment.SpecialFolder.CommonPictures)]
        [InlineData(Environment.SpecialFolder.CommonStartMenu)]
        [InlineData(Environment.SpecialFolder.CommonPrograms)]
        [InlineData(Environment.SpecialFolder.CommonStartup)]
        [InlineData(Environment.SpecialFolder.CommonDesktopDirectory)]
        [InlineData(Environment.SpecialFolder.CommonTemplates)]
        [InlineData(Environment.SpecialFolder.CommonVideos)]
        [InlineData(Environment.SpecialFolder.Fonts)]
        [InlineData(Environment.SpecialFolder.NetworkShortcuts)]
        // [InlineData(Environment.SpecialFolder.PrinterShortcuts)]
        [InlineData(Environment.SpecialFolder.UserProfile)]
        [InlineData(Environment.SpecialFolder.CommonProgramFilesX86)]
        [InlineData(Environment.SpecialFolder.ProgramFilesX86)]
        [InlineData(Environment.SpecialFolder.Resources)]
        // [InlineData(Environment.SpecialFolder.LocalizedResources)]
        [InlineData(Environment.SpecialFolder.SystemX86)]
        [InlineData(Environment.SpecialFolder.Windows)]
        [PlatformSpecific(TestPlatforms.Windows)]  // Tests OS-specific environment
        public unsafe void GetFolderPath_Windows(Environment.SpecialFolder folder)
        {
            string knownFolder = Environment.GetFolderPath(folder);

            Assert.NotEmpty(knownFolder);

            // Call the older folder API to compare our results.
            char* buffer = stackalloc char[260];
            SHGetFolderPathW(IntPtr.Zero, (int)folder, IntPtr.Zero, 0, buffer);
            string folderPath = new string(buffer);

            Assert.Equal(folderPath, knownFolder);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Uses P/Invokes
        public void GetLogicalDrives_Unix_AtLeastOneIsRoot()
        {
            string[] drives = Environment.GetLogicalDrives();
            Assert.NotNull(drives);
            Assert.True(drives.Length > 0, "Expected at least one drive");
            Assert.All(drives, d => Assert.NotNull(d));
            Assert.Contains(drives, d => d == "/");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public void GetLogicalDrives_Windows_MatchesExpectedLetters()
        {
            string[] drives = Environment.GetLogicalDrives();

            uint mask = (uint)GetLogicalDrives();
            var bits = new BitArray(new[] { (int)mask });

            Assert.Equal(bits.Cast<bool>().Count(b => b), drives.Length);
            for (int bit = 0, d = 0; bit < bits.Length; bit++)
            {
                if (bits[bit])
                {
                    Assert.Contains((char)('A' + bit), drives[d++]);
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int GetLogicalDrives();

        [DllImport("shell32.dll", SetLastError = false, BestFitMapping = false, ExactSpelling = true)]
        internal static extern unsafe int SHGetFolderPathW(
            IntPtr hwndOwner,
            int nFolder,
            IntPtr hToken,
            uint dwFlags,
            char* pszPath);

        public static IEnumerable<object[]> EnvironmentVariableTargets
        {
            get
            {
                yield return new object[] { EnvironmentVariableTarget.Process };
                if (!(s_EnvironmentRegKeysStillAccessDenied.Value))
                {
                    yield return new object[] { EnvironmentVariableTarget.User };
                    yield return new object[] { EnvironmentVariableTarget.Machine };
                }
            }
        }

        private static readonly Lazy<bool> s_EnvironmentRegKeysStillAccessDenied = new Lazy<bool>(
            delegate ()
            {
                if (!PlatformDetection.IsWindows)
                    return false;  // On Unix, registry-based environment api's won't throw a SecurityException - they just eat all writes.
                if (!PlatformDetection.IsWinRT)
                    return false;  // On non-appcontainer apps, these won't throw (except writes to Target.Machine on non-elevated but that's accounted for separately.)

                try
                {
                    Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
                }
                catch (SecurityException)
                {
                    return true; // AppX registry exemptions not yet granted (at least on this build.)
                }
                catch
                {
                    return false; // Hmm... some other exception. We'll enable the individual tests and let them report it...
                }
                return false;
            });
    }
}
