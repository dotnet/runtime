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

    private class VectorHWAccelTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorHWAccel2(T a, T b, T c)
        {
            Vector<T> A = new Vector<T>(a);
            Vector<T> B = new Vector<T>(b);
            Vector<T> C = A + B;
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

    [Fact]
    public static int TestEntryPoint()
    {
        if (Vector.IsHardwareAccelerated)
        {
            // The test harness will check to ensure that this method was compiled, which it will
            // not be if IsHardwareAccelerated returns false.
            return VectorHWAccelTest<float>.VectorHWAccel2(1, 2, (float)(1 + 2));
        }
        return Pass;
    }
}
