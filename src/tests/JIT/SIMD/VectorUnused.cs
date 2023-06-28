// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// The Rationalizer was not correctly handling a case of an unused SIMD expression
// involving a localVar or temporary value, where the SIMD expression is returning a non-SIMD
// value, and the expression is sufficiently complex (e.g. a call to vector * scalar which is
// inlined but not an intrinsic).

using System;
using System.Numerics;
using Xunit;

public partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorUnusedTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorUnused(T t1, T t2)
        {
            Vector<T> v1 = new Vector<T>(t1);
            v1.Equals(Vector<T>.One * t2);
            return Pass;
        }
    }

    private class Vector4Test
    {
        public static int VectorUnused()
        {
            Vector4 v1 = new Vector4(3f);
            Vector4.Dot(default(Vector4) * 2f, Vector4.One);
            Vector4.Dot(v1, Vector4.One * 2f);
            v1.Equals(Vector4.One * 3f);
            return Pass;
        }
    }

    private class Vector3Test
    {
        public static int VectorUnused()
        {
            Vector3 v1 = new Vector3(3f);
            Vector3.Dot(default(Vector3) * 2f, Vector3.One);
            Vector3.Dot(v1, Vector3.One * 2f);
            v1.Equals(Vector3.One * 3f);
            return Pass;
        }
    }

    private class Vector2Test
    {
        public static int VectorUnused()
        {
            Vector2 v1 = new Vector2(3f);
            Vector2.Dot(default(Vector2) * 2f, Vector2.One);
            Vector2.Dot(v1, Vector2.One * 2f);
            v1.Equals(Vector2.One * 3f);
            return Pass;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = Pass;

        if (VectorUnusedTest<float>.VectorUnused(3f, 2f) != Pass) returnVal = Fail;
        if (VectorUnusedTest<double>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (VectorUnusedTest<int>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (VectorUnusedTest<long>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (VectorUnusedTest<ushort>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (VectorUnusedTest<byte>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (VectorUnusedTest<short>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (VectorUnusedTest<sbyte>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (VectorUnusedTest<uint>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (VectorUnusedTest<ulong>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (VectorUnusedTest<nint>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (VectorUnusedTest<nuint>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector4Test.VectorUnused() != Pass) returnVal = Fail;
        if (Vector3Test.VectorUnused() != Pass) returnVal = Fail;
        if (Vector2Test.VectorUnused() != Pass) returnVal = Fail;
        return returnVal;
    }
}
