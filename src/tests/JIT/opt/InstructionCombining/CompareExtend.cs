// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestCompareExtend
{
    public class Program
    {
        static int result = 100;
        static int failCtr = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckCompareExtend()
        {
            // Signed / Unsigned
            AssertTrue(SByteByte(12, 12));
            AssertFalse(SByteByte(-12, 34));

            AssertTrue(ShortByte(12, 12));
            AssertFalse(ShortByte(-12, 12));

            AssertTrue(ShortUShort(12, 12));
            AssertFalse(ShortUShort(-12, 12));

            AssertTrue(IntByte(12, 12));
            AssertFalse(IntByte(-12, 34));

            AssertTrue(IntUShort(12, 12));
            AssertFalse(IntUShort(-12, 34));

            AssertTrue(IntUInt(12, 12));
            AssertFalse(IntUInt(-12, 34));

            AssertTrue(LongByte(12, 12));
            AssertFalse(LongByte(-12, 34));

            AssertTrue(LongUShort(12, 12));
            AssertFalse(LongUShort(-12, 34));

            AssertTrue(LongUInt(12, 12));
            AssertFalse(LongUInt(-12, 34));

            // Signed / Signed
            AssertTrue(SByteSByte(12, 12));
            AssertFalse(SByteSByte(12, 34));

            AssertTrue(ShortSByte(12, 12));
            AssertFalse(ShortSByte(-1234, -12));

            AssertTrue(ShortShort(1234, 1234));
            AssertFalse(ShortShort(1234, 3456));

            AssertTrue(IntSByte(12, 12));
            AssertFalse(IntSByte(-12, -34));

            AssertTrue(IntShort(1234, 1234));
            AssertFalse(IntShort(1234, -1234));

            AssertTrue(LongSByte(12, 12));
            AssertFalse(LongSByte(12, -34));

            AssertTrue(LongShort(12, 12));
            AssertFalse(LongShort(-12, 34));

            AssertTrue(LongInt(12, 12));
            AssertFalse(LongInt(12, -34));

            // Unsigned / Signed
            AssertTrue(ByteSByte(12, 12));
            AssertFalse(ByteSByte(12, -12));

            AssertTrue(UShortSByte(12, 12));
            AssertFalse(UShortSByte(12, -12));

            AssertTrue(UShortShort(1234, 1234));
            AssertFalse(UShortShort(1234, -1234));

            AssertTrue(UIntSByte(12, 12));
            AssertFalse(UIntSByte(12, -12));

            AssertTrue(UIntShort(1234, 1234));
            AssertFalse(UIntShort(1234, -1234));

            AssertTrue(UIntInt(1234, 1234));
            AssertFalse(UIntInt(1234, -1234));

            AssertTrue(ULongSByte(12, 12));
            AssertFalse(ULongSByte(12, -12));

            AssertTrue(ULongShort(1234, 1234));
            AssertFalse(ULongShort(1234, -1234));

            AssertTrue(ULongInt(1234, 1234));
            AssertFalse(ULongInt(1234, -1234));

            // Unsigned / Unsigned
            AssertTrue(ByteByte(12, 12));
            AssertFalse(ByteByte(12, 34));

            AssertTrue(UShortByte(12, 12));
            AssertFalse(UShortByte(12, 34));

            AssertTrue(UShortUShort(1234, 1234));
            AssertFalse(UShortUShort(1234, 3456));

            AssertTrue(UIntByte(12, 12));
            AssertFalse(UIntByte(12, 34));

            AssertTrue(UIntUShort(1234, 1234));
            AssertFalse(UIntUShort(1234, 3456));

            AssertTrue(ULongByte(12, 12));
            AssertFalse(ULongByte(12, 34));

            AssertTrue(ULongShort(1234, 1234));
            AssertFalse(ULongShort(1234, -3456));

            AssertTrue(ULongUShort(1234, 1234));
            AssertFalse(ULongUShort(1234, 3456));

            AssertTrue(ULongUInt(1234, 1234));
            AssertFalse(ULongUInt(1234, 3456));

            return result + failCtr;
        }

        static void AssertTrue(bool b)
        {
            if (!b)
            {
                failCtr += 1;
            }
        }

        static void AssertFalse(bool b)
        {
            AssertTrue(!b);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ByteByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTB
            return (byte)a == (byte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ByteSByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, SXTB
            return (byte)a == (sbyte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool SByteSByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, SXTB
            return (sbyte)a == (sbyte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool SByteByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTB
            return (sbyte)a == (byte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ShortByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTB
            return (short)a == (byte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ShortSByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, SXTB
            return (short)a == (sbyte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ShortShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, SXTH
            return (short)a == (short)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ShortUShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTH
            return (short)a == (ushort)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool UShortByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTB
            return (ushort)a == (byte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool UShortSByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, SXTB
            return (ushort)a == (sbyte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool UShortShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, SXTH
            return (ushort)a == (short)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool UShortUShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTH
            return (ushort)a == (ushort)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IntByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTB
            return (int)a == (byte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IntSByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, SXTB
            return (int)a == (sbyte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IntShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, SXTH
            return (int)a == (short)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IntUShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTH
            return (int)a == (ushort)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IntUInt(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, UXTW
            return (int)a == (uint)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool UIntByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTB
            return (uint)a == (byte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool UIntUShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, UXTH
            return (uint)a == (ushort)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool UIntSByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, SXTW
            return (uint)a == (sbyte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool UIntShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, SXTW
            return (uint)a == (short)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool UIntInt(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, SXTW
            return (uint)a == (int)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool LongByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, UXTW
            return a == (byte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool LongSByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, SXTW
            return a == (sbyte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool LongShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, SXTW
            return a == (short)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool LongUShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, UXTW
            return a == (ushort)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool LongInt(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, SXTW
            return a == (int)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool LongUInt(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, UXTW
            return a == (uint)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ULongByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, UXTW
            return (ulong)a == (byte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ULongSByte(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, SXTW
            return (ulong)a == (ulong)(sbyte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ULongShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, SXTW
            return (ulong)a == (ulong)(short)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ULongUShort(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, UXTW
            return (ulong)a == (ushort)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ULongInt(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, SXTW
            return (ulong)a == (ulong)(int)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ULongUInt(long a, long b)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{w[0-9]+}}, UXTW
            return (ulong)a == (uint)b;
        }
    }
}