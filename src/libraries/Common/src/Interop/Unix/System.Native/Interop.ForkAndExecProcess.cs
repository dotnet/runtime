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
                AllocNullTerminatedArray(argv, ref argvPtr);
                AllocNullTerminatedArray(envp, ref envpPtr);
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
                FreeArray(envpPtr, envp.Length);
                FreeArray(argvPtr, argv.Length);
            }
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ForkAndExecProcess", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static unsafe partial int ForkAndExecProcess(
            string filename, byte** argv, byte** envp, string? cwd,
            int redirectStdin, int redirectStdout, int redirectStderr,
            int setUser, uint userId, uint groupId, uint* groups, int groupsLength,
            out int lpChildPid, out int stdinFd, out int stdoutFd, out int stderrFd);

        private static unsafe void AllocNullTerminatedArray(string[] arr, ref byte** arrPtr)
        {
            nuint arrLength = (nuint)arr.Length + 1; // +1 is for null termination

            // Allocate the unmanaged array to hold each string pointer.
            // It needs to have an extra element to null terminate the array.
            // Zero the memory so that if any of the individual string allocations fails,
            // we can loop through the array to free any that succeeded.
            // The last element will remain null.
            arrPtr = (byte**)NativeMemory.AllocZeroed(arrLength, (nuint)sizeof(byte*));

            // Now copy each string to unmanaged memory referenced from the array.
            // We need the data to be an unmanaged, null-terminated array of UTF8-encoded bytes.
            for (int i = 0; i < arr.Length; i++)
            {
                string str = arr[i];

                int byteLength = Encoding.UTF8.GetByteCount(str);
                arrPtr[i] = (byte*)NativeMemory.Alloc((nuint)byteLength + 1); //+1 for null termination

                int bytesWritten = Encoding.UTF8.GetBytes(str, new Span<byte>(arrPtr[i], byteLength));
                Debug.Assert(bytesWritten == byteLength);

                arrPtr[i][bytesWritten] = (byte)'\0'; // null terminate
            }
        }

        private static unsafe void FreeArray(byte** arr, int length)
        {
            if (arr != null)
            {
                // Free each element of the array
                for (int i = 0; i < length; i++)
                {
                    NativeMemory.Free(arr[i]);
                }

                // And then the array itself
                NativeMemory.Free(arr);
            }
        }
    }
}
