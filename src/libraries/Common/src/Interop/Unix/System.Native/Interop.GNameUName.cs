// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Buffers;
using System.Text;
using System;
using System.Collections.Generic;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Gets the group name associated to the specified group ID.
        /// </summary>
        /// <param name="gid">The group ID.</param>
        /// <returns>On success, return a string with the group name. On failure, returns a null string.</returns>
        internal static string GetGName(uint gid) => GetGNameOrUName(gid, isGName: true);

        /// <summary>
        /// Gets the user name associated to the specified user ID.
        /// </summary>
        /// <param name="uid">The user ID.</param>
        /// <returns>On success, return a string with the user name. On failure, returns a null string.</returns>
        internal static string GetUName(uint uid) => GetGNameOrUName(uid, isGName: false);

        private static string GetGNameOrUName(uint id, bool isGName)
        {
            int bufferSize = 256; // Upper limit allowed for login name in kernel

            Span<byte> buffer = stackalloc byte[bufferSize];

            int resultLength = GetGNameOrUnameInternal(id, isGName, buffer);
            if(resultLength > 0 && resultLength <= buffer.Length)
            {
                return Encoding.UTF8.GetString(buffer.Slice(0, resultLength));
            }

            while (true)
            {
                bufferSize *= 2;
                byte[] pooledBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    resultLength = GetGNameOrUnameInternal(id, isGName, pooledBuffer);
                    if(resultLength > 0 && resultLength <= pooledBuffer.Length)
                    {
                        return Encoding.UTF8.GetString(pooledBuffer.AsSpan(0, resultLength));
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(pooledBuffer);
                }

                if (bufferSize >= 1024)
                {
                    return string.Empty;
                }
            }
        }

        private static int GetGNameOrUnameInternal(uint id, bool isGName, Span<byte> buffer)
        {
            unsafe
            {
                fixed (byte* pBuffer = &MemoryMarshal.GetReference(buffer))
                {
                    int result = isGName ?
                        Interop.Sys.GetGName(id, pBuffer, buffer.Length) :
                        Interop.Sys.GetUName(id, pBuffer, buffer.Length);

                    if (result <= 0)
                    {
                        ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                        if (errorInfo.Error == Interop.Error.ERANGE) // Insufficient buffer space, try again
                        {
                            return -1;
                        }
                        throw Interop.GetExceptionForIoErrno(errorInfo);
                    }
                    return result;
                }
            }
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetGName", SetLastError = true)]
        private static unsafe partial int GetGName(uint uid, byte* buffer, long bufferSize);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetUName", SetLastError = true)]
        private static unsafe partial int GetUName(uint uid, byte* buffer, long bufferSize);
    }
}
