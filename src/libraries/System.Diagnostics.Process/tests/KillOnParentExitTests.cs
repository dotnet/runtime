// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.IO;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class KillOnParentExitTests : ProcessTestBase
    {
        [Fact]
        public void KillOnParentExit_DefaultsToFalse()
        {
            ProcessStartInfo startInfo = new();
            Assert.False(startInfo.KillOnParentExit);
        }

        [Fact]
        public void KillOnParentExit_CanBeSetToTrue()
        {
            ProcessStartInfo startInfo = new();
            startInfo.KillOnParentExit = true;
            Assert.True(startInfo.KillOnParentExit);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void KillOnParentExit_ProcessStartsAndExitsNormally()
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "echo test" }, KillOnParentExit = true }
                : new("sh") { ArgumentList = { "-c", "echo test" }, KillOnParentExit = true };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo);
            using Process fetchedProcess = Process.GetProcessById(processHandle.ProcessId);
            Assert.True(fetchedProcess.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(true)]
        [InlineData(false)]
        public void KillOnParentExit_KillsTheChild_WhenParentExits(bool enabled)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                (enabledStr) =>
                {
                    ProcessStartInfo processStartInfo = CreateLongRunningStartInfo();
                    processStartInfo.KillOnParentExit = bool.Parse(enabledStr);

                    using SafeProcessHandle started = SafeProcessHandle.Start(processStartInfo);
                    Console.WriteLine(started.ProcessId);
                },
                arg: enabled.ToString(),
                remoteInvokeOptions);

            string firstLine = remoteHandle.Process.StandardOutput.ReadLine();
            int grandChildPid = int.Parse(firstLine);
            remoteHandle.Process.WaitForExit();

            VerifyProcessIsRunning(enabled, grandChildPid);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(true)]
        [InlineData(false)]
        public void KillOnParentExit_KillsTheChild_WhenParentIsKilled(bool enabled)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;
            remoteInvokeOptions.StartInfo.RedirectStandardInput = true;

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                (enabledStr) =>
                {
                    ProcessStartInfo processStartInfo = CreateLongRunningStartInfo();
                    processStartInfo.KillOnParentExit = bool.Parse(enabledStr);

                    using SafeProcessHandle started = SafeProcessHandle.Start(processStartInfo);
                    Console.WriteLine(started.ProcessId);

                    // This will block the child until parent kills it.
                    _ = Console.ReadLine();
                },
                arg: enabled.ToString(),
                remoteInvokeOptions);

            string firstLine = remoteHandle.Process.StandardOutput.ReadLine();
            int grandChildPid = int.Parse(firstLine);
            remoteHandle.Process.Kill();
            remoteHandle.Process.WaitForExit();

            VerifyProcessIsRunning(enabled, grandChildPid);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(true)]
        [InlineData(false)]
        public void KillOnParentExit_KillsTheChild_WhenParentCrashes(bool enabled)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;
            remoteInvokeOptions.StartInfo.RedirectStandardError = true;
            remoteInvokeOptions.StartInfo.RedirectStandardInput = true;

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                (enabledStr) =>
                {
                    ProcessStartInfo processStartInfo = CreateLongRunningStartInfo();
                    processStartInfo.KillOnParentExit = bool.Parse(enabledStr);

                    using SafeProcessHandle started = SafeProcessHandle.Start(processStartInfo);
                    Console.WriteLine(started.ProcessId);

                    // This will block the child until parent writes input.
                    _ = Console.ReadLine();

                    // Guaranteed Access Violation - write to null pointer
                    Marshal.WriteInt32(IntPtr.Zero, 42);
                },
                arg: enabled.ToString(),
                remoteInvokeOptions);

            string firstLine = remoteHandle.Process.StandardOutput.ReadLine();
            int grandChildPid = int.Parse(firstLine);
            remoteHandle.Process.StandardInput.WriteLine("One AccessViolationException please.");
            remoteHandle.Process.WaitForExit();

            VerifyProcessIsRunning(enabled, grandChildPid);
        }

        private static bool IsAdmin_IsNotNano_RemoteExecutorIsSupported
            => PlatformDetection.IsWindows && PlatformDetection.IsNotWindowsNanoServer
            && PlatformDetection.IsPrivilegedProcess && RemoteExecutor.IsSupported;

        [ConditionalFact(typeof(KillOnParentExitTests), nameof(IsAdmin_IsNotNano_RemoteExecutorIsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [OuterLoop("Requires admin privileges")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/80019", TestRuntimes.Mono)]
        public void KillOnParentExit_WithUserCredentials()
        {
            using Process longRunning = CreateProcessLong();
            longRunning.StartInfo.KillOnParentExit = true;
            longRunning.StartInfo.LoadUserProfile = true;

            using TestProcessState testAccountCleanup = CreateUserAndExecute(longRunning, Setup, Cleanup);

            string username = testAccountCleanup.ProcessAccountName.Split('\\').Last();
            Assert.Equal(username, Helpers.GetProcessUserName(longRunning));

            void Setup(string username, string workingDirectory)
            {
                if (PlatformDetection.IsNotWindowsServerCore)
                {
                    SetAccessControl(username, longRunning.StartInfo.FileName, workingDirectory, add: true);
                }
            }

            void Cleanup(string username, string workingDirectory)
            {
                if (PlatformDetection.IsNotWindowsServerCore)
                {
                    SetAccessControl(username, longRunning.StartInfo.FileName, workingDirectory, add: false);
                }
            }
        }

        private static ProcessStartInfo CreateLongRunningStartInfo()
        {
            // Create a process that sleeps for approximately 10 seconds
            return OperatingSystem.IsWindows()
                ? new("ping") { ArgumentList = { "127.0.0.1", "-n", "11" } }
                : new("sleep") { ArgumentList = { "10" } };
        }

        private static void VerifyProcessIsRunning(bool shouldBeKilled, int processId)
        {
            // Give the OS a moment to clean up
            Thread.Sleep(500);

            if (shouldBeKilled)
            {
                // When KillOnParentExit is enabled, the process should have been terminated.
                Assert.Throws<ArgumentException>(() => Process.GetProcessById(processId));
            }
            else
            {
                // When KillOnParentExit is disabled, the process should still be running.
                using Process process = Process.GetProcessById(processId);
                try
                {
                    Assert.False(process.HasExited);
                }
                finally
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }

        private static void SetAccessControl(string userName, string filePath, string directoryPath, bool add)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            FileSecurity fileSecurity = fileInfo.GetAccessControl();
            Apply(userName, fileSecurity, FileSystemRights.ReadAndExecute, add);
            fileInfo.SetAccessControl(fileSecurity);

            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
            DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();
            Apply(userName, directorySecurity, FileSystemRights.Read, add);
            directoryInfo.SetAccessControl(directorySecurity);

            static void Apply(string userName, FileSystemSecurity accessControl, FileSystemRights rights, bool add)
            {
                FileSystemAccessRule fileSystemAccessRule = new FileSystemAccessRule(userName, rights, AccessControlType.Allow);

                if (add)
                {
                    accessControl.AddAccessRule(fileSystemAccessRule);
                }
                else
                {
                    accessControl.RemoveAccessRule(fileSystemAccessRule);
                }
            }
        }

        private const int ERROR_SHARING_VIOLATION = 32;

        private static TestProcessState CreateUserAndExecute(
            Process process,
            Action<string, string> additionalSetup = null,
            Action<string, string> additionalCleanup = null,
            [CallerMemberName] string memberName = "")
        {
            string callerIntials = new string(memberName.Where(c => char.IsUpper(c)).Take(18).ToArray());

            WindowsTestAccount processAccount = new WindowsTestAccount(string.Concat("d", callerIntials));
            string workingDirectory = string.IsNullOrEmpty(process.StartInfo.WorkingDirectory)
                    ? Directory.GetCurrentDirectory()
                    : process.StartInfo.WorkingDirectory;

            additionalSetup?.Invoke(processAccount.AccountName, workingDirectory);

            process.StartInfo.UserName = processAccount.AccountName.Split('\\').Last();
            process.StartInfo.Domain = processAccount.AccountName.Split('\\').First();
            process.StartInfo.PasswordInClearText = processAccount.Password;

            try
            {
                bool hasStarted = process.Start();
                return new TestProcessState(process, hasStarted, processAccount, workingDirectory, additionalCleanup);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_SHARING_VIOLATION)
            {
                throw new SkipTestException($"{process.StartInfo.FileName} has been locked by some other process");
            }
        }

        private class TestProcessState : IDisposable
        {
            private readonly Process _process;
            private readonly bool _hasStarted;
            private readonly WindowsTestAccount _processAccount;
            private readonly string _workingDirectory;
            private readonly Action<string, string> _additionalCleanup;

            public TestProcessState(
                Process process,
                bool hasStarted,
                WindowsTestAccount processAccount,
                string workingDirectory,
                Action<string, string> additionalCleanup)
            {
                _process = process;
                _hasStarted = hasStarted;
                _processAccount = processAccount;
                _workingDirectory = workingDirectory;
                _additionalCleanup = additionalCleanup;
            }

            public string ProcessAccountName => _processAccount?.AccountName;

            public void Dispose()
            {
                if (_hasStarted)
                {
                    _process.Kill();
                    Assert.True(_process.WaitForExit(WaitInMS));
                }

                _additionalCleanup?.Invoke(_processAccount?.AccountName, _workingDirectory);
                _processAccount?.Dispose();
            }
        }
    }
}
