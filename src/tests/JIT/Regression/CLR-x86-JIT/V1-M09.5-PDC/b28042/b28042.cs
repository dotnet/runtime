// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class A
{
    public static int[] B = new int[2];

    static void Test()
    {
        A[] aa;
        int n;
        for (aa = new A[7]; true; n = B[2] + B[2]) ;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Test();
        }
        catch (IndexOutOfRangeException) { }
        return 100;
    }
}
