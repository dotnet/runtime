// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Security;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System
{
    //
    // Simple limited console class for internal printf-style debugging in mscorlib
    // and low-level tests that just want to depend on mscorlib.
    //

    public static class Console
    {
        [SecurityCritical]
        static SafeFileHandle _outputHandle;

        [SecuritySafeCritical]
        static Console()
        {
            _outputHandle = new SafeFileHandle(Win32Native.GetStdHandle(Win32Native.STD_OUTPUT_HANDLE), false);
        }

        [SecuritySafeCritical]
        public static unsafe void Write(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);

            fixed (byte * pBytes = bytes)
            {
                int bytesWritten;
                Win32Native.WriteFile(_outputHandle, pBytes, bytes.Length, out bytesWritten, IntPtr.Zero);
            }
        }

        public static void WriteLine(string s)
        {
            Write(s + Environment.NewLine);
        }

        public static void WriteLine()
        {
            Write(Environment.NewLine);
        }
     }
}
