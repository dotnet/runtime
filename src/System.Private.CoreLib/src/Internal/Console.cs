// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Internal
{
    //
    // Simple limited console class for internal printf-style debugging in System.Private.CoreLib
    // and low-level tests that want to call System.Private.CoreLib directly
    //

    public static class Console
    {
        private static readonly SafeFileHandle _outputHandle =
            new SafeFileHandle(Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_OUTPUT_HANDLE), ownsHandle: false);

        public static unsafe void Write(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            fixed (byte* pBytes = bytes)
            {
                Interop.Kernel32.WriteFile(_outputHandle, pBytes, bytes.Length, out _, IntPtr.Zero);
            }
        }

        public static void WriteLine(string? s) =>
            Write(s + Environment.NewLine);

        public static void WriteLine() =>
            Write(Environment.NewLine);
    }
}
