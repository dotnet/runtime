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
        // Optimize boolean

        if (!AreZero(0, 0))
        {
            Console.WriteLine("CBoolTest:AreZero(0, 0) failed");
            return 101;
        }

        if (AreZero(1, 1))
        {
            Console.WriteLine("CBoolTest:AreZero(1, 1) failed");
            return 101;
        }

        if (AreZero(0, 2))
        {
            Console.WriteLine("CBoolTest:AreZero(0, 2) failed");
            return 101;
        }

        if (AreZero(3, 0))
        {
            Console.WriteLine("CBoolTest:AreZero(3, 0) failed");
            return 101;
        }

        if (!AreNull(null, null))
        {
            Console.WriteLine("CBoolTest:AreNull(null, null) failed");
            return 101;
        }

        if (AreNull(new Object(), new Object()))
        {
            Console.WriteLine("CBoolTest:AreNull(obj, obj) failed");
            return 101;
        }

        if (!AreZero2(0, 0))
        {
            Console.WriteLine("CBoolTest:AreZero2(0, 0) failed");
            return 101;
        }

        if (AreZero2(2, 1))
        {
            Console.WriteLine("CBoolTest:AreZero2(2, 1) failed");
            return 101;
        }

        if (!AreZero3(0, 0, 0))
        {
            Console.WriteLine("CBoolTest:AreZero3(0, 0, 0) failed");
            return 101;
        }

        if (AreZero3(0, 1, 2))
        {
            Console.WriteLine("CBoolTest:AreZero3(0, 1, 2) failed");
            return 101;
        }

        if (!AreZero4(0, 0, 0, 0))
        {
            Console.WriteLine("CBoolTest:AreZero4(0, 0, 0, 0) failed");
            return 101;
        }

        if (AreZero4(0, 1, 2, 3))
        {
            Console.WriteLine("CBoolTest:AreZero4(0, 1, 2, 3) failed");
            return 101;
        }

        // Skip optimization

        // Test if ANDing or GT_NE requires both operands to be boolean
        if (!AreOne(1, 1))
        {
            Console.WriteLine("CBoolTest:AreOne(1, 1) failed");
            return 101;
        }

        // Skip cases where x or y is greather than 1
        if (AreOne(3, 1))
        {
            Console.WriteLine("CBoolTest:AreOne(1, 3) failed");
            return 101;
        }

        // Test if ANDing requires both operands to be boolean
        if (!IsEitherZero(0, 1))
        {
            Console.WriteLine("CBoolTest:IsEitherZero(0, 1) failed");
            return 101;
        }

        // Skip cases where x and y have opposite bits set
        if (IsEitherZero(2, 1))
        {
            Console.WriteLine("CBoolTest:IsEitherZero(2, 1) failed");
            return 101;
        }

        // Test if GT_NE requires both operands to be boolean
        if (!IsEitherOne(0, 1))
        {
            Console.WriteLine("CBoolTest:IsEitherOne(0, 1) failed");
            return 101;
        }

        // Skip cases where either x or y is greater than 1
        if (IsEitherOne(2, 0))
        {
            Console.WriteLine("CBoolTest:IsEitherOne(2, 0) failed");
            return 101;
        }

        return 100;
    }
}
