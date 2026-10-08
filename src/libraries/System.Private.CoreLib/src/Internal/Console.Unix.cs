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
            int byteCount = Encoding.UTF8.GetByteCount(s);
            Span<byte> bytes = (uint)byteCount < 1024 ? stackalloc byte[byteCount] : new byte[byteCount];
            int cbytes = Encoding.UTF8.GetBytes(s, bytes);

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
