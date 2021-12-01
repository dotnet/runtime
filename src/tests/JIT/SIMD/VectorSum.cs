// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorSumTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorSum(T a, T b)
        {
            Vector<T> A = new Vector<T>(a);
            T B = Vector.Sum(A);

            if (!(CheckValue<T>(B, b)))
            {
                return Fail;
            }
            return Pass;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;

        if (VectorSumTest<float>.VectorSum(1, (float)Vector<float>.Count) != Pass) returnVal = Fail;
        if (VectorSumTest<double>.VectorSum(1, (double)Vector<double>.Count) != Pass) returnVal = Fail;
        if (VectorSumTest<int>.VectorSum(1, (int)Vector<int>.Count) != Pass) returnVal = Fail;
        if (VectorSumTest<long>.VectorSum(1, (long)Vector<long>.Count) != Pass) returnVal = Fail;
        if (VectorSumTest<ushort>.VectorSum(1, (ushort)Vector<ushort>.Count) != Pass) returnVal = Fail;
        if (VectorSumTest<byte>.VectorSum(1, (byte)Vector<byte>.Count) != Pass) returnVal = Fail;
        if (VectorSumTest<short>.VectorSum(-1, (short)(-Vector<short>.Count)) != Pass) returnVal = Fail;
        if (VectorSumTest<sbyte>.VectorSum(-1, (sbyte)(-Vector<sbyte>.Count)) != Pass) returnVal = Fail;
        if (VectorSumTest<uint>.VectorSum(0x41000000u, 0x41000000u * (uint)Vector<uint>.Count) != Pass) returnVal = Fail;
        if (VectorSumTest<ulong>.VectorSum(0x4100000000000000ul, 0x4100000000000000ul * (uint)Vector<ulong>.Count) != Pass) returnVal = Fail;
        if (VectorSumTest<nint>.VectorSum(1, (nint)Vector<nint>.Count) != Pass) returnVal = Fail;
        if (VectorSumTest<nuint>.VectorSum(0x41000000u, 0x41000000u * (nuint)(uint)Vector<nuint>.Count) != Pass) returnVal = Fail;

        JitLog jitLog = new JitLog();

        if (Sse3.IsSupported || AdvSimd.IsSupported)
        {
            if (!jitLog.Check("Sum", "Single")) returnVal = Fail;
            if (!jitLog.Check("Sum", "Double")) returnVal = Fail;
        }

        if (Ssse3.IsSupported || AdvSimd.IsSupported)
        {
            if (!jitLog.Check("Sum", "Int16")) returnVal = Fail;
            if (!jitLog.Check("Sum", "Int32")) returnVal = Fail;
            if (!jitLog.Check("Sum", "UInt16")) returnVal = Fail;
            if (!jitLog.Check("Sum", "UInt32")) returnVal = Fail;
        }

        if (AdvSimd.IsSupported)
        {
            if (!jitLog.Check("Sum", "Byte")) returnVal = Fail;
            if (!jitLog.Check("Sum", "Int64")) returnVal = Fail;
            if (!jitLog.Check("Sum", "IntPtr")) returnVal = Fail;
            if (!jitLog.Check("Sum", "SByte")) returnVal = Fail;
            if (!jitLog.Check("Sum", "UInt64")) returnVal = Fail;
            if (!jitLog.Check("Sum", "UIntPtr")) returnVal = Fail;
        }

        jitLog.Dispose();

        return returnVal;
    }
}
