// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SpawnProcess", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static unsafe partial int SpawnProcess(
            string path,
            byte** argv,
            byte** envp,
            int stdinFd,
            int stdoutFd,
            int stderrFd,
            string? workingDir,
            out int pid,
            out int pidfd,
            int killOnParentDeath,
            int createSuspended,
            int createNewProcessGroup,
            int detached,
            int* inheritedHandles,
            int inheritedHandlesCount);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SendSignal", SetLastError = true)]
        internal static partial int SendSignal(int pidfd, int pid, PosixSignal managedSignal);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitForExitAndReap", SetLastError = true)]
        internal static partial int WaitForExitAndReap(SafeProcessHandle pidfd, int pid, out int exitCode, out int signal);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_TryWaitForExit", SetLastError = true)]
        internal static partial int TryWaitForExit(SafeProcessHandle pidfd, int pid, int timeoutMs, out int exitCode, out int signal);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_TryWaitForExitCancellable", SetLastError = true)]
        internal static partial int TryWaitForExitCancellable(SafeProcessHandle pidfd, int pid, int cancelPipeFd, out int exitCode, out int signal);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitForExitOrKillOnTimeout", SetLastError = true)]
        internal static partial int WaitForExitOrKillOnTimeout(SafeProcessHandle pidfd, int pid, int timeoutMs, out int exitCode, out int signal, out int hasTimedout);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_TryGetExitCode", SetLastError = true)]
        internal static partial int TryGetExitCode(SafeProcessHandle pidfd, int pid, out int exitCode, out int signal);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_OpenProcess", SetLastError = true)]
        internal static partial int OpenProcess(int pid, out int outPidfd);
    }
}
