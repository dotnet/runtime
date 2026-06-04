// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Internal
{
    public static partial class Console
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string s)
        {
            WriteCore(s, error: false);
        }

        public static partial class Error
        {
            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public static void Write(string s)
            {
                WriteCore(s, error: true);
            }
        }

        private static unsafe void WriteCore(string s, bool error)
        {
            int bufferSize = checked(s.Length * 3); // max UTF-8 bytes per char
            Span<byte> bytes = (uint)bufferSize < 1024 ? stackalloc byte[bufferSize] : new byte[bufferSize];
            int cbytes;

            fixed (char* pChars = s)
            fixed (byte* pBytes = bytes)
            {
                cbytes = Encoding.UTF8.GetBytes(pChars, s.Length, pBytes, bytes.Length);
            }

            fixed (byte* pBytes = bytes)
            {
                if (error)
                    Interop.Sys.LogError(pBytes, cbytes);
                else
                    Interop.Sys.Log(pBytes, cbytes);
            }
        }
    }
}
