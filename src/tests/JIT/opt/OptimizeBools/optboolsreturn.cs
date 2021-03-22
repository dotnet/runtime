// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for boolean optimization

using System;
using System.Runtime.CompilerServices;

public class CBoolTest
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZero(int x, int y)
    {
        return (x == 0 && y == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreNull(object x, object y)
    {
        return (x == null && y == null);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZero2(int x, int y)
    {
        return x == 0 && y == 0 && BitConverter.IsLittleEndian;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZero3(int x, int y, int z)
    {
        return x == 0 && y == 0 && z == 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZero4(int x, int y, int z, int w)
    {
        return (x == 0 && y == 0 && z == 0 && w == 0);
    }

    // Cases that skip optimization
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreOne(int x, int y)
    {
        return (x == 1 && y == 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsEitherZero(int x, int y)
    {
        return (x == 0 || y == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsEitherOne(int x, int y)
    {
        return (x == 1 || y == 1);
    }

    public static int Main()
    {
        try
        {
            // Optimize boolean

            AreZero(0, 0);
            AreNull(null, null);
            AreNull(new Object(), new Object());
            AreZero(1, 1);
            AreZero2(0, 0);
            AreZero3(0, 0, 0);
            AreZero4(0, 0, 0, 0);

            // Skip optimization

            // Test if ANDing or GT_NE requires both operands to be boolean
            AreOne(1, 1);
            // Test if ANDing requires both operands to be boolean
            IsEitherZero(0, 1);
            // Test if GT_NE requires both operands to be boolean
            IsEitherOne(0, 1);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 101;
        }
        Console.WriteLine("PASSED");
        return 100;
    }
}
