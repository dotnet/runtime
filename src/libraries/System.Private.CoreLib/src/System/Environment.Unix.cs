// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System
{
    public static partial class Environment
    {
        public static string[] GetLogicalDrives() => Interop.Sys.GetAllMountPoints();

        public static string MachineName
        {
            get
            {
                string hostName = Interop.Sys.GetHostName();
                int dotPos = hostName.IndexOf('.');
                return dotPos == -1 ? hostName : hostName.Substring(0, dotPos);
            }
        }

        public static unsafe string UserName
        {
            get
            {
                // First try with a buffer that should suffice for 99% of cases.
                string? username;
                const int BufLen = Interop.Sys.Passwd.InitialBufferSize;
                byte* stackBuf = stackalloc byte[BufLen];
                if (TryGetUserNameFromPasswd(stackBuf, BufLen, out username))
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
                        if (TryGetUserNameFromPasswd(buf, heapBuf.Length, out username))
                        {
                            return username ?? string.Empty;
                        }
                    }
                }

            }
        }

        private static unsafe bool TryGetUserNameFromPasswd(byte* buf, int bufLen, out string? username)
        {
            // Call getpwuid_r to get the passwd struct
            Interop.Sys.Passwd passwd;
            int error = Interop.Sys.GetPwUidR(Interop.Sys.GetEUid(), out passwd, buf, bufLen);

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

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Avoid inlining PInvoke frame into the hot path
        private static int GetProcessId() => Interop.Sys.GetPid();

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Avoid inlining PInvoke frame into the hot path
        private static string? GetProcessPath() => Interop.Sys.GetProcessPath();
    }
}
