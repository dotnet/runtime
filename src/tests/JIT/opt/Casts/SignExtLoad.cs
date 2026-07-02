// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression tests for https://github.com/dotnet/runtime/issues/69472:
// a small-typed load fed to a sign-extending cast should emit a single
// signed-load instruction, not a zero-extending load followed by a separate
// sign-extension. Contained-source lowering in ContainCheckCast used to
// refuse to fold when the load's small-type signedness didn't match the
// cast's extension direction.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public class SignExtLoad
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int UByteSpanIndexed(ReadOnlySpan<byte> s, int i)
        {
            // X64:     movsx
            // X64-NOT: movzx
            return (sbyte)s[i];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CharArrayIndexed(char[] a, int i)
        {
            // X64:     movsx
            // X64-NOT: movzx
            return (short)a[i];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ByteArrayIndexed(byte[] a, int i)
        {
            // X64:     movsx
            // X64-NOT: movzx
            return (sbyte)a[i];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int UShortArrayIndexed(ushort[] a, int i)
        {
            // X64:     movsx
            // X64-NOT: movzx
            return (short)a[i];
        }

        [Fact]
        public static int TestEntryPoint()
        {
            byte[] bytes = new byte[] { 1, 0xFF };
            char[] chars = new char[] { 'A', '\uFFFF' };
            ushort[] ushorts = new ushort[] { 1, 0xFFFF };

            if (UByteSpanIndexed(bytes, 0) != 1) return 0;
            if (UByteSpanIndexed(bytes, 1) != -1) return 0;
            if (ByteArrayIndexed(bytes, 0) != 1) return 0;
            if (ByteArrayIndexed(bytes, 1) != -1) return 0;
            if (CharArrayIndexed(chars, 0) != 'A') return 0;
            if (CharArrayIndexed(chars, 1) != -1) return 0;
            if (UShortArrayIndexed(ushorts, 0) != 1) return 0;
            if (UShortArrayIndexed(ushorts, 1) != -1) return 0;

            return 100;
        }
    }
}
