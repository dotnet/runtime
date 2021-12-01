// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorMulTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorDiv(T left, T right, T result)
        {
            Vector<T> A = new Vector<T>(left);
            Vector<T> B = new Vector<T>(right);

            Vector<T> C = A / B;
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
        public static int VectorDiv(float left, float right, float result)
        {
            Vector4 A = new Vector4(left);
            Vector4 B = new Vector4(right);
            Vector4 C = A / B;
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            if (!(CheckValue<float>(C.Z, result))) return Fail;
            if (!(CheckValue<float>(C.W, result))) return Fail;
            return Pass;
        }
    }

    private class Vector3Test
    {
        public static int VectorDiv(float left, float right, float result)
        {
            Vector3 A = new Vector3(left);
            Vector3 B = new Vector3(right);
            Vector3 C = A / B;
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            if (!(CheckValue<float>(C.Z, result))) return Fail;
            return Pass;
        }
    }

    private class Vector2Test
    {
        public static int VectorDiv(float left, float right, float result)
        {
            Vector2 A = new Vector2(left);
            Vector2 B = new Vector2(right);
            Vector2 C = A / B;
            if (!(CheckValue<float>(C.X, result))) return Fail;
            if (!(CheckValue<float>(C.Y, result))) return Fail;
            return Pass;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;
        if (VectorMulTest<float>.VectorDiv(6f, 2f, 6f / 2f) != Pass) returnVal = Fail;
        if (VectorMulTest<double>.VectorDiv(8d, 4d, 8d / 4d) != Pass) returnVal = Fail;
        if (VectorMulTest<int>.VectorDiv(6, 3, 2) != Pass) returnVal = Fail;
        if (returnVal == Fail)
        {
            Console.WriteLine("Failed after int");
        }
        if (VectorMulTest<long>.VectorDiv(8, 2, 4) != Pass) returnVal = Fail;
        if (returnVal == Fail)
        {
            Console.WriteLine("Failed after long");
        }
        if (Vector4Test.VectorDiv(8f, 3f, 8f / 3f) != Pass)
        {
            Console.WriteLine("Vector4Test.VectorDiv failed");
            returnVal = Fail;
        }
        if (Vector3Test.VectorDiv(8f, 3f, 8f / 3f) != Pass)
        {
            Console.WriteLine("Vector3Test.VectorDiv failed");
            returnVal = Fail;
        }
        if (Vector2Test.VectorDiv(7f, 2f, 7f / 2f) != Pass)
        {
            Console.WriteLine("Vector2Test.VectorDiv failed");
            returnVal = Fail;
        }
        if (VectorMulTest<ushort>.VectorDiv(6, 3, 2) != Pass) returnVal = Fail;
        if (VectorMulTest<byte>.VectorDiv(6, 3, 2) != Pass) returnVal = Fail;
        if (VectorMulTest<short>.VectorDiv(6, -3, -2) != Pass) returnVal = Fail;
        if (VectorMulTest<sbyte>.VectorDiv(6, -3, -2) != Pass) returnVal = Fail;
        if (VectorMulTest<uint>.VectorDiv(6u, 3u, 2u) != Pass) returnVal = Fail;
        if (VectorMulTest<ulong>.VectorDiv(8ul, 2ul, 4ul) != Pass) returnVal = Fail;
        if (VectorMulTest<nint>.VectorDiv(6, 3, 2) != Pass) returnVal = Fail;
        if (VectorMulTest<nuint>.VectorDiv(6u, 3u, 2u) != Pass) returnVal = Fail;

        JitLog jitLog = new JitLog();
        // Division is only recognized as an intrinsic for floating point element types.
        if (!jitLog.Check("op_Division", "Single")) returnVal = Fail;
        if (!jitLog.Check("op_Division", "Double")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector4:op_Division")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector3:op_Division")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector2:op_Division")) returnVal = Fail;
        jitLog.Dispose();

        return returnVal;
    }
}
