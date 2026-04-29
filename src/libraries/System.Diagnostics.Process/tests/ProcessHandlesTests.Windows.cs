// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
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

            Assert.Equal(RemoteExecutor.SuccessExitCode, RunWithInvalidHandles(process.StartInfo));
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

            Assert.Equal(RemoteExecutor.SuccessExitCode, RunWithInvalidHandles(process.StartInfo));
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

            Assert.Equal(RemoteExecutor.SuccessExitCode, RunWithInvalidHandles(process.StartInfo));
        }

        private unsafe int RunWithInvalidHandles(ProcessStartInfo startInfo)
        {
            const nint INVALID_HANDLE_VALUE = -1;

            // RemoteExector has provided us with the right path and arguments,
            // we just need to add the terminating null character.
            string arguments = $"\"{startInfo.FileName}\" {startInfo.Arguments}\0";

            Interop.Kernel32.STARTUPINFOEX startupInfoEx = default;
            Interop.Kernel32.PROCESS_INFORMATION processInfo = default;
            Interop.Kernel32.SECURITY_ATTRIBUTES unused_SecAttrs = default;

            startupInfoEx.StartupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFOEX);
            startupInfoEx.StartupInfo.hStdInput = INVALID_HANDLE_VALUE;
            startupInfoEx.StartupInfo.hStdOutput = INVALID_HANDLE_VALUE;
            startupInfoEx.StartupInfo.hStdError = INVALID_HANDLE_VALUE;

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
                    bInheritHandles: false,
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
