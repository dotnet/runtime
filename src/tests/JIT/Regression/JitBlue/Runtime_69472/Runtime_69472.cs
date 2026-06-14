// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_69472;

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Validates the fgOptimizeCast extension that looks through GT_COMMA wrappers
// to fold `CAST<small>(COMMA(side-effects, IND<small-same-size>))` into
// `COMMA(side-effects, IND<castToType>)`. The common shape is
// `(sbyte)span[i]` which previously emitted a zero-extending load followed by
// a sign-extending cast and should now collapse to a single `movsx` (or
// equivalently `movzx`) load.
//
// Correctness must hold across signed/unsigned source-and-target combinations
// and across all relevant integer values for the small type.

public class Runtime_69472
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SbyteFromByteArr(byte[] a, int i) => (sbyte)a[i];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ByteFromSbyteArr(sbyte[] a, int i) => (byte)a[i];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ShortFromUshortArr(ushort[] a, int i) => (short)a[i];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int UshortFromShortArr(short[] a, int i) => (ushort)a[i];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SbyteFromReadOnlySpan(ReadOnlySpan<byte> s, int i) => (sbyte)s[i];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int SbyteFromPointer(byte* p) => (sbyte)*p;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SbyteFromByteField(Boxed b) => (sbyte)b._value;

    private sealed class Boxed
    {
        public byte _value;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool ok = true;

        for (int v = 0; v < 256; v++)
        {
            byte b   = (byte)v;
            sbyte sb = unchecked((sbyte)v);

            byte[] ba   = new[] { b };
            sbyte[] sba = new[] { sb };

            int expSbyteFromByte = unchecked((sbyte)b);
            int expByteFromSbyte = (byte)sb;

            if (SbyteFromByteArr(ba, 0) != expSbyteFromByte)
            {
                ok = false;
            }
            if (ByteFromSbyteArr(sba, 0) != expByteFromSbyte)
            {
                ok = false;
            }
            if (SbyteFromReadOnlySpan(ba, 0) != expSbyteFromByte)
            {
                ok = false;
            }
            if (SbyteFromByteField(new Boxed { _value = b }) != expSbyteFromByte)
            {
                ok = false;
            }
            unsafe
            {
                byte bb = b;
                if (SbyteFromPointer(&bb) != expSbyteFromByte)
                {
                    ok = false;
                }
            }
        }

        for (int v = 0; v <= 0xFFFF; v++)
        {
            ushort uw = (ushort)v;
            short  sw = unchecked((short)v);
            ushort[] ua = new[] { uw };
            short[]  sa = new[] { sw };

            if (ShortFromUshortArr(ua, 0) != unchecked((short)uw))
            {
                ok = false;
            }
            if (UshortFromShortArr(sa, 0) != (ushort)sw)
            {
                ok = false;
            }
        }

        return ok ? 100 : 1;
    }
}
