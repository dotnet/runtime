// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

struct S
{
    public Vector<float> v1;
    public Vector<float> v2;
};

public static class GitHub_18497
{
    static S sStatic;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<float> Sum(S s)
    {
        return s.v1 + s.v2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<float> Test()
    {
        S sLocal = sStatic;
        return Sum(sLocal);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = true;
        sStatic.v1 = new Vector<float>(0.0F);
        sStatic.v2 = new Vector<float>(1.0F);
        Vector<float> v = Test();

        for (int i = 0; i < Vector<float>.Count; i++)
        {
            if (Math.Abs((double)(v[i] - 1.0F)) > (double)Single.Epsilon)
            {
                pass = false;
            }
        }
        if (!pass)
        {
            Console.WriteLine("Failed: v = " + v.ToString());
            return -1;
        }
        return 100;
    }
}
