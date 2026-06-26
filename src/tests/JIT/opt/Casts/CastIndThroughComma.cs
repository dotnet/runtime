// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public class CastIndThroughComma
    {
        private struct ByteStruct
        {
            public byte Value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int SbyteFromReadOnlySpan(ReadOnlySpan<byte> span, int index)
        {
            // X64-NOT: movzx
            // X64:     movsx
            // X64-NOT: movzx
            return (sbyte)span[index];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ByteFromReadOnlySpan(ReadOnlySpan<sbyte> span, int index)
        {
            // X64-NOT: movsx
            // X64:     movzx
            // X64-NOT: movsx
            return (byte)span[index];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ShortFromReadOnlySpan(ReadOnlySpan<ushort> span, int index)
        {
            // X64-NOT: movzx
            // X64:     movsx
            // X64-NOT: movzx
            return (short)span[index];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int UshortFromReadOnlySpan(ReadOnlySpan<short> span, int index)
        {
            // X64-NOT: movsx
            // X64:     movzx
            // X64-NOT: movsx
            return (ushort)span[index];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int SbyteFromStructField(ByteStruct value)
        {
            // X64-NOT: movzx
            // X64:     movsx
            // X64-NOT: movzx
            return (sbyte)value.Value;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            byte[] bytes = [0, 1, 127, 128, 255];
            sbyte[] sbytes = [0, 1, 127, -128, -1];
            ushort[] ushorts = [0, 1, 32767, 32768, 65535];
            short[] shorts = [0, 1, 32767, -32768, -1];

            bool ok = true;
            for (int i = 0; i < bytes.Length; i++)
            {
                ok &= SbyteFromReadOnlySpan(bytes, i) == unchecked((sbyte)bytes[i]);
                ok &= ByteFromReadOnlySpan(sbytes, i) == (byte)sbytes[i];
                ok &= ShortFromReadOnlySpan(ushorts, i) == unchecked((short)ushorts[i]);
                ok &= UshortFromReadOnlySpan(shorts, i) == (ushort)shorts[i];
            }

            ok &= SbyteFromStructField(new ByteStruct { Value = 128 }) == -128;

            return ok ? 100 : 1;
        }
    }
}
