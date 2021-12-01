// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorMaxTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorMax(T left, T right, T result)
        {
            Vector<T> A = new Vector<T>(left);
            Vector<T> B = new Vector<T>(right);

            Vector<T> C = Vector.Max<T>(A, B);
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
        public static int VectorMax(float left, float right, float result)
        {
            Vector4 A = new Vector4(left);
            Vector4 B = new Vector4(right);
            Vector4 C = Vector4.Max(A, B);
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            if (!(CheckValue<float>(C.Z, result))) return Fail;
            if (!(CheckValue<float>(C.W, result))) return Fail;
            return Pass;
        }
    }

    private class Vector3Test
    {
        public static int VectorMax(float left, float right, float result)
        {
            Vector3 A = new Vector3(left);
            Vector3 B = new Vector3(right);
            Vector3 C = Vector3.Max(A, B);
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            if (!(CheckValue<float>(C.Z, result))) return Fail;
            return Pass;
        }
    }

    private class Vector2Test
    {
        public static int VectorMax(float left, float right, float result)
        {
            Vector2 A = new Vector2(left);
            Vector2 B = new Vector2(right);
            Vector2 C = Vector2.Max(A, B);
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            return Pass;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;
        if (VectorMaxTest<float>.VectorMax(2f, 3f, 3f) != Pass) returnVal = Fail;
        if (VectorMaxTest<double>.VectorMax(2d, 3d, 3d) != Pass) returnVal = Fail;
        if (VectorMaxTest<int>.VectorMax(2, 3, 3) != Pass) returnVal = Fail;
        if (VectorMaxTest<long>.VectorMax(2, 3, 3) != Pass) returnVal = Fail;
        if (Vector4Test.VectorMax(2f, 3f, 3f) != Pass) returnVal = Fail;
        if (Vector3Test.VectorMax(2f, 3f, 3f) != Pass) returnVal = Fail;
        if (Vector2Test.VectorMax(2f, 3f, 3f) != Pass) returnVal = Fail;
        if (VectorMaxTest<ushort>.VectorMax(2, 3, 3) != Pass) returnVal = Fail;
        if (VectorMaxTest<byte>.VectorMax(2, 3, 3) != Pass) returnVal = Fail;
        if (VectorMaxTest<short>.VectorMax(-2, -3, -2) != Pass) returnVal = Fail;
        if (VectorMaxTest<sbyte>.VectorMax(-2, 3, 3) != Pass) returnVal = Fail;
        if (VectorMaxTest<uint>.VectorMax(0x80000000u, 0x40000000u, 0x80000000u) != Pass) returnVal = Fail;
        if (VectorMaxTest<ulong>.VectorMax(2ul, 3ul, 3ul) != Pass) returnVal = Fail;
        if (VectorMaxTest<nint>.VectorMax(2, 3, 3) != Pass) returnVal = Fail;
        if (VectorMaxTest<nuint>.VectorMax(0x80000000u, 0x40000000u, 0x80000000u) != Pass) returnVal = Fail;

        JitLog jitLog = new JitLog();
        if (!jitLog.Check("Max", "Single")) returnVal = Fail;
        if (!jitLog.Check("Max", "Double")) returnVal = Fail;
        if (!jitLog.Check("Max", "Int32")) returnVal = Fail;
        if (!jitLog.Check("Max", "Int64")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector4:Max")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector3:Max")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector2:Max")) returnVal = Fail;
        if (!jitLog.Check("Max", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("Max", "Byte")) returnVal = Fail;
        if (!jitLog.Check("Max", "Int16")) returnVal = Fail;
        if (!jitLog.Check("Max", "SByte")) returnVal = Fail;
        if (!jitLog.Check("Max", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("Max", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("Max", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("Max", "UIntPtr")) returnVal = Fail;
        jitLog.Dispose();

        return returnVal;
    }
}
