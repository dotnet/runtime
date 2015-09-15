// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, SetLastError = true)]
        private static unsafe extern byte* GetCwd(byte* buffer, int bufferLength);

        internal static unsafe string GetCwd()
        {      
            const int StackLimit = 256;
       
            // First try to get the path into a buffer on the stack
            byte* stackBuf = stackalloc byte[StackLimit];
            string result = GetCwdHelper(stackBuf, StackLimit);
            if (result != null)
            {
                return result;
            }

            // If that was too small, try increasing large buffer sizes
            // until we get one that works or until we hit MaxPath.
            int maxPath = Interop.Sys.MaxPath;
            if (StackLimit < maxPath)
            {
                int bufferSize = StackLimit;
                do
                {
                    checked { bufferSize *= 2; }
                    var buf = new byte[Math.Min(bufferSize, maxPath)];
                    fixed (byte* ptr = buf)
                    {
                        result = GetCwdHelper(ptr, buf.Length);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
                while (bufferSize < maxPath);
            }

            // If we couldn't get the cwd with a MaxPath-sized buffer, something's wrong.
            throw Interop.GetExceptionForIoErrno(new ErrorInfo(Interop.Error.ENAMETOOLONG));
        }

        private static unsafe string GetCwdHelper(byte* ptr, int bufferSize)
        {
            // Call the real getcwd
            byte* result = GetCwd(ptr, bufferSize);

            // If it returned non-null, the null-terminated path is in the buffer
            if (result != null)
            {
                return Marshal.PtrToStringAnsi((IntPtr)ptr);
            }

            // Otherwise, if it failed due to the buffer being too small, return null;
            // for anything else, throw.
            ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
            if (errorInfo.Error == Interop.Error.ERANGE)
            {
               return null;
            }
            throw Interop.GetExceptionForIoErrno(errorInfo);
        }
    }
}
