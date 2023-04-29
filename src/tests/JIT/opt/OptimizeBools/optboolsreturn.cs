// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for boolean optimization

using System;
using System.Runtime.CompilerServices;
using Xunit;

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

    // Mixed int/obj can be optimized on 32-bit platforms, where `int` and `object` are the same size.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZeroNull(int x, object y)
    {
        return (x == 0 && y == null);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZero8(int x, int y, int z, int w, int a, int b, int c, int d)
    {
        return x == 0 && y == 0 && z == 0 && w == 0 && a == 0 && b == 0 && c == 0 && d == 0;
    }

    // Test cases that compute the same values as above, but don't directly return the values: instead,
    // store them, do something else, then return the values.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZeroWithOutput(int x, int y)
    {
        bool b = (x == 0 && y == 0);
        if (b)
        {
            Console.WriteLine("AreZeroWithOutput true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreNullWithOutput(object x, object y)
    {
        bool b = (x == null && y == null);
        if (b)
        {
            Console.WriteLine("AreNullWithOutput true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZero2WithOutput(int x, int y)
    {
        bool b = x == 0 && y == 0 && BitConverter.IsLittleEndian;
        if (b)
        {
            Console.WriteLine("AreZero2WithOutput true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZero3WithOutput(int x, int y, int z)
    {
        bool b = x == 0 && y == 0 && z == 0;
        if (b)
        {
            Console.WriteLine("AreZero3WithOutput true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZero3WithOutput2(int x, int y, int z)
    {
        if (x == 0 && y == 0 && z == 0)
        {
            Console.WriteLine("AreZero3WithOutput2 true");
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreZero4WithOutput(int x, int y, int z, int w)
    {
        bool b = x == 0 && y == 0 && z == 0 && w == 0;
        if (b)
        {
            Console.WriteLine("AreZero4WithOutput true");
        }
        return b;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool BothGreatThanZero(int x, int y)
    {
        return x >= 0 && y >= 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EitherLessThanZero(int x, int y)
    {
        return x < 0 || y < 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EitherNonZero(int x, int y)
    {
        return x != 0 || y != 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AreBothGreatThanZero(int x, int y)
    {
        bool b = x >= 0 && y >= 0;
        if (b)
        {
            Console.WriteLine("AreBothGreatThanZero true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsEitherLessThanZero(int x, int y)
    {
        bool b = x < 0 || y < 0;
        if (b)
        {
            Console.WriteLine("IsEitherLessThanZero true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsEitherNonZero(int x, int y)
    {
        bool b = x != 0 || y != 0;
        if (b)
        {
            Console.WriteLine("IsEitherNonZero true");
        }
        return b;
    }

    [Fact]
    public static int TestEntryPoint()
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

        if (AreZero(0, 1))
        {
            Console.WriteLine("CBoolTest:AreZero(0, 1) failed");
            return 101;
        }

        if (AreZero(1, 0))
        {
            Console.WriteLine("CBoolTest:AreZero(1, 0) failed");
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

        if (AreNull(null, new Object()))
        {
            Console.WriteLine("CBoolTest:AreNull(null, obj) failed");
            return 101;
        }

        if (AreNull(new Object(), null))
        {
            Console.WriteLine("CBoolTest:AreNull(obj, null) failed");
            return 101;
        }

        if (!AreZeroNull(0, null))
        {
            Console.WriteLine("CBoolTest:AreZeroNull(0, null) failed");
            return 101;
        }

        if (AreZeroNull(0, new Object()))
        {
            Console.WriteLine("CBoolTest:AreZeroNull(0, obj) failed");
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

        if (AreZero2(1, 0))
        {
            Console.WriteLine("CBoolTest:AreZero2(1, 0) failed");
            return 101;
        }

        if (!BothGreatThanZero(45, 23))
        {
            Console.WriteLine("CBoolTest:BothGreatThanZero(45, 23) failed");
            return 101;
        }

        if (!BothGreatThanZero(0, 23))
        {
            Console.WriteLine("CBoolTest:BothGreatThanZero(0, 23) failed");
            return 101;
        }

        if (!BothGreatThanZero(45, 0))
        {
            Console.WriteLine("CBoolTest:BothGreatThanZero(45, 0) failed");
            return 101;
        }

        if (!BothGreatThanZero(0, 0))
        {
            Console.WriteLine("CBoolTest:BothGreatThanZero(0, 0) failed");
            return 101;
        }

        if (BothGreatThanZero(-22, 23))
        {
            Console.WriteLine("CBoolTest:BothGreatThanZero(-22, 23) failed");
            return 101;
        }

        if (BothGreatThanZero(45, -36))
        {
            Console.WriteLine("CBoolTest:BothGreatThanZero(45, -36) failed");
            return 101;
        }

        if (BothGreatThanZero(-22, -36))
        {
            Console.WriteLine("CBoolTest:BothGreatThanZero(-22, -36) failed");
            return 101;
        }

        if (EitherLessThanZero(45, 23))
        {
            Console.WriteLine("CBoolTest:EitherLessThanZero(45, 23) failed");
            return 101;
        }

        if (EitherLessThanZero(0, 23))
        {
            Console.WriteLine("CBoolTest:EitherLessThanZero(0, 23) failed");
            return 101;
        }

        if (EitherLessThanZero(45, 0))
        {
            Console.WriteLine("CBoolTest:EitherLessThanZero(45, 0) failed");
            return 101;
        }

        if (EitherLessThanZero(0, 0))
        {
            Console.WriteLine("CBoolTest:EitherLessThanZero(0, 0) failed");
            return 101;
        }

        if (!EitherLessThanZero(-22, 23))
        {
            Console.WriteLine("CBoolTest:EitherLessThanZero(-22, 23) failed");
            return 101;
        }

        if (!EitherLessThanZero(45, -36))
        {
            Console.WriteLine("CBoolTest:EitherLessThanZero(45, -36) failed");
            return 101;
        }

        if (!EitherLessThanZero(-22, -36))
        {
            Console.WriteLine("CBoolTest:EitherLessThanZero(-22, -36) failed");
            return 101;
        }

        if (!EitherNonZero(45, 23))
        {
            Console.WriteLine("CBoolTest:EitherNonZero(45, 23) failed");
            return 101;
        }

        if (!EitherNonZero(0, 23))
        {
            Console.WriteLine("CBoolTest:EitherNonZero(0, 23) failed");
            return 101;
        }

        if (!EitherNonZero(45, 0))
        {
            Console.WriteLine("CBoolTest:EitherNonZero(45, 0) failed");
            return 101;
        }

        if (EitherNonZero(0, 0))
        {
            Console.WriteLine("CBoolTest:EitherNonZero(0, 0) failed");
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

        if (AreZero3(0, 1, 0))
        {
            Console.WriteLine("CBoolTest:AreZero3(0, 1, 0) failed");
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

        if (AreZero4(0, 0, 1, 0))
        {
            Console.WriteLine("CBoolTest:AreZero4(0, 0, 1, 0) failed");
            return 101;
        }

        if (!AreZero8(0, 0, 0, 0, 0, 0, 0, 0))
        {
            Console.WriteLine("CBoolTest:AreZero8(0, 0, 0, 0, 0, 0, 0, 0) failed");
            return 101;
        }

        if (AreZero8(0, 0, 0, 0, 0, 0, 0, 1))
        {
            Console.WriteLine("CBoolTest:AreZero8(0, 0, 0, 0, 0, 0, 0, 1) failed");
            return 101;
        }

        // With output (to force not a final `return`)

        if (!AreZeroWithOutput(0, 0))
        {
            Console.WriteLine("CBoolTest:AreZeroWithOutput(0, 0) failed");
            return 101;
        }

        if (AreZeroWithOutput(1, 1))
        {
            Console.WriteLine("CBoolTest:AreZeroWithOutput(1, 1) failed");
            return 101;
        }

        if (AreZeroWithOutput(0, 1))
        {
            Console.WriteLine("CBoolTest:AreZeroWithOutput(0, 1) failed");
            return 101;
        }

        if (AreZeroWithOutput(1, 0))
        {
            Console.WriteLine("CBoolTest:AreZeroWithOutput(1, 0) failed");
            return 101;
        }

        if (!AreBothGreatThanZero(45, 23))
        {
            Console.WriteLine("CBoolTest:AreBothGreatThanZero(45, 23) failed");
            return 101;
        }

        if (!AreBothGreatThanZero(0, 23))
        {
            Console.WriteLine("CBoolTest:AreBothGreatThanZero(0, 23) failed");
            return 101;
        }

        if (!AreBothGreatThanZero(45, 0))
        {
            Console.WriteLine("CBoolTest:AreBothGreatThanZero(45, 0) failed");
            return 101;
        }

        if (!AreBothGreatThanZero(0, 0))
        {
            Console.WriteLine("CBoolTest:AreBothGreatThanZero(0, 0) failed");
            return 101;
        }

        if (AreBothGreatThanZero(-22, 23))
        {
            Console.WriteLine("CBoolTest:AreBothGreatThanZero(-22, 23) failed");
            return 101;
        }

        if (AreBothGreatThanZero(45, -36))
        {
            Console.WriteLine("CBoolTest:AreBothGreatThanZero(45, -36) failed");
            return 101;
        }

        if (AreBothGreatThanZero(-22, -36))
        {
            Console.WriteLine("CBoolTest:AreBothGreatThanZero(-22, -36) failed");
            return 101;
        }

        if (IsEitherLessThanZero(45, 23))
        {
            Console.WriteLine("CBoolTest:IsEitherLessThanZero(45, 23) failed");
            return 101;
        }

        if (IsEitherLessThanZero(0, 23))
        {
            Console.WriteLine("CBoolTest:IsEitherLessThanZero(0, 23) failed");
            return 101;
        }

        if (IsEitherLessThanZero(45, 0))
        {
            Console.WriteLine("CBoolTest:IsEitherLessThanZero(45, 0) failed");
            return 101;
        }

        if (IsEitherLessThanZero(0, 0))
        {
            Console.WriteLine("CBoolTest:IsEitherLessThanZero(0, 0) failed");
            return 101;
        }

        if (!IsEitherLessThanZero(-22, 23))
        {
            Console.WriteLine("CBoolTest:IsEitherLessThanZero(-22, 23) failed");
            return 101;
        }

        if (!IsEitherLessThanZero(45, -36))
        {
            Console.WriteLine("CBoolTest:IsEitherLessThanZero(45, -36) failed");
            return 101;
        }

        if (!IsEitherLessThanZero(-22, -36))
        {
            Console.WriteLine("CBoolTest:IsEitherLessThanZero(-22, -36) failed");
            return 101;
        }

        if (!IsEitherNonZero(45, 23))
        {
            Console.WriteLine("CBoolTest:IsEitherNonZero(45, 23) failed");
            return 101;
        }

        if (!IsEitherNonZero(0, 23))
        {
            Console.WriteLine("CBoolTest:IsEitherNonZero(0, 23) failed");
            return 101;
        }

        if (!IsEitherNonZero(45, 0))
        {
            Console.WriteLine("CBoolTest:IsEitherNonZero(45, 0) failed");
            return 101;
        }

        if (IsEitherNonZero(0, 0))
        {
            Console.WriteLine("CBoolTest:IsEitherNonZero(0, 0) failed");
            return 101;
        }

        if (!AreNullWithOutput(null, null))
        {
            Console.WriteLine("CBoolTest:AreNullWithOutput(null, null) failed");
            return 101;
        }

        if (AreNullWithOutput(new Object(), new Object()))
        {
            Console.WriteLine("CBoolTest:AreNullWithOutput(obj, obj) failed");
            return 101;
        }

        if (AreNullWithOutput(null, new Object()))
        {
            Console.WriteLine("CBoolTest:AreNullWithOutput(null, obj) failed");
            return 101;
        }

        if (AreNullWithOutput(new Object(), null))
        {
            Console.WriteLine("CBoolTest:AreNullWithOutput(obj, null) failed");
            return 101;
        }

        if (!AreZero2WithOutput(0, 0))
        {
            Console.WriteLine("CBoolTest:AreZero2WithOutput(0, 0) failed");
            return 101;
        }

        if (AreZero2WithOutput(1, 0))
        {
            Console.WriteLine("CBoolTest:AreZero2(1, 0) failed");
            return 101;
        }

        if (!AreZero3WithOutput(0, 0, 0))
        {
            Console.WriteLine("CBoolTest:AreZero3WithOutput(0, 0, 0) failed");
            return 101;
        }

        if (AreZero3WithOutput(0, 1, 0))
        {
            Console.WriteLine("CBoolTest:AreZero3WithOutput(0, 1, 0) failed");
            return 101;
        }

        if (!AreZero3WithOutput2(0, 0, 0))
        {
            Console.WriteLine("CBoolTest:AreZero3WithOutput2(0, 0, 0) failed");
            return 101;
        }

        if (AreZero3WithOutput2(0, 1, 0))
        {
            Console.WriteLine("CBoolTest:AreZero3WithOutput2(0, 1, 0) failed");
            return 101;
        }

        if (!AreZero4WithOutput(0, 0, 0, 0))
        {
            Console.WriteLine("CBoolTest:AreZero4WithOutput(0, 0, 0, 0) failed");
            return 101;
        }

        if (AreZero4WithOutput(0, 0, 1, 0))
        {
            Console.WriteLine("CBoolTest:AreZero4WithOutput(0, 0, 1, 0) failed");
            return 101;
        }

        // Skip optimization

        // Test if ANDing or GT_NE requires both operands to be boolean
        if (!AreOne(1, 1))
        {
            Console.WriteLine("CBoolTest:AreOne(1, 1) failed");
            return 101;
        }

        // Skip cases where x or y is greater than 1
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

        Console.WriteLine("PASSED");
        return 100;
    }
}
