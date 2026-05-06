// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using Xunit;

public partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorMulTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorMul(T left, T right, T leftTimesRight, T leftTimesRightSquared, T rightTimesRight)
        {
            Vector<T> A = new Vector<T>(left);
            Vector<T> B = new Vector<T>(right);

            Vector<T> C = A * B;
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!(CheckValue<T>(C[i], leftTimesRight)))
                {
                    Console.WriteLine("FAILED Loop1: C[" + i + "] = " + C[i] + "; should be " + leftTimesRight);
                    return Fail;
                }
            }

            C = C * C;
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!(CheckValue<T>(C[i], leftTimesRightSquared)))
                {
                    Console.WriteLine("FAILED Loop2: C[" + i + "] = " + C[i] + "; should be " + leftTimesRight);
                    return Fail;
                }
            }

            B = B * B;
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!(CheckValue<T>(B[i], rightTimesRight)))
                {
                    Console.WriteLine("FAILED Loop3: B[" + i + "] = " + C[i] + "; should be " + leftTimesRight);
                    return Fail;
                }
            }

            return Pass;
        }
    }
    private class Vector4Test
    {
        public static int VectorMul(float left, float right, float result)
        {
            Vector4 A = new Vector4(left);
            Vector4 B = new Vector4(right);
            Vector4 C = A * B;
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            if (!(CheckValue<float>(C.Z, result))) return Fail;
            if (!(CheckValue<float>(C.W, result))) return Fail;
            return Pass;
        }
    }

    private class Vector3Test
    {
        public static int VectorMul(float left, float right, float result)
        {
            Vector3 A = new Vector3(left);
            Vector3 B = new Vector3(right);
            Vector3 C = A * B;
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            if (!(CheckValue<float>(C.Z, result))) return Fail;
            return Pass;
        }
    }

    private class Vector2Test
    {
        public static int VectorMul(float left, float right, float result)
        {
            Vector2 A = new Vector2(left);
            Vector2 B = new Vector2(right);
            Vector2 C = A * B;
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            return Pass;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = Pass;
        if (VectorMulTest<float>.VectorMul(2, 3, (float)(2 * 3), (float)(2 * 3) * (2 * 3), (float)(3 * 3)) != Pass)
            returnVal = Fail;
        if (VectorMulTest<double>.VectorMul(2, 3, (double)(2 * 3), (double)(2 * 3) * (2 * 3), (double)(3 * 3)) != Pass)
            returnVal = Fail;
        if (VectorMulTest<int>.VectorMul(2, 3, (2 * 3), (2 * 3) * (2 * 3), (3 * 3)) != Pass)
            returnVal = Fail;
        if (VectorMulTest<long>.VectorMul(2, 3, (long)(2 * 3), (long)(2 * 3) * (2 * 3), (long)(3 * 3)) != Pass)
            returnVal = Fail;
        if (Vector4Test.VectorMul(2, 3, (float)(2 * 3)) != Pass) returnVal = Fail;
        if (Vector3Test.VectorMul(2, 3, (float)(2 * 3)) != Pass) returnVal = Fail;
        if (Vector2Test.VectorMul(2, 3, (float)(2 * 3)) != Pass) returnVal = Fail;
        if (VectorMulTest<ushort>.VectorMul(2, 3, (ushort)(2 * 3), (ushort)(2 * 3) * (2 * 3), (ushort)(3 * 3)) != Pass)
            returnVal = Fail;
        if (VectorMulTest<byte>.VectorMul(2, 3, (byte)(2 * 3), (byte)(2 * 3) * (2 * 3), (byte)(3 * 3)) != Pass)
            returnVal = Fail;
        if (VectorMulTest<short>.VectorMul(2, 3, (short)(2 * 3), (short)(2 * 3) * (2 * 3), (short)(3 * 3)) != Pass)
            returnVal = Fail;
        if (VectorMulTest<sbyte>.VectorMul(2, 3, (sbyte)(2 * 3), (sbyte)(2 * 3) * (2 * 3), (sbyte)(3 * 3)) != Pass)
            returnVal = Fail;
        if (VectorMulTest<uint>.VectorMul(2u, 3u, 2u * 3u, (2u * 3u) * (2u * 3u), (3u * 3u)) != Pass)
            returnVal = Fail;
        if (VectorMulTest<ulong>.VectorMul(2ul, 3ul, 2ul * 3ul, (2ul * 3ul) * (2ul * 3ul), (3ul * 3ul)) != Pass)
            returnVal = Fail;
        if (VectorMulTest<nint>.VectorMul(2, 3, (2 * 3), (2 * 3) * (2 * 3), (3 * 3)) != Pass)
            returnVal = Fail;
        if (VectorMulTest<nuint>.VectorMul(2u, 3u, 2u * 3u, (2u * 3u) * (2u * 3u), (3u * 3u)) != Pass)
            returnVal = Fail;

        JitLog jitLog = new JitLog();
        // Multiply is supported only for float, double, int and short
        if (!jitLog.Check("op_Multiply", "Single")) returnVal = Fail;
        if (!jitLog.Check("op_Multiply", "Double")) returnVal = Fail;
        if (!jitLog.Check("op_Multiply", "Int32")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector4:op_Multiply")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector3:op_Multiply")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector2:op_Multiply")) returnVal = Fail;
        if (!jitLog.Check("op_Multiply", "Int16")) returnVal = Fail;
        jitLog.Dispose();

        return returnVal;
    }
}
