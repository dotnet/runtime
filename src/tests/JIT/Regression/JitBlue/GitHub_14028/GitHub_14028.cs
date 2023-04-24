// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

public class GitHub_10215
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test(Vector<int> x, Vector<int> y) => x[0] == y[0];

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = 100;
        Vector<int> X0 = new Vector<int>(0);
        Vector<int> X1 = new Vector<int>(1);
        Vector<int> Y0 = new Vector<int>(0);
        if (!Test(X0,Y0))
        {
            returnVal = -1;
        }
        if (Test(X1,Y0))
        {
            returnVal = -1;
        }
        return returnVal;
    }
}
