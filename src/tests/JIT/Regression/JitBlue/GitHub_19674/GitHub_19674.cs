// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public static class GitHub_19674
{
    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = 100;
        try
        {
            var vec0 = new Vector3(0, 0, 0);
            Test1(vec0);
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL Test1: " + e.Message);
            returnVal = -1;
        }
        try
        {
            var vec0 = new Vector3(0, 0, 0);
            Test2(vec0);
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL Test1: " + e.Message);
            returnVal = -1;
        }
        if (returnVal == 100)
        {
            Console.WriteLine("PASS");
        }
        return returnVal;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test1(Vector3 vec0)
    {
        // This was causing an assert (in Checked) or an access violation on the `new` line.
        vec0.X = -vec0.X;
        new Vector3(vec0.X, vec0.Y, vec0.Z);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test2(Vector3 vec0)
    {
        // This was causing an assert (in Checked) or a null reference exception on the `new` line
        vec0.X = 0;
        new Vector3(vec0.X, vec0.Y, vec0.Z);
    }
}
