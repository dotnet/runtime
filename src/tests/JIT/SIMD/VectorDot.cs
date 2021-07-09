// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorDotTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorDot(T left, T right, T checkResult)
        {
            const int Pass = 100;
            const int Fail = -1;

            Vector<T> A = new Vector<T>(left);
            Vector<T> B = new Vector<T>(right);

            T dotProduct = Vector.Dot<T>(A, B);
            if (!(CheckValue<T>(dotProduct, checkResult)))
            {
                Console.WriteLine("Dot product of Vector<" + typeof(T) + "> failed");
                return Fail;
            }

            return Pass;
        }
    }

    private class Vector4Test
    {
        public static int VectorDot(float left, float right, float checkResult)
        {
            const int Pass = 100;
            const int Fail = -1;

            Vector4 A = new Vector4(left);
            Vector4 B = new Vector4(right);

            float dotProduct = Vector4.Dot(A, B);
            if (!(CheckValue<float>(dotProduct, checkResult)))
            {
                Console.WriteLine("Dot product of Vector4 failed");
                return Fail;
            }

            return Pass;
        }
    }

    private class Vector3Test
    {
        public static int VectorDot(float left, float right, float checkResult)
        {
            const int Pass = 100;
            const int Fail = -1;

            Vector3 A = new Vector3(left);
            Vector3 B = new Vector3(right);

            float dotProduct = Vector3.Dot(A, B);
            if (!(CheckValue<float>(dotProduct, checkResult)))
            {
                Console.WriteLine("Dot product of Vector3 failed");
                return Fail;
            }

            return Pass;
        }
    }

    private class Vector2Test
    {
        public static int VectorDot(float left, float right, float checkResult)
        {
            const int Pass = 100;
            const int Fail = -1;

            Vector2 A = new Vector2(left);
            Vector2 B = new Vector2(right);

            float dotProduct = Vector2.Dot(A, B);
            if (!(CheckValue<float>(dotProduct, checkResult)))
            {
                Console.WriteLine("Dot product of Vector2 failed");
                return Fail;
            }

            return Pass;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;
        if (VectorDotTest<float>.VectorDot(3f, 2f, 6f * Vector<float>.Count) != Pass) returnVal = Fail;
        if (VectorDotTest<double>.VectorDot(3d, 2d, 6d * Vector<double>.Count) != Pass) returnVal = Fail;
        if (VectorDotTest<int>.VectorDot(3, 2, 6 * Vector<int>.Count) != Pass) returnVal = Fail;
        if (VectorDotTest<long>.VectorDot(3, 2, (long)(6 * Vector<long>.Count)) != Pass) returnVal = Fail;
        if (Vector4Test.VectorDot(3f, 2f, 24f) != Pass) returnVal = Fail;
        if (Vector3Test.VectorDot(3f, 2f, 18f) != Pass) returnVal = Fail;
        if (Vector2Test.VectorDot(3f, 2f, 12f) != Pass) returnVal = Fail;
        if (VectorDotTest<ushort>.VectorDot(3, 2, (ushort)(6 * Vector<ushort>.Count)) != Pass) returnVal = Fail;
        if (VectorDotTest<byte>.VectorDot(3, 2, (byte)(6 * Vector<byte>.Count)) != Pass) returnVal = Fail;
        if (VectorDotTest<short>.VectorDot(3, 2, (short)(6 * Vector<short>.Count)) != Pass) returnVal = Fail;
        if (VectorDotTest<sbyte>.VectorDot(3, 2, (sbyte)(6 * Vector<sbyte>.Count)) != Pass) returnVal = Fail;
        if (VectorDotTest<uint>.VectorDot(3u, 2u, (uint)(6 * Vector<uint>.Count)) != Pass) returnVal = Fail;
        if (VectorDotTest<ulong>.VectorDot(3ul, 2ul, 6ul * (ulong)Vector<ulong>.Count) != Pass) returnVal = Fail;
        if (VectorDotTest<nint>.VectorDot(3, 2, 6 * Vector<nint>.Count) != Pass) returnVal = Fail;
        if (VectorDotTest<nuint>.VectorDot(3u, 2u, (nuint)(6 * Vector<nuint>.Count)) != Pass) returnVal = Fail;

        JitLog jitLog = new JitLog();
        // Dot is only recognized as an intrinsic for floating point element types
        // and Vector<int> on AVX.
        if (!jitLog.Check("Dot", "Single")) returnVal = Fail;
        if (!jitLog.Check("Dot", "Double")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector4:Dot")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector3:Dot")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector2:Dot")) returnVal = Fail;
        if (Vector<int>.Count == 8)
        {
            if (!jitLog.Check("Dot", "Int32")) returnVal = Fail;
        }
        jitLog.Dispose();

        return returnVal;
    }
}
