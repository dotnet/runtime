// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static unsafe int ForkAndExecProcess(
            string filename, string[] argv, IDictionary<string, string?> env, string? cwd,
            bool setUser, uint userId, uint groupId, uint[]? groups,
            out int lpChildPid, SafeFileHandle? stdinFd, SafeFileHandle? stdoutFd, SafeFileHandle? stderrFd, bool shouldThrow = true)
        {
            byte** argvPtr = null, envpPtr = null;
            byte* envpBlock = null;
            int result = -1;

            bool stdinRefAdded = false, stdoutRefAdded = false, stderrRefAdded = false;
            try
            {
                int stdinRawFd = -1, stdoutRawFd = -1, stderrRawFd = -1;

                if (stdinFd is not null)
                {
                    stdinFd.DangerousAddRef(ref stdinRefAdded);
                    stdinRawFd = stdinFd.DangerousGetHandle().ToInt32();
                }

                if (stdoutFd is not null)
                {
                    stdoutFd.DangerousAddRef(ref stdoutRefAdded);
                    stdoutRawFd = stdoutFd.DangerousGetHandle().ToInt32();
                }

                if (stderrFd is not null)
                {
                    stderrFd.DangerousAddRef(ref stderrRefAdded);
                    stderrRawFd = stderrFd.DangerousGetHandle().ToInt32();
                }

                AllocNullTerminatedArray(argv, ref argvPtr);
                AllocNullTerminatedEnvpArray(env, ref envpPtr, ref envpBlock);
                fixed (uint* pGroups = groups)
                {
                    result = ForkAndExecProcess(
                        filename, argvPtr, envpPtr, cwd,
                        setUser ? 1 : 0, userId, groupId, pGroups, groups?.Length ?? 0,
                        out lpChildPid, stdinRawFd, stdoutRawFd, stderrRawFd);
                }
                return result == 0 ? 0 : Marshal.GetLastPInvokeError();
            }
            finally
            {
                NativeMemory.Free(envpBlock);
                NativeMemory.Free(envpPtr);
                FreeArray(argvPtr, argv.Length);

                if (stdinRefAdded)
                    stdinFd!.DangerousRelease();
                if (stdoutRefAdded)
                    stdoutFd!.DangerousRelease();
                if (stderrRefAdded)
                    stderrFd!.DangerousRelease();
            }
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ForkAndExecProcess", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static unsafe partial int ForkAndExecProcess(
            string filename, byte** argv, byte** envp, string? cwd,
            int setUser, uint userId, uint groupId, uint* groups, int groupsLength,
            out int lpChildPid, int stdinFd, int stdoutFd, int stderrFd);

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

        private static unsafe void AllocNullTerminatedEnvpArray(IDictionary<string, string?> env, ref byte** arrPtr, ref byte* dataBlock)
        {
            // First pass: count entries with non-null values and compute total buffer size.
            int count = 0;
            int totalByteLength = 0;
            foreach (KeyValuePair<string, string?> pair in env)
            {
                if (pair.Value is not null)
                {
                    // Each entry will be: UTF8(key) + '=' + UTF8(value) + '\0'
                    totalByteLength += Encoding.UTF8.GetByteCount(pair.Key) + 1 + Encoding.UTF8.GetByteCount(pair.Value) + 1;
                    count++;
                }
            }

            // Allocate the pointer array with null termination.
            arrPtr = (byte**)NativeMemory.AllocZeroed((nuint)count + 1, (nuint)sizeof(byte*));

            // Allocate a single contiguous block for all key=value\0 strings.
            dataBlock = totalByteLength > 0 ? (byte*)NativeMemory.Alloc((nuint)totalByteLength) : null;

            // Second pass: encode each key=value pair directly into the buffer.
            int entryIndex = 0;
            byte* current = dataBlock;
            int remainingBytes = totalByteLength;
            foreach (KeyValuePair<string, string?> pair in env)
            {
                if (pair.Value is not null)
                {
                    if ((uint)entryIndex >= (uint)count)
                    {
                        // More entries than expected; the collection was concurrently modified.
                        throw new InvalidOperationException();
                    }

                    arrPtr[entryIndex] = current;

                    Span<byte> buffer = new Span<byte>(current, remainingBytes);
                    int keyBytes = Encoding.UTF8.GetBytes(pair.Key, buffer);
                    buffer[keyBytes] = (byte)'=';
                    int valueBytes = Encoding.UTF8.GetBytes(pair.Value, buffer.Slice(keyBytes + 1));
                    current[keyBytes + 1 + valueBytes] = (byte)'\0';

                    int entryLength = keyBytes + 1 + valueBytes + 1;
                    current += entryLength;
                    remainingBytes -= entryLength;
                    entryIndex++;
                }
            }

            // If the counts or sizes don't match, the collection was concurrently modified.
            if (entryIndex != count || remainingBytes != 0)
            {
                throw new InvalidOperationException();
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
