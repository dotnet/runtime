// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public class IntCast
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Cast_Short_To_Long(short value)
        {
            // X64-NOT: cdqe
            return (long)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Cast_Short_To_Long_Add(short value1, short value2)
        {
            // X64:     movsx
            // X64-NOT: cdqe
            // X64:     movsx
            // X64-NOT: movsxd

            return (long)value1 + (long)value2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static nint Cast_LeadingZeroCount_To_NInt(ulong value)
        {
            // X64-NOT: cdqe
            // X64-NOT: movsxd
            return BitOperations.LeadingZeroCount(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static nint Cast_TrailingZeroCount_To_NInt(ulong value)
        {
            // X64-NOT: cdqe
            // X64-NOT: movsxd
            return BitOperations.TrailingZeroCount(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static nint Cast_PopCount_To_NInt(ulong value)
        {
            // X64-NOT: cdqe
            // X64-NOT: movsxd
            return BitOperations.PopCount(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Cast_Long_To_Int_To_Long(long value)
        {
            return unchecked((long)(int)value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Cast_Long_To_UInt_To_Long(long value)
        {
            return unchecked((long)(uint)value);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (Cast_Short_To_Long(Int16.MaxValue) != 32767)
                return 0;

            if (Cast_Short_To_Long_Add(Int16.MaxValue, Int16.MaxValue) != 65534)
                return 0;

            if (Cast_LeadingZeroCount_To_NInt(0) != 64)
                return 0;

            if (Cast_LeadingZeroCount_To_NInt(ulong.MaxValue) != 0)
                return 0;

            if (Cast_TrailingZeroCount_To_NInt(0) != 64)
                return 0;

            if (Cast_TrailingZeroCount_To_NInt(1) != 0)
                return 0;

            if (Cast_PopCount_To_NInt(0) != 0)
                return 0;

            if (Cast_PopCount_To_NInt(ulong.MaxValue) != 64)
                return 0;

            if (Cast_Long_To_Int_To_Long(0xFFFF_FFFFL) != -1)
                return 0;

            if (Cast_Long_To_UInt_To_Long(-1) != uint.MaxValue)
                return 0;

            return 100;
        }
    }
}
