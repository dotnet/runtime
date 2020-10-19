// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Internal
{
    public static partial class Console
    {
        private static readonly IntPtr s_outputHandle =
            Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_OUTPUT_HANDLE);

        public static unsafe void Write(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            fixed (byte* pBytes = bytes)
            {
                Interop.Kernel32.WriteFile(s_outputHandle, pBytes, bytes.Length, out _, IntPtr.Zero);
            }
        }
    }
}
