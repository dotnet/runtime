// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal unsafe struct Passwd
        {
            internal const int InitialBufferSize = 256;

            internal byte* Name;
            internal byte* Password;
            internal uint  UserId;
            internal uint  GroupId;
            internal byte* UserInfo;
            internal byte* HomeDirectory;
            internal byte* Shell;
        }

        /// <summary>
        /// Gets the user name associated to the specified UID.
        /// </summary>
        /// <param name="uid">The user ID.</param>
        /// <returns>On success, return a string with the user name associated to the specified UID. On failure, returns an empty string.</returns>
        internal static unsafe string GetUserNameFromPasswd(uint uid)
        {
            // First try with a buffer that should suffice for 99% of cases.
            string? username;
            const int BufLen = Interop.Sys.Passwd.InitialBufferSize;
            byte* stackBuf = stackalloc byte[BufLen];
            if (TryGetUserNameFromPasswd(uid, stackBuf, BufLen, out username))
            {
                return username ?? string.Empty;
            }

            // Fallback to heap allocations if necessary, growing the buffer until
            // we succeed.  TryGetUserNameFromPasswd will throw if there's an unexpected error.
            int lastBufLen = BufLen;
            while (true)
            {
                lastBufLen *= 2;
                byte[] heapBuf = new byte[lastBufLen];
                fixed (byte* buf = &heapBuf[0])
                {
                    if (TryGetUserNameFromPasswd(uid, buf, heapBuf.Length, out username))
                    {
                        return username ?? string.Empty;
                    }
                }
            }
        }

        private static unsafe bool TryGetUserNameFromPasswd(uint uid, byte* buf, int bufLen, out string? username)
        {
            // Call getpwuid_r to get the passwd struct
            Interop.Sys.Passwd passwd;
            int error = Interop.Sys.GetPwUidR(uid, out passwd, buf, bufLen);

            // If the call succeeds, give back the user name retrieved
            if (error == 0)
            {
                Debug.Assert(passwd.Name != null);
                username = Marshal.PtrToStringAnsi((IntPtr)passwd.Name);
                return true;
            }

            // If the current user's entry could not be found, give back null,
            // but still return true (false indicates the buffer was too small).
            if (error == -1)
            {
                username = null;
                return true;
            }

            var errorInfo = new Interop.ErrorInfo(error);

            // If the call failed because the buffer was too small, return false to
            // indicate the caller should try again with a larger buffer.
            if (errorInfo.Error == Interop.Error.ERANGE)
            {
                username = null;
                return false;
            }

            // Otherwise, fail.
            throw new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPwUidR", SetLastError = false)]
        internal static unsafe partial int GetPwUidR(uint uid, out Passwd pwd, byte* buf, int bufLen);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPwNamR", StringMarshalling = StringMarshalling.Utf8, SetLastError = false)]
        internal static unsafe partial int GetPwNamR(string name, out Passwd pwd, byte* buf, int bufLen);
    }
}
