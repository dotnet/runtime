// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_63610
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test(int[] x)
    {
        try
        {
             Callee1(x);
        }
        catch
        {
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Callee1(int[] x) => Callee2(x, 0);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Callee2(int[] x, int index)
    {
        if (x == null)
            Callee3();

        return x.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Callee3() {}

    [Fact]
    public static int TestEntryPoint()
    {
        // Make sure it doesn't assert 
        // https://github.com/dotnet/runtime/issues/63610
        Test(new int[42]);
        return 100;
    }
}
