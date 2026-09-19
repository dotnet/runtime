// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace CodeGenTests
{
    public class UnsafeTests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte UnsafeAsNarrowCast_Short(short value)
        {
            // X64-NOT: dword ptr
            return Unsafe.As<short, byte>(ref value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte UnsafeAsNarrowCast_Int(int value)
        {
            // X64-NOT: dword ptr
            return Unsafe.As<int, byte>(ref value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte UnsafeAsNarrowCast_Long(long value)
        {
            // X64-NOT: qword ptr
            return Unsafe.As<long, byte>(ref value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static float UnsafeAsSecondFloat_Double(double value)
        {
            // X64-FULL-LINE: {{v?movshdup}} {{xmm[0-9]+}}, {{xmm[0-9]+}}
            // ARM64-FULL-LINE: dup {{s[0-9]+}}, {{v[0-9]+}}.s[1]
            return Unsafe.Add(ref Unsafe.As<double, float>(ref value), 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static float UnsafeAsMisalignedFloat_Double(double value)
        {
            return Unsafe.As<byte, float>(ref Unsafe.AddByteOffset(ref Unsafe.As<double, byte>(ref value), 2));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int UnsafeAsInt_Vector128(Vector128<float> value)
        {
            // ARM64-FULL-LINE: smov {{x[0-9]+}}, {{v[0-9]+}}.s[2]
            return Unsafe.Add(ref Unsafe.As<Vector128<float>, int>(ref value), 2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static double UnsafeAsSecondDouble_Vector128(Vector128<double> value)
        {
            // ARM64-FULL-LINE: dup {{d[0-9]+}}, {{v[0-9]+}}.d[1]
            return Unsafe.Add(ref Unsafe.As<Vector128<double>, double>(ref value), 1);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (UnsafeAsNarrowCast_Short(255) != 255)
                return 0;

            if (UnsafeAsNarrowCast_Int(255) != 255)
                return 0;

            if (UnsafeAsNarrowCast_Long(255) != 255)
                return 0;

            double doubleValue = BitConverter.Int64BitsToDouble(
                ((long)BitConverter.SingleToInt32Bits(2.5f) << 32) | (uint)BitConverter.SingleToInt32Bits(1.25f));
            if (UnsafeAsSecondFloat_Double(doubleValue) != 2.5f)
                return 0;

            float expectedMisaligned = BitConverter.Int32BitsToSingle((int)(BitConverter.DoubleToInt64Bits(doubleValue) >> 16));
            if (UnsafeAsMisalignedFloat_Double(doubleValue) != expectedMisaligned)
                return 0;

            Vector128<float> floatVector = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
            if (UnsafeAsInt_Vector128(floatVector) != BitConverter.SingleToInt32Bits(3.0f))
                return 0;

            Vector128<double> doubleVector = Vector128.Create(5.0, 6.0);
            if (UnsafeAsSecondDouble_Vector128(doubleVector) != 6.0)
                return 0;

            return 100;
        }
    }
}
