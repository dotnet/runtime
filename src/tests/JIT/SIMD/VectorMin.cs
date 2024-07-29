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

    private class VectorMinTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorMin(T left, T right, T result)
        {
            Vector<T> A = new Vector<T>(left);
            Vector<T> B = new Vector<T>(right);

            Vector<T> C = Vector.Min<T>(A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!(CheckValue<T>(C[i], result)))
                {
                    return Fail;
                }
            }
            return Pass;
        }
    }
    private class Vector4Test
    {
        public static int VectorMin(float left, float right, float result)
        {
            Vector4 A = new Vector4(left);
            Vector4 B = new Vector4(right);
            Vector4 C = Vector4.Min(A, B);
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            if (!(CheckValue<float>(C.Z, result))) return Fail;
            if (!(CheckValue<float>(C.W, result))) return Fail;
            return Pass;
        }
    }

    private class Vector3Test
    {
        public static int VectorMin(float left, float right, float result)
        {
            Vector3 A = new Vector3(left);
            Vector3 B = new Vector3(right);
            Vector3 C = Vector3.Min(A, B);
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            if (!(CheckValue<float>(C.Z, result))) return Fail;
            return Pass;
        }
    }

    private class Vector2Test
    {
        public static int VectorMin(float left, float right, float result)
        {
            Vector2 A = new Vector2(left);
            Vector2 B = new Vector2(right);
            Vector2 C = Vector2.Min(A, B);
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            return Pass;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = Pass;
        if (VectorMinTest<float>.VectorMin(2f, 3f, 2f) != Pass) returnVal = Fail;
        if (VectorMinTest<double>.VectorMin(2d, 3d, 2d) != Pass) returnVal = Fail;
        if (VectorMinTest<int>.VectorMin(2, 3, 2) != Pass) returnVal = Fail;
        if (VectorMinTest<long>.VectorMin(2, 3, 2) != Pass) returnVal = Fail;
        if (Vector4Test.VectorMin(2f, 3f, 2f) != Pass) returnVal = Fail;
        if (Vector3Test.VectorMin(2f, 3f, 2f) != Pass) returnVal = Fail;
        if (Vector2Test.VectorMin(2f, 3f, 2f) != Pass) returnVal = Fail;
        if (VectorMinTest<ushort>.VectorMin(2, 3, 2) != Pass) returnVal = Fail;
        if (VectorMinTest<byte>.VectorMin(2, 3, 2) != Pass) returnVal = Fail;
        if (VectorMinTest<short>.VectorMin(-2, -3, -3) != Pass) returnVal = Fail;
        if (VectorMinTest<sbyte>.VectorMin(-2, 3, -2) != Pass) returnVal = Fail;
        if (VectorMinTest<uint>.VectorMin(0x80000000u, 0x40000000u, 0x40000000u) != Pass) returnVal = Fail;
        if (VectorMinTest<ulong>.VectorMin(2ul, 3ul, 2ul) != Pass) returnVal = Fail;
        if (VectorMinTest<nint>.VectorMin(2, 3, 2) != Pass) returnVal = Fail;
        if (VectorMinTest<nuint>.VectorMin(0x80000000u, 0x40000000u, 0x40000000u) != Pass) returnVal = Fail;

        JitLog jitLog = new JitLog();
        if (!jitLog.Check("Min", "Single")) returnVal = Fail;
        if (!jitLog.Check("Min", "Double")) returnVal = Fail;
        if (!jitLog.Check("Min", "Int32")) returnVal = Fail;
        if (!jitLog.Check("Min", "Int64")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector4:Min")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector3:Min")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector2:Min")) returnVal = Fail;
        if (!jitLog.Check("Min", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("Min", "Byte")) returnVal = Fail;
        if (!jitLog.Check("Min", "Int16")) returnVal = Fail;
        if (!jitLog.Check("Min", "SByte")) returnVal = Fail;
        if (!jitLog.Check("Min", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("Min", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("Min", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("Min", "UIntPtr")) returnVal = Fail;
        jitLog.Dispose();

        return returnVal;
    }
}
