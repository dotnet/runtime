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

                AllocArgvArray(argv, ref argvPtr);
                AllocEnvpArray(env, ref envpPtr);
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
                NativeMemory.Free(envpPtr);
                NativeMemory.Free(argvPtr);

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

        /// <summary>
        /// Allocates a single native memory block containing both a null-terminated pointer array
        /// and the UTF-8 encoded string data for the given array of strings.
        /// </summary>
        private static unsafe void AllocArgvArray(string[] arr, ref byte** arrPtr)
        {
            int count = arr.Length;

            // First pass: compute total byte length of all strings.
            int totalByteLength = 0;
            foreach (string str in arr)
            {
                totalByteLength = checked(totalByteLength + Encoding.UTF8.GetByteCount(str) + 1); // +1 for null terminator
            }

            // Allocate a single block: pointer array (count + 1 for null terminator) followed by string data.
            nuint pointersByteLength = checked((nuint)(count + 1) * (nuint)sizeof(byte*));
            byte* block = (byte*)NativeMemory.Alloc(checked(pointersByteLength + (nuint)totalByteLength));
            arrPtr = (byte**)block;

            // Set the null terminator for the pointer array.
            arrPtr[count] = null;

            Span<byte> data = new Span<byte>(block + pointersByteLength, totalByteLength);
            int dataOffset = 0;
            for (int i = 0; i < count; i++)
            {
                arrPtr[i] = block + pointersByteLength + (nuint)dataOffset;

                int bytesWritten = Encoding.UTF8.GetBytes(arr[i], data.Slice(dataOffset));
                data[dataOffset + bytesWritten] = (byte)'\0';
                dataOffset += bytesWritten + 1;
            }

            Debug.Assert(dataOffset == totalByteLength);
        }

        /// <summary>
        /// Allocates a single native memory block containing both a null-terminated pointer array
        /// and the UTF-8 encoded "key=value\0" data for all non-null entries in the environment dictionary.
        /// </summary>
        private static unsafe void AllocEnvpArray(IDictionary<string, string?> env, ref byte** arrPtr)
        {
            // First pass: count entries with non-null values and compute total buffer size.
            int count = 0;
            int totalByteLength = 0;
            foreach (KeyValuePair<string, string?> pair in env)
            {
                if (pair.Value is not null)
                {
                    // Each entry: UTF8(key) + '=' + UTF8(value) + '\0'
                    totalByteLength = checked(totalByteLength + Encoding.UTF8.GetByteCount(pair.Key) + 1 + Encoding.UTF8.GetByteCount(pair.Value) + 1);
                    count++;
                }
            }

            // Allocate a single block: pointer array (count + 1 for null terminator) followed by string data.
            nuint pointersByteLength = checked((nuint)(count + 1) * (nuint)sizeof(byte*));
            byte* block = (byte*)NativeMemory.Alloc(checked(pointersByteLength + (nuint)totalByteLength));
            arrPtr = (byte**)block;

            // Set the null terminator for the pointer array.
            arrPtr[count] = null;

            // Second pass: encode each key=value pair directly into the buffer.
            // Span bounds checking on the data portion will throw if the collection was concurrently modified.
            Span<byte> data = new Span<byte>(block + pointersByteLength, totalByteLength);
            int entryIndex = 0;
            int dataOffset = 0;
            foreach (KeyValuePair<string, string?> pair in env)
            {
                if (pair.Value is not null)
                {
                    arrPtr[entryIndex] = block + pointersByteLength + (nuint)dataOffset;

                    int keyBytes = Encoding.UTF8.GetBytes(pair.Key, data.Slice(dataOffset));
                    data[dataOffset + keyBytes] = (byte)'=';
                    int valueBytes = Encoding.UTF8.GetBytes(pair.Value, data.Slice(dataOffset + keyBytes + 1));
                    data[dataOffset + keyBytes + 1 + valueBytes] = (byte)'\0';

                    dataOffset += keyBytes + 1 + valueBytes + 1;
                    entryIndex++;
                }
            }

            Debug.Assert(entryIndex == count);
            Debug.Assert(dataOffset == totalByteLength);
        }
    }
}
