// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using Xunit;

public partial class VectorTest
{
    private static int VectorIntEquals()
    {
        const int Pass = 100;
        const int Fail = -1;

        Vector<int> A = new Vector<int>(3);
        Vector<int> B = new Vector<int>(3);
        Vector<int> C = new Vector<int>(5);

        bool result = A.Equals(B);
        if (!result)
        {
            return Fail;
        }

        result = A.Equals(C);
        if (result)
        {
            return Fail;
        }

        if (A.Equals(Vector<int>.Zero))
        {
            return Fail;
        }

        if (!Vector<int>.Zero.Equals(Vector<int>.Zero))
        {
            return Fail;
        }

        if (Vector<int>.Zero.Equals(B))
        {
            return Fail;
        }

        if (!(A == B))
        {
            return Fail;
        }

        if (A == Vector<int>.Zero)
        {
            return Fail;
        }

        if (!(A != Vector<int>.Zero))
        {
            return Fail;
        }

        if (A != B)
        {
            return Fail;
        }

        if (!(A != C))
        {
            return Fail;
        }

        if (!(Vector<int>.Zero != A))
        {
            return Fail;
        }

        if (Vector<int>.Zero != Vector<int>.Zero)
        {
            return Fail;
        }

        return Pass;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return VectorIntEquals();
    }
}
