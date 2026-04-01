// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Diagnostics.Tests
{
    partial class ProcessStartInfoTests : ProcessTestBase
    {
        private static bool IsAdmin_IsNotNano_RemoteExecutorIsSupported_CanShareFiles
            => IsAdmin_IsNotNano_RemoteExecutorIsSupported && WindowsTestFileShare.CanShareFiles;

        [ConditionalFact(typeof(ProcessStartInfoTests), nameof(IsAdmin_IsNotNano_RemoteExecutorIsSupported_CanShareFiles))] // Nano has no "netapi32.dll", Admin rights are required
        [PlatformSpecific(TestPlatforms.Windows)]
        [OuterLoop("Requires admin privileges")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/80019", TestRuntimes.Mono)]
        public void TestUserNetworkCredentialsPropertiesOnWindows()
        {
            const string ShareName = "testForDotNet";
            const string TestFileContent = "42";
            const string UncPathEnvVar = nameof(UncPathEnvVar);

            string testFilePath = GetTestFilePath();
            File.WriteAllText(testFilePath, TestFileContent);

            using WindowsTestFileShare fileShare = new WindowsTestFileShare(ShareName, Path.GetDirectoryName(testFilePath));
            string testFileUncPath = $"\\\\{Environment.MachineName}\\{ShareName}\\{Path.GetFileName(testFilePath)}";

            using Process process = CreateProcess(() =>
            {
                try
                {
                    Assert.Equal(TestFileContent, File.ReadAllText(Environment.GetEnvironmentVariable(UncPathEnvVar)));

                    return RemoteExecutor.SuccessExitCode;
                }
                catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
                {
                    return -1;
                }
            });
            process.StartInfo.Environment[UncPathEnvVar] = testFileUncPath;
            process.StartInfo.UseCredentialsForNetworkingOnly = true;

            using TestProcessState processInfo = CreateUserAndExecute(process, Setup, Cleanup);

            Assert.Equal(Environment.UserName, Helpers.GetProcessUserName(process));

            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, process.ExitCode);

            void Setup(string username, string _)
            {
                if (PlatformDetection.IsNotWindowsServerCore) // for this particular Windows version it fails with Attempted to perform an unauthorized operation (#46619)
                {
                    SetAccessControl(username, testFilePath, Path.GetDirectoryName(testFilePath), add: true);
                }
            }

            void Cleanup(string username, string _)
            {
                if (PlatformDetection.IsNotWindowsServerCore)
                {
                    // remove the access
                    SetAccessControl(username, testFilePath, Path.GetDirectoryName(testFilePath), add: false);
                }
            }
        }

        [ConditionalTheory(typeof(ProcessStartInfoTests), nameof(IsAdmin_IsNotNano_RemoteExecutorIsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(ProcessWindowStyle.Normal)]
        [InlineData(ProcessWindowStyle.Hidden)]
        [InlineData(ProcessWindowStyle.Minimized)]
        [InlineData(ProcessWindowStyle.Maximized)]
        public void TestWindowStyle(ProcessWindowStyle windowStyle)
        {
            (bool expectUsesShowWindow, int expectedWindowFlag) = windowStyle switch
            {
                ProcessWindowStyle.Hidden => (true, 0), // SW_HIDE is 0
                ProcessWindowStyle.Minimized => (true, 2), // SW_SHOWMINIMIZED is 2
                ProcessWindowStyle.Maximized => (true, 3), // SW_SHOWMAXIMIZED is 3
                _ => (false, 0),
            };

            using Process p = CreateProcess((string procArg) =>
            {
                Interop.GetStartupInfoW(out Interop.STARTUPINFO si);

                string[] argSplit = procArg.Split(" ");
                bool expectUsesShowWindow = bool.Parse(argSplit[0]);
                short expectedWindowFlag = short.Parse(argSplit[1]);

                Assert.Equal(expectUsesShowWindow, (si.dwFlags & 0x1) != 0); // STARTF_USESHOWWINDOW is 0x1
                Assert.Equal(expectedWindowFlag, si.wShowWindow);
                return RemoteExecutor.SuccessExitCode;
            }, $"{expectUsesShowWindow} {expectedWindowFlag}");
            p.StartInfo.WindowStyle = windowStyle;
            p.Start();

            Assert.True(p.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, p.ExitCode);
        }

        [ConditionalFact(typeof(ProcessStartInfoTests), nameof(IsAdmin_IsNotNano_RemoteExecutorIsSupported))] // Nano has no "netapi32.dll", Admin rights are required
        [PlatformSpecific(TestPlatforms.Windows)]
        [OuterLoop("Requires admin privileges")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/80019", TestRuntimes.Mono)]
        public void TestUserCredentialsAndRestrictedHandles()
        {
            // CreateProcessAsUserW (used when EXTENDED_STARTUPINFO_PRESENT is set) requires these privileges.
            // CreateProcessWithLogonW (used otherwise) does not require them.
            // Only enable privileges when we'll actually use CreateProcessAsUserW.
            PrivilegeHelper? increaseQuotaPrivilege = new("SeIncreaseQuotaPrivilege");
            PrivilegeHelper? assignPrimaryTokenPrivilege = new("SeAssignPrimaryTokenPrivilege");

            try
            {
                increaseQuotaPrivilege.Enable();
                assignPrimaryTokenPrivilege.Enable();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1300) // ERROR_NOT_ALL_ASSIGNED
            {
                // In case the first worked and the second has failed.
                increaseQuotaPrivilege.Revert();

                throw new SkipTestException("Test requires SeIncreaseQuotaPrivilege and SeAssignPrimaryTokenPrivilege.");
            }

            using Process longRunning = CreateProcessLong();
            longRunning.StartInfo.LoadUserProfile = true;
            longRunning.StartInfo.InheritedHandles = [];

            try
            {
                using TestProcessState testAccountCleanup = CreateUserAndExecute(longRunning, Setup, Cleanup);

                string username = testAccountCleanup.ProcessAccountName.Split('\\').Last();
                Assert.Equal(username, Helpers.GetProcessUserName(longRunning));
                bool isProfileLoaded = GetNamesOfUserProfiles().Any(profile => profile.Equals(username));
                Assert.True(isProfileLoaded);

                void Setup(string username, string workingDirectory)
                {
                    if (PlatformDetection.IsNotWindowsServerCore) // for this particular Windows version it fails with Attempted to perform an unauthorized operation (#46619)
                    {
                        // ensure the new user can access the .exe (otherwise you get Access is denied exception)
                        SetAccessControl(username, longRunning.StartInfo.FileName, workingDirectory, add: true);
                    }
                }

                void Cleanup(string username, string workingDirectory)
                {
                    if (PlatformDetection.IsNotWindowsServerCore)
                    {
                        // remove the access
                        SetAccessControl(username, longRunning.StartInfo.FileName, workingDirectory, add: false);
                    }
                }
            }
            finally
            {
                increaseQuotaPrivilege?.Revert();
                assignPrimaryTokenPrivilege?.Revert();
            }
        }

        private sealed class PrivilegeHelper
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct LUID
            {
                public uint LowPart;
                public int HighPart;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct LUID_AND_ATTRIBUTES
            {
                public LUID Luid;
                public uint Attributes;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct TOKEN_PRIVILEGE
            {
                public uint PrivilegeCount;
                public LUID_AND_ATTRIBUTES Privileges;
            }

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern bool LookupPrivilegeValueW(string? lpSystemName, string lpName, out LUID lpLuid);

            [DllImport("kernel32.dll")]
            private static extern IntPtr GetCurrentProcess();

            [DllImport("advapi32.dll", SetLastError = true)]
            private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

            [DllImport("advapi32.dll", SetLastError = true)]
            private static unsafe extern bool AdjustTokenPrivileges(
                IntPtr TokenHandle,
                bool DisableAllPrivileges,
                TOKEN_PRIVILEGE* NewState,
                uint BufferLength,
                TOKEN_PRIVILEGE* PreviousState,
                uint* ReturnLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool CloseHandle(IntPtr hObject);

            private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
            private const uint TOKEN_QUERY = 0x0008;
            private const uint SE_PRIVILEGE_ENABLED = 0x00000002;

            private bool _needToRevert;
            private readonly LUID _luid;
            private IntPtr _tokenHandle;

            public PrivilegeHelper(string privilegeName)
            {
                if (!LookupPrivilegeValueW(null, privilegeName, out _luid))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out _tokenHandle))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            public unsafe void Enable()
            {
                TOKEN_PRIVILEGE newState;
                newState.PrivilegeCount = 1;
                newState.Privileges.Luid = _luid;
                newState.Privileges.Attributes = SE_PRIVILEGE_ENABLED;

                if (!AdjustTokenPrivileges(_tokenHandle, false, &newState, 0, null, null))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                // AdjustTokenPrivileges can return true but still fail to enable privileges!
                // Check GetLastError() for ERROR_NOT_ALL_ASSIGNED (1300)
                int error = Marshal.GetLastWin32Error();
                if (error == 1300) // ERROR_NOT_ALL_ASSIGNED
                {
                    throw new Win32Exception(error, "Failed to enable privilege. The process token does not have this privilege.");
                }

                _needToRevert = true;
            }

            public unsafe void Revert()
            {
                if (_needToRevert)
                {
                    TOKEN_PRIVILEGE newState;
                    newState.PrivilegeCount = 1;
                    newState.Privileges.Luid = _luid;
                    newState.Privileges.Attributes = 0; // Disable

                    AdjustTokenPrivileges(_tokenHandle, false, &newState, 0, null, null);

                    _needToRevert = false;
                }

                if (_tokenHandle != IntPtr.Zero)
                {
                    CloseHandle(_tokenHandle);
                    _tokenHandle = IntPtr.Zero;
                }
            }
        }
    }
}
