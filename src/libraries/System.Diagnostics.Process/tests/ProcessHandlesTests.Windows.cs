// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessHandlesTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ProcessStartedWithInvalidHandles_ConsoleReportsInvalidHandles()
        {
            using Process process = CreateProcess(() =>
            {
                Assert.True(Console.OpenStandardInputHandle().IsInvalid);
                Assert.True(Console.OpenStandardOutputHandle().IsInvalid);
                Assert.True(Console.OpenStandardErrorHandle().IsInvalid);

                return RemoteExecutor.SuccessExitCode;
            });

            Assert.Equal(RemoteExecutor.SuccessExitCode, RunWithHandles(process.StartInfo, (nint)(-1), (nint)(-1), (nint)(-1)));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void ProcessStartedWithInvalidHandles_CanStartChildProcessWithDerivedInvalidHandles(bool restrictHandles, bool killOnParentExit)
        {
            using Process process = CreateProcess((inheritanceArg, killArg) =>
            {
                using (Process childProcess = CreateProcess(() =>
                {
                    Assert.True(Console.OpenStandardInputHandle().IsInvalid);
                    Assert.True(Console.OpenStandardOutputHandle().IsInvalid);
                    Assert.True(Console.OpenStandardErrorHandle().IsInvalid);

                    return RemoteExecutor.SuccessExitCode;
                }))
                {
                    childProcess.StartInfo.InheritedHandles = bool.Parse(inheritanceArg) ? [] : null;
                    childProcess.StartInfo.KillOnParentExit = bool.Parse(killArg);
                    childProcess.Start();

                    try
                    {
                        childProcess.WaitForExit(WaitInMS);
                        return childProcess.ExitCode;
                    }
                    finally
                    {
                        childProcess.Kill();
                    }
                }
            }, restrictHandles.ToString(), killOnParentExit.ToString());

            Assert.Equal(RemoteExecutor.SuccessExitCode, RunWithHandles(process.StartInfo, (nint)(-1), (nint)(-1), (nint)(-1)));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void ProcessStartedWithInvalidHandles_CanRedirectOutput(bool restrictHandles)
        {
            using Process process = CreateProcess(arg =>
            {
                using (Process childProcess = CreateProcess(() =>
                {
                    Assert.True(Console.OpenStandardInputHandle().IsInvalid);
                    Assert.False(Console.OpenStandardOutputHandle().IsInvalid);
                    Assert.True(Console.OpenStandardErrorHandle().IsInvalid);

                    Console.Write("hello");

                    return RemoteExecutor.SuccessExitCode;
                }))
                {
                    childProcess.StartInfo.RedirectStandardOutput = true;
                    childProcess.StartInfo.InheritedHandles = bool.Parse(arg) ? [] : null;
                    childProcess.Start();

                    try
                    {
                        Assert.Equal("hello", childProcess.StandardOutput.ReadToEnd());
                        childProcess.WaitForExit(WaitInMS);
                        return childProcess.ExitCode;
                    }
                    finally
                    {
                        childProcess.Kill();
                    }
                }
            }, restrictHandles.ToString());

            Assert.Equal(RemoteExecutor.SuccessExitCode, RunWithHandles(process.StartInfo, (nint)(-1), (nint)(-1), (nint)(-1)));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public unsafe void ProcessStartedWithAnonymousPipeHandles_CanCaptureOutput()
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle outputReadHandle, out SafeFileHandle outputWriteHandle);
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle errorReadHandle, out SafeFileHandle errorWriteHandle);

            using (outputReadHandle)
            using (outputWriteHandle)
            using (errorReadHandle)
            using (errorWriteHandle)
            {
                // Enable inheritance on the write ends so the child process can use them.
                if (!Interop.Kernel32.SetHandleInformation(
                    outputWriteHandle,
                    Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT,
                    Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!Interop.Kernel32.SetHandleInformation(
                    errorWriteHandle,
                    Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT,
                    Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                using Process remoteProcess = CreateProcess(() =>
                {
                    Console.Write("stdout_hello");
                    Console.Error.Write("stderr_hello");

                    return RemoteExecutor.SuccessExitCode;
                });

                ProcessStartInfo startInfo = remoteProcess.StartInfo;
                string arguments = $"\"{startInfo.FileName}\" {startInfo.Arguments}\0";

                Interop.Kernel32.STARTUPINFOEX startupInfoEx = default;
                Interop.Kernel32.PROCESS_INFORMATION processInfo = default;
                Interop.Kernel32.SECURITY_ATTRIBUTES unused_SecAttrs = default;

                startupInfoEx.StartupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFOEX);
                startupInfoEx.StartupInfo.hStdInput = (nint)(-1);
                startupInfoEx.StartupInfo.hStdOutput = outputWriteHandle.DangerousGetHandle();
                startupInfoEx.StartupInfo.hStdError = errorWriteHandle.DangerousGetHandle();
                startupInfoEx.StartupInfo.dwFlags = Interop.Advapi32.StartupInfoOptions.STARTF_USESTDHANDLES;

                fixed (char* commandLinePtr = arguments)
                {
                    bool retVal = Interop.Kernel32.CreateProcess(
                        null,
                        commandLinePtr,
                        ref unused_SecAttrs,
                        ref unused_SecAttrs,
                        bInheritHandles: true,
                        Interop.Kernel32.EXTENDED_STARTUPINFO_PRESENT,
                        null,
                        null,
                        &startupInfoEx,
                        &processInfo
                    );

                    if (!retVal)
                    {
                        throw new Win32Exception();
                    }
                }

                try
                {
                    SafeProcessHandle safeProcessHandle = new(processInfo.hProcess, ownsHandle: true);

                    using Process process = new(
                        safeProcessHandle,
                        standardOutput: outputReadHandle,
                        standardError: errorReadHandle);

                    // Close the write ends so reads don't block once the child exits.
                    outputWriteHandle.Close();
                    errorWriteHandle.Close();

                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();

                    process.WaitForExit(WaitInMS);

                    Assert.Equal("stdout_hello", stdout);
                    Assert.Equal("stderr_hello", stderr);
                    Assert.Equal(RemoteExecutor.SuccessExitCode, process.ExitCode);
                }
                finally
                {
                    Interop.Kernel32.CloseHandle(processInfo.hThread);
                }
            }
        }

        private unsafe int RunWithHandles(ProcessStartInfo startInfo, nint hStdInput, nint hStdOutput, nint hStdError)
        {
            // RemoteExector has provided us with the right path and arguments,
            // we just need to add the terminating null character.
            string arguments = $"\"{startInfo.FileName}\" {startInfo.Arguments}\0";

            Interop.Kernel32.STARTUPINFOEX startupInfoEx = default;
            Interop.Kernel32.PROCESS_INFORMATION processInfo = default;
            Interop.Kernel32.SECURITY_ATTRIBUTES unused_SecAttrs = default;

            startupInfoEx.StartupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFOEX);
            startupInfoEx.StartupInfo.hStdInput = hStdInput;
            startupInfoEx.StartupInfo.hStdOutput = hStdOutput;
            startupInfoEx.StartupInfo.hStdError = hStdError;

            // If STARTF_USESTDHANDLES is not set, the new process will inherit the standard handles.
            startupInfoEx.StartupInfo.dwFlags = Interop.Advapi32.StartupInfoOptions.STARTF_USESTDHANDLES;

            bool retVal = false;
            fixed (char* commandLinePtr = arguments)
            {
                retVal = Interop.Kernel32.CreateProcess(
                    null,
                    commandLinePtr,
                    ref unused_SecAttrs,
                    ref unused_SecAttrs,
                    bInheritHandles: true,
                    Interop.Kernel32.EXTENDED_STARTUPINFO_PRESENT,
                    null,
                    null,
                    &startupInfoEx,
                    &processInfo
                );

                if (!retVal)
                {
                    throw new Win32Exception();
                }
            }

            try
            {
                using SafeProcessHandle safeProcessHandle = new(processInfo.hProcess, ownsHandle: true);

                try
                {
                    ProcessExitStatus exitStatus = safeProcessHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(WaitInMS));
                    return exitStatus.ExitCode;
                }
                finally
                {
                    safeProcessHandle.Kill();
                }
            }
            finally
            {
                Interop.Kernel32.CloseHandle(processInfo.hThread);
            }
        }

        private static unsafe string GetSafeFileHandleId(SafeFileHandle handle)
        {
            const int MaxPath = 32_767;
            char[] buffer = new char[MaxPath];
            uint result;
            fixed (char* ptr = buffer)
            {
                result = Interop.Kernel32.GetFinalPathNameByHandle(handle, ptr, (uint)MaxPath, Interop.Kernel32.FILE_NAME_NORMALIZED);
            }

            if (result == 0)
            {
                throw new Win32Exception();
            }

            return new string(buffer, 0, (int)result);
        }
    }
}
