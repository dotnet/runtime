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
        public static int VectorHWAccel(T a, T b, T c)
        {
            if (Vector.IsHardwareAccelerated)
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
            else
            {
                return Pass;
            }
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = Pass;
        if (VectorHWAccelTest<float>.VectorHWAccel(1, 2, (float)(1 + 2)) != Pass) returnVal = Fail;
        return returnVal;
    }
}
