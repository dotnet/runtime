// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Tests
{
    public class EnvironmentTests : FileCleanupTestBase
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CurrentDirectory_SetToValidOtherDirectory()
        {
            RemoteExecutor.Invoke(() =>
            {
                Environment.CurrentDirectory = TestDirectory;
                Assert.Equal(Directory.GetCurrentDirectory(), Environment.CurrentDirectory);

                if (!OperatingSystem.IsMacOS())
                {
                    // On OSX, the temp directory /tmp/ is a symlink to /private/tmp, so setting the current
                    // directory to a symlinked path will result in GetCurrentDirectory returning the absolute
                    // path that followed the symlink.
                    Assert.Equal(TestDirectory, Directory.GetCurrentDirectory());
                }
            }).Dispose();
        }

        [Fact]
        public void CurrentManagedThreadId_Idempotent()
        {
            Assert.Equal(Environment.CurrentManagedThreadId, Environment.CurrentManagedThreadId);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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
        public void ProcessId_Idempotent()
        {
            Assert.InRange(Environment.ProcessId, 1, int.MaxValue);
            Assert.Equal(Environment.ProcessId, Environment.ProcessId);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ProcessId_MatchesExpectedValue()
        {
            using RemoteInvokeHandle handle = RemoteExecutor.Invoke(() => Console.WriteLine(Environment.ProcessId), new RemoteInvokeOptions { StartInfo = new ProcessStartInfo { RedirectStandardOutput = true } });
            Assert.Equal(handle.Process.Id, int.Parse(handle.Process.StandardOutput.ReadToEnd()));
        }

        [Fact]
        public void ProcessPath_Idempotent()
        {
            Assert.Same(Environment.ProcessPath, Environment.ProcessPath);
        }

        [Fact]
        public void ProcessPath_MatchesExpectedValue()
        {
            string expectedProcessPath = PlatformDetection.IsBrowser ? null : Process.GetCurrentProcess().MainModule.FileName;
            Assert.Equal(expectedProcessPath, Environment.ProcessPath);
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
            PlatformID expected = OperatingSystem.IsWindows() ? PlatformID.Win32NT : OperatingSystem.IsBrowser() ? PlatformID.Other : PlatformID.Unix;
            Assert.Equal(expected, id);
        }

        [Fact]
        public void OSVersion_ValidVersion()
        {
            Version version = Environment.OSVersion.Version;
            string versionString = Environment.OSVersion.VersionString;

            Assert.False(string.IsNullOrWhiteSpace(versionString), "Expected non-empty version string");
            Assert.True(version.Major > 0);

            Assert.Contains(version.ToString(2), versionString);

            string expectedOS = OperatingSystem.IsWindows() ? "Windows " : OperatingSystem.IsBrowser() ? "Other " : "Unix ";
            Assert.Contains(expectedOS, versionString);
        }

        // On non-OSX Unix, we must parse the version from uname -r
        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.OSX & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.MacCatalyst)]
        [InlineData("2.6.19-1.2895.fc6", 2, 6, 19, 1)]
        [InlineData("xxx1yyy2zzz3aaa4bbb", 1, 2, 3, 4)]
        [InlineData("2147483647.2147483647.2147483647.2147483647", int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)]
        [InlineData("0.0.0.0", 0, 0, 0, 0)]
        [InlineData("-1.-1.-1.-1", 1, 1, 1, 1)]
        [InlineData("nelknet 4.15.0-10000000000-generic", 4, 15, 0, int.MaxValue)] // integer overflow
        [InlineData("nelknet 4.15.0-24201807041620-generic", 4, 15, 0, int.MaxValue)] // integer overflow
        [InlineData("", 0, 0, 0, 0)]
        [InlineData("1abc", 1, 0, 0, 0)]
        public void OSVersion_ParseVersion(string input, int major, int minor, int build, int revision)
        {
            var getOSMethod = typeof(Environment).GetMethod("GetOperatingSystem", BindingFlags.Static | BindingFlags.NonPublic);

            var expected = new Version(major, minor, build, revision);
            var actual = ((OperatingSystem)getOSMethod.Invoke(null, new object[] { input })).Version;

            Assert.Equal(expected, actual);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49106", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
        public void OSVersion_ValidVersion_OSX()
        {
            Version version = Environment.OSVersion.Version;

            // verify that the Environment.OSVersion.Version matches the current RID
            Assert.Contains(version.ToString(2), RuntimeInformation.RuntimeIdentifier);

            Assert.True(version.Build >= 0, "OSVersion Build should be non-negative");
            Assert.Equal(-1, version.Revision); // Revision is never set on OSX
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
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void UserInteractive_Unix_True()
        {
            Assert.True(Environment.UserInteractive);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UserInteractive_Windows_DoesNotThrow()
        {
            var dummy = Environment.UserInteractive; // Does not throw
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindowsNanoServer))]
        public void UserInteractive_WindowsNano()
        {
            // Defaults to true on Nano, because it doesn't expose WindowStations
            Assert.True(Environment.UserInteractive);
        }

        [Fact]
        public void Version_Valid()
        {
            Assert.True(Environment.Version >= new Version(3, 0));
        }

        [Fact]
        public void WorkingSet_Valid()
        {
            if (PlatformDetection.IsBrowser)
                Assert.Equal(0, Environment.WorkingSet);
            else
                Assert.True(Environment.WorkingSet > 0, "Expected positive WorkingSet value");
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // fail fast crashes the process
        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void FailFast_ExpectFailureExitCode()
        {
            using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(() => Environment.FailFast("message")))
            {
                Process p = handle.Process;
                handle.Process = null;
                p.WaitForExit();
                Assert.NotEqual(RemoteExecutor.SuccessExitCode, p.ExitCode);
            }

            using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(() => Environment.FailFast("message", new Exception("uh oh"))))
            {
                Process p = handle.Process;
                handle.Process = null;
                p.WaitForExit();
                Assert.NotEqual(RemoteExecutor.SuccessExitCode, p.ExitCode);
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // fail fast crashes the process
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void FailFast_ExceptionStackTrace_ArgumentException()
        {
            var psi = new ProcessStartInfo();
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(
                () => Environment.FailFast("message", new ArgumentException("bad arg")),
                new RemoteInvokeOptions { StartInfo = psi }))
            {
                Process p = handle.Process;
                handle.Process = null;
                p.WaitForExit();
                string consoleOutput = p.StandardError.ReadToEnd();
                Assert.Contains("ArgumentException:", consoleOutput);
                Assert.Contains("bad arg", consoleOutput);
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // fail fast crashes the process
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void FailFast_ExceptionStackTrace_StackOverflowException()
        {
            // Test using another type of exception
            var psi = new ProcessStartInfo();
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(
                () => Environment.FailFast("message", new StackOverflowException("SO exception")),
                new RemoteInvokeOptions { StartInfo = psi }))
            {
                Process p = handle.Process;
                handle.Process = null;
                p.WaitForExit();
                string consoleOutput = p.StandardError.ReadToEnd();
                Assert.Contains("StackOverflowException", consoleOutput);
                Assert.Contains("SO exception", consoleOutput);
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // fail fast crashes the process
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void FailFast_ExceptionStackTrace_InnerException()
        {
            // Test if inner exception details are also logged
            var psi = new ProcessStartInfo();
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(
                () => Environment.FailFast("message", new ArgumentException("first exception", new NullReferenceException("inner exception"))),
                new RemoteInvokeOptions { StartInfo = psi }))
            {
                Process p = handle.Process;
                handle.Process = null;
                p.WaitForExit();
                string consoleOutput = p.StandardError.ReadToEnd();
                Assert.Contains("first exception", consoleOutput);
                Assert.Contains("inner exception", consoleOutput);
                Assert.Contains("ArgumentException", consoleOutput);
                Assert.Contains("NullReferenceException", consoleOutput);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix | TestPlatforms.Browser)]
        public void GetFolderPath_Unix_PersonalExists()
        {
            Assert.True(Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Personal)));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix | TestPlatforms.Browser)]  // Tests OS-specific environment
        public void GetFolderPath_Unix_PersonalIsHomeAndUserProfile()
        {
            if (!PlatformDetection.IsiOS && !PlatformDetection.IstvOS && !PlatformDetection.IsMacCatalyst)
            {
                Assert.Equal(Environment.GetEnvironmentVariable("HOME"), Environment.GetFolderPath(Environment.SpecialFolder.Personal));
                Assert.Equal(Environment.GetEnvironmentVariable("HOME"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            }
            // tvOS effectively doesn't have a HOME
            if (!PlatformDetection.IsiOS && !PlatformDetection.IstvOS)
            {
                Assert.Equal(Environment.GetEnvironmentVariable("HOME"), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }
        }

        [Theory]
        [OuterLoop]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests OS-specific environment
        [InlineData(Environment.SpecialFolder.ApplicationData)]
        [InlineData(Environment.SpecialFolder.Desktop)]
        [InlineData(Environment.SpecialFolder.DesktopDirectory)]
        [InlineData(Environment.SpecialFolder.Fonts)]
        [InlineData(Environment.SpecialFolder.MyMusic)]
        [InlineData(Environment.SpecialFolder.MyPictures)]
        [InlineData(Environment.SpecialFolder.MyVideos)]
        [InlineData(Environment.SpecialFolder.Templates)]
        public void GetFolderPath_Unix_SpecialFolderDoesNotExist_CreatesSuccessfully(Environment.SpecialFolder folder)
        {
            string path = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
            if (Directory.Exists(path))
                return;
            path = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.Create);
            Assert.True(Directory.Exists(path));
            Directory.Delete(path);
        }

        [Fact]
        public void GetSystemDirectory()
        {
            if (PlatformDetection.IsWindowsNanoServer)
            {
                // https://github.com/dotnet/runtime/issues/21430
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
        [InlineData(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.None)] // MyDocuments == Personal
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49868", TestPlatforms.Android)]
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

        // Requires recent RS3 builds and needs to run inside AppContainer
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version1709OrGreater), nameof(PlatformDetection.IsInAppContainer))]
        [InlineData(Environment.SpecialFolder.LocalApplicationData)]
        [InlineData(Environment.SpecialFolder.Cookies)]
        [InlineData(Environment.SpecialFolder.History)]
        [InlineData(Environment.SpecialFolder.InternetCache)]
        [InlineData(Environment.SpecialFolder.System)]
        [InlineData(Environment.SpecialFolder.SystemX86)]
        [InlineData(Environment.SpecialFolder.Windows)]
        public void GetFolderPath_UWP_ExistAndAccessible(Environment.SpecialFolder folder)
        {
            string knownFolder = Environment.GetFolderPath(folder);
            Assert.NotEmpty(knownFolder);
            AssertDirectoryExists(knownFolder);
        }

        // Requires recent RS3 builds and needs to run inside AppContainer
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version1709OrGreater), nameof(PlatformDetection.IsInAppContainer))]
        [InlineData(Environment.SpecialFolder.ApplicationData)]
        [InlineData(Environment.SpecialFolder.MyMusic)]
        [InlineData(Environment.SpecialFolder.MyPictures)]
        [InlineData(Environment.SpecialFolder.MyVideos)]
        [InlineData(Environment.SpecialFolder.Recent)]
        [InlineData(Environment.SpecialFolder.Templates)]
        [InlineData(Environment.SpecialFolder.DesktopDirectory)]
        [InlineData(Environment.SpecialFolder.Personal)]
        [InlineData(Environment.SpecialFolder.UserProfile)]
        [InlineData(Environment.SpecialFolder.CommonDocuments)]
        [InlineData(Environment.SpecialFolder.CommonMusic)]
        [InlineData(Environment.SpecialFolder.CommonPictures)]
        [InlineData(Environment.SpecialFolder.CommonDesktopDirectory)]
        [InlineData(Environment.SpecialFolder.CommonVideos)]
        // These are in the package folder
        [InlineData(Environment.SpecialFolder.CommonApplicationData)]
        [InlineData(Environment.SpecialFolder.Desktop)]
        [InlineData(Environment.SpecialFolder.Favorites)]
        public void GetFolderPath_UWP_NotEmpty(Environment.SpecialFolder folder)
        {
            // The majority of the paths here cannot be accessed from an appcontainer
            string knownFolder = Environment.GetFolderPath(folder);
            Assert.NotEmpty(knownFolder);
        }

        private void AssertDirectoryExists(string path)
        {
            // Directory.Exists won't tell us if access was denied, etc. Invoking directly
            // to get diagnosable test results.

            FileAttributes attributes = GetFileAttributesW(path);
            if (attributes == (FileAttributes)(-1))
            {
                int error = Marshal.GetLastPInvokeError();
                Assert.False(true, $"error {error} getting attributes for {path}");
            }

            Assert.True((attributes & FileAttributes.Directory) == FileAttributes.Directory, $"not a directory: {path}");
        }

        public static IEnumerable<object[]> GetFolderPath_WindowsTestData
        {
            get
            {
                yield return new object[] { Environment.SpecialFolder.ApplicationData };
                yield return new object[] { Environment.SpecialFolder.CommonApplicationData };
                yield return new object[] { Environment.SpecialFolder.LocalApplicationData };
                yield return new object[] { Environment.SpecialFolder.Cookies };
                yield return new object[] { Environment.SpecialFolder.Desktop };
                yield return new object[] { Environment.SpecialFolder.Favorites };
                yield return new object[] { Environment.SpecialFolder.History };
                yield return new object[] { Environment.SpecialFolder.InternetCache };
                yield return new object[] { Environment.SpecialFolder.Programs };
                yield return new object[] { Environment.SpecialFolder.MyMusic };
                yield return new object[] { Environment.SpecialFolder.MyPictures };
                yield return new object[] { Environment.SpecialFolder.MyVideos };
                yield return new object[] { Environment.SpecialFolder.Recent };
                yield return new object[] { Environment.SpecialFolder.SendTo };
                yield return new object[] { Environment.SpecialFolder.StartMenu };
                yield return new object[] { Environment.SpecialFolder.System };
                yield return new object[] { Environment.SpecialFolder.DesktopDirectory };
                yield return new object[] { Environment.SpecialFolder.Personal };
                yield return new object[] { Environment.SpecialFolder.ProgramFiles };
                yield return new object[] { Environment.SpecialFolder.CommonProgramFiles };
                yield return new object[] { Environment.SpecialFolder.CommonAdminTools };
                yield return new object[] { Environment.SpecialFolder.CommonDocuments };
                yield return new object[] { Environment.SpecialFolder.CommonMusic };
                yield return new object[] { Environment.SpecialFolder.CommonPictures };
                yield return new object[] { Environment.SpecialFolder.CommonStartMenu };
                yield return new object[] { Environment.SpecialFolder.CommonPrograms };
                yield return new object[] { Environment.SpecialFolder.CommonStartup };
                yield return new object[] { Environment.SpecialFolder.CommonDesktopDirectory };
                yield return new object[] { Environment.SpecialFolder.CommonTemplates };
                yield return new object[] { Environment.SpecialFolder.CommonVideos };
                yield return new object[] { Environment.SpecialFolder.Fonts };
                yield return new object[] { Environment.SpecialFolder.UserProfile };
                yield return new object[] { Environment.SpecialFolder.CommonProgramFilesX86 };
                yield return new object[] { Environment.SpecialFolder.ProgramFilesX86 };
                yield return new object[] { Environment.SpecialFolder.Resources };
                yield return new object[] { Environment.SpecialFolder.SystemX86 };
                yield return new object[] { Environment.SpecialFolder.Windows };

                if (PlatformDetection.IsNotWindowsNanoNorServerCore)
                {
                    // Our windows docker containers don't have these folders.
                    yield return new object[] { Environment.SpecialFolder.Startup };
                    yield return new object[] { Environment.SpecialFolder.AdminTools };
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // https://github.com/dotnet/runtime/issues/21430
        [MemberData(nameof(GetFolderPath_WindowsTestData))]
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

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern FileAttributes GetFileAttributesW(string lpFileName);

        public static IEnumerable<object[]> EnvironmentVariableTargets
        {
            get
            {
                yield return new object[] { EnvironmentVariableTarget.Process };
                yield return new object[] { EnvironmentVariableTarget.User };
                yield return new object[] { EnvironmentVariableTarget.Machine };
            }
        }
    }
}
