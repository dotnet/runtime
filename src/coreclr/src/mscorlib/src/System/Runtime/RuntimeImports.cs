﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if BIT64
using nuint = System.UInt64;
#else
    using nuint = System.UInt32;
#endif

namespace System.Runtime
{
    public class RuntimeImports
    {
        // Non-inlinable wrapper around the QCall that avoids poluting the fast path
        // with P/Invoke prolog/epilog.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        internal unsafe static void RhZeroMemory(ref byte b, nuint byteLength)
        {
            fixed (byte* bytePointer = &b)
            {
                RhZeroMemory(bytePointer, byteLength);
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        extern private unsafe static void RhZeroMemory(byte* b, nuint byteLength);
    }
}
