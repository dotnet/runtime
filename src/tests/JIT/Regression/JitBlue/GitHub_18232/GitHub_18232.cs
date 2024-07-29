// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Test cases for issues with optVNConstantPropOnTree/optPrepareTreeForReplacement/gtExtractSideEffList.

public class Program
{
    static int[,] s_1 = new int[1, 1] { { 42 } };
    static ushort[,] s_2 = new ushort[,] { { 0 } };

    [Fact]
    public static int TestEntryPoint()
    {
        if (!Test1() || (s_1[0, 0] != 0))
        {
            return -1;
        }

        if (!Test2())
        {
            return -1;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test1()
    {
        try
        {
            M(s_1[0, 0] & 0);
        }
        catch (NullReferenceException)
        {
            return false;
        }

        return true;
    }

    static void M(int arg0)
    {
        s_1[0, 0] = arg0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test2()
    {
        try
        {
            bool vr5 = 0 == ((0 % ((0 & s_2[0, 0]) | 1)) * s_2[0, 0]);
        }
        catch (NullReferenceException)
        {
            return false;
        }

        return true;
    }
}
