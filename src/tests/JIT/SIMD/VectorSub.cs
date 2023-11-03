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

    private class VectorSubTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorSub(T a, T b, T c)
        {
            Vector<T> A = new Vector<T>(a);
            Vector<T> B = new Vector<T>(b);
            Vector<T> C = A - B;
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!(CheckValue<T>(C[i], c)))
                {
                    return Fail;
                }
            }
            return Pass;
        }
    }
    private class Vector4Test
    {
        public static int VectorSub()
        {
            Vector4 A = new Vector4(3);
            Vector4 B = new Vector4(2);
            Vector4 C = A - B;
            if (!(CheckValue<float>(C.X, 1f))) return Fail;
            if (!(CheckValue<float>(C.Y, 1f))) return Fail;
            if (!(CheckValue<float>(C.Z, 1f))) return Fail;
            if (!(CheckValue<float>(C.W, 1f))) return Fail;
            return Pass;
        }
    }
    private class Vector3Test
    {
        public static int VectorSub()
        {
            Vector3 A = new Vector3(3);
            Vector3 B = new Vector3(2);
            Vector3 C = A - B;
            if (!(CheckValue<float>(C.X, 1f))) return Fail;
            if (!(CheckValue<float>(C.Y, 1f))) return Fail;
            if (!(CheckValue<float>(C.Z, 1f))) return Fail;
            return Pass;
        }
    }
    private class Vector2Test
    {
        public static int VectorSub()
        {
            Vector2 A = new Vector2(4, 3);
            Vector2 B = new Vector2(3, 2);
            Vector2 C = A - B;
            if (!(CheckValue<float>(C.X, 1f))) return Fail;
            if (!(CheckValue<float>(C.Y, 1f))) return Fail;
            return Pass;
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = Pass;
        if (VectorSubTest<float>.VectorSub(3, 2, (float)(3 - 2)) != Pass) returnVal = Fail;
        if (VectorSubTest<double>.VectorSub(3, 2, (float)(3 - 2)) != Pass) returnVal = Fail;
        if (VectorSubTest<int>.VectorSub(3, 2, (int)(3 - 2)) != Pass) returnVal = Fail;
        if (VectorSubTest<long>.VectorSub(3, 2, (long)(3 - 2)) != Pass) returnVal = Fail;
        if (Vector3Test.VectorSub() != Pass) returnVal = Fail;
        if (Vector2Test.VectorSub() != Pass) returnVal = Fail;
        if (VectorSubTest<ushort>.VectorSub(3, 2, (ushort)(3 - 2)) != Pass) returnVal = Fail;
        if (VectorSubTest<byte>.VectorSub(3, 2, (byte)(3 - 2)) != Pass) returnVal = Fail;
        if (VectorSubTest<short>.VectorSub(3, -2, (short)(3 + 2)) != Pass) returnVal = Fail;
        if (VectorSubTest<sbyte>.VectorSub(3, -2, (sbyte)(3 + 2)) != Pass) returnVal = Fail;
        if (VectorSubTest<uint>.VectorSub(0x42000000u, 0x41000000u, 0x42000000u - 0x41000000u) != Pass) returnVal = Fail;
        if (VectorSubTest<ulong>.VectorSub(0x42000000ul, 0x41000000ul, 0x42000000ul - 0x41000000ul) != Pass) returnVal = Fail;
        if (VectorSubTest<nint>.VectorSub(3, 2, (nint)(3 - 2)) != Pass) returnVal = Fail;
        if (VectorSubTest<nuint>.VectorSub(0x42000000u, 0x41000000u, 0x42000000u - 0x41000000u) != Pass) returnVal = Fail;
        return returnVal;
    }
}
