// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static unsafe int ForkAndExecProcess(
            string filename, string[] argv, string[] envp, string? cwd,
            bool redirectStdin, bool redirectStdout, bool redirectStderr,
            bool setUser, uint userId, uint groupId, uint[]? groups,
            out int lpChildPid, out int stdinFd, out int stdoutFd, out int stderrFd, bool shouldThrow = true)
        {
            byte** argvPtr = null, envpPtr = null;
            int result = -1;
            try
            {
                System.Diagnostics.ProcessUtils.AllocNullTerminatedArray(argv, ref argvPtr);
                System.Diagnostics.ProcessUtils.AllocNullTerminatedArray(envp, ref envpPtr);
                fixed (uint* pGroups = groups)
                {
                    result = ForkAndExecProcess(
                        filename, argvPtr, envpPtr, cwd,
                        redirectStdin ? 1 : 0, redirectStdout ? 1 : 0, redirectStderr ? 1 : 0,
                        setUser ? 1 : 0, userId, groupId, pGroups, groups?.Length ?? 0,
                        out lpChildPid, out stdinFd, out stdoutFd, out stderrFd);
                }
                return result == 0 ? 0 : Marshal.GetLastPInvokeError();
            }
            finally
            {
                System.Diagnostics.ProcessUtils.FreeArray(envpPtr, envp.Length);
                System.Diagnostics.ProcessUtils.FreeArray(argvPtr, argv.Length);
            }
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ForkAndExecProcess", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static unsafe partial int ForkAndExecProcess(
            string filename, byte** argv, byte** envp, string? cwd,
            int redirectStdin, int redirectStdout, int redirectStderr,
            int setUser, uint userId, uint groupId, uint* groups, int groupsLength,
            out int lpChildPid, out int stdinFd, out int stdoutFd, out int stderrFd);
    }
}
