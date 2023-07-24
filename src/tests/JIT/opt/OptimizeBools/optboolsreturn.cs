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
    private static bool GreaterThanOrEqualZero(int x)
    {
        return x == 0 || x > 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreaterThanOrEqualZeroBis(int x)
    {
        return  x > 0 || x == 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessThanOrEqualZero(int x)
    {
        return x == 0 || x < 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessThanOrEqualZeroBis(int x)
    {
        return x < 0 || x == 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreaterThanZero(int x)
    {
        return x != 0 && x >= 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreaterThanZeroBis(int x)
    {
        return x >= 0 && x != 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessThanZero(int x)
    {
        return x != 0 && x <= 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessThanZeroBis(int x)
    {
        return x <= 0 && x != 0;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsGreaterThanOrEqualZero(int x)
    {
        bool b = x == 0 || x > 0;
        if (b)
        {
            Console.WriteLine("IsGreaterThanOrEqualZero true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsGreaterThanOrEqualZeroBis(int x)
    {
        bool b =  x > 0 || x == 0;
        if (b)
        {
            Console.WriteLine("IsGreaterThanOrEqualZeroBis true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsLessThanOrEqualZero(int x)
    {
        bool b = x == 0 || x < 0;
        if (b)
        {
            Console.WriteLine("IsLessThanOrEqualZero true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsLessThanOrEqualZeroBis(int x)
    {
        bool b =  x < 0 || x == 0;
        if (b)
        {
            Console.WriteLine("IsLessThanOrEqualZeroBis true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsGreaterThanZero(int x)
    {
        bool b = x != 0 && x >= 0;
        if (b)
        {
            Console.WriteLine("IsGreaterThanZero true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsGreaterThanZeroBis(int x)
    {
        bool b = x >= 0 && x != 0;
        if (b)
        {
            Console.WriteLine("IsGreaterThanZeroBis true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsLessThanZero(int x)
    {
        bool b = x != 0 && x <= 0;
        if (b)
        {
            Console.WriteLine("IsLessThanZero true");
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsLessThanZeroBis(int x)
    {
        bool b = x <= 0 && x != 0;
        if (b)
        {
            Console.WriteLine("IsLessThanZeroBis true");
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

        if (!GreaterThanOrEqualZero(10))
        {
            Console.WriteLine("CBoolTest:GreaterThanOrEqualZero(10) failed");
            return 101;
        }

        if (!GreaterThanOrEqualZero(0))
        {
            Console.WriteLine("CBoolTest:GreaterThanOrEqualZero(0) failed");
            return 101;
        }

        if (GreaterThanOrEqualZero(-10))
        {
            Console.WriteLine("CBoolTest:GreaterThanOrEqualZero(-10) failed");
            return 101;
        }

        if (!GreaterThanOrEqualZeroBis(10))
        {
            Console.WriteLine("CBoolTest:GreaterThanOrEqualZeroBis(10) failed");
            return 101;
        }

        if (!GreaterThanOrEqualZeroBis(0))
        {
            Console.WriteLine("CBoolTest:GreaterThanOrEqualZeroBis(0) failed");
            return 101;
        }

        if (GreaterThanOrEqualZeroBis(-10))
        {
            Console.WriteLine("CBoolTest:GreaterThanOrEqualZeroBis(-10) failed");
            return 101;
        }

        if (!GreaterThanZero(10))
        {
            Console.WriteLine("CBoolTest:GreaterThanZero(10) failed");
            return 101;
        }

        if (GreaterThanZero(0))
        {
            Console.WriteLine("CBoolTest:GreaterThanZero(0) failed");
            return 101;
        }

        if (GreaterThanZero(-10))
        {
            Console.WriteLine("CBoolTest:GreaterThanZero(-10) failed");
            return 101;
        }

        if (!GreaterThanZeroBis(10))
        {
            Console.WriteLine("CBoolTest:GreaterThanZeroBis(10) failed");
            return 101;
        }

        if (GreaterThanZeroBis(0))
        {
            Console.WriteLine("CBoolTest:GreaterThanZero(0) failed");
            return 101;
        }

        if (GreaterThanZeroBis(-10))
        {
            Console.WriteLine("CBoolTest:GreaterThanZero(-10) failed");
            return 101;
        }

        if (LessThanOrEqualZero(10))
        {
            Console.WriteLine("CBoolTest:LessThanOrEqualZero(10) failed");
            return 101;
        }

        if (!LessThanOrEqualZero(0))
        {
            Console.WriteLine("CBoolTest:LessThanOrEqualZero(0) failed");
            return 101;
        }

        if (!LessThanOrEqualZero(-10))
        {
            Console.WriteLine("CBoolTest:LessThanOrEqualZero(-10) failed");
            return 101;
        }

        if (LessThanOrEqualZeroBis(10))
        {
            Console.WriteLine("CBoolTest:LessThanOrEqualZeroBis(10) failed");
            return 101;
        }

        if (!LessThanOrEqualZeroBis(0))
        {
            Console.WriteLine("CBoolTest:LessThanOrEqualZeroBis(0) failed");
            return 101;
        }

        if (!LessThanOrEqualZeroBis(-10))
        {
            Console.WriteLine("CBoolTest:LessThanOrEqualZeroBis(-10) failed");
            return 101;
        }

        if (LessThanZero(10))
        {
            Console.WriteLine("CBoolTest:LessThanZero(10) failed");
            return 101;
        }

        if (LessThanZero(0))
        {
            Console.WriteLine("CBoolTest:LessThanZero(0) failed");
            return 101;
        }

        if (!LessThanZero(-10))
        {
            Console.WriteLine("CBoolTest:LessThanZero(-10) failed");
            return 101;
        }

        if (LessThanZeroBis(10))
        {
            Console.WriteLine("CBoolTest:LessThanZeroBis(10) failed");
            return 101;
        }

        if (LessThanZeroBis(0))
        {
            Console.WriteLine("CBoolTest:LessThanZeroBis(0) failed");
            return 101;
        }

        if (!LessThanZeroBis(-10))
        {
            Console.WriteLine("CBoolTest:LessThanZeroBis(-10) failed");
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

        if (!IsGreaterThanOrEqualZero(10))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanOrEqualZero(10) failed");
            return 101;
        }

        if (!IsGreaterThanOrEqualZero(0))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanOrEqualZero(0) failed");
            return 101;
        }

        if (IsGreaterThanOrEqualZero(-10))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanOrEqualZero(-10) failed");
            return 101;
        }

        if (!IsGreaterThanOrEqualZeroBis(10))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanOrEqualZeroBis(10) failed");
            return 101;
        }

        if (!IsGreaterThanOrEqualZeroBis(0))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanOrEqualZeroBis(0) failed");
            return 101;
        }

        if (IsGreaterThanOrEqualZeroBis(-10))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanOrEqualZeroBis(-10) failed");
            return 101;
        }

        if (!IsGreaterThanZero(10))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanZero(10) failed");
            return 101;
        }

        if (IsGreaterThanZero(0))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanZero(0) failed");
            return 101;
        }

        if (IsGreaterThanZero(-10))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanZero(-10) failed");
            return 101;
        }

        if (!IsGreaterThanZeroBis(10))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanZeroBis(10) failed");
            return 101;
        }

        if (IsGreaterThanZeroBis(0))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanZeroBis(0) failed");
            return 101;
        }

        if (IsGreaterThanZeroBis(-10))
        {
            Console.WriteLine("CBoolTest:IsGreaterThanZero(-10) failed");
            return 101;
        }

        if (IsLessThanOrEqualZero(10))
        {
            Console.WriteLine("CBoolTest:IsLessThanOrEqualZero(10) failed");
            return 101;
        }

        if (!IsLessThanOrEqualZero(0))
        {
            Console.WriteLine("CBoolTest:IsLessThanOrEqualZero(0) failed");
            return 101;
        }

        if (!IsLessThanOrEqualZero(-10))
        {
            Console.WriteLine("CBoolTest:IsLessThanOrEqualZero(-10) failed");
            return 101;
        }

        if (IsLessThanOrEqualZeroBis(10))
        {
            Console.WriteLine("CBoolTest:IsLessThanOrEqualZeroBis(10) failed");
            return 101;
        }

        if (!IsLessThanOrEqualZeroBis(0))
        {
            Console.WriteLine("CBoolTest:IsLessThanOrEqualZeroBis(0) failed");
            return 101;
        }

        if (!IsLessThanOrEqualZeroBis(-10))
        {
            Console.WriteLine("CBoolTest:IsLessThanOrEqualZeroBis(-10) failed");
            return 101;
        }

        if (IsLessThanZero(10))
        {
            Console.WriteLine("CBoolTest:IsLessThanZero(10) failed");
            return 101;
        }

        if (IsLessThanZero(0))
        {
            Console.WriteLine("CBoolTest:IsLessThanZero(0) failed");
            return 101;
        }

        if (!IsLessThanZero(-10))
        {
            Console.WriteLine("CBoolTest:IsLessThanZero(-10) failed");
            return 101;
        }

        if (IsLessThanZeroBis(10))
        {
            Console.WriteLine("CBoolTest:IsLessThanZeroBis(10) failed");
            return 101;
        }

        if (IsLessThanZeroBis(0))
        {
            Console.WriteLine("CBoolTest:IsLessThanZeroBis(0) failed");
            return 101;
        }

        if (!IsLessThanZeroBis(-10))
        {
            Console.WriteLine("CBoolTest:IsLessThanZeroBis(-10) failed");
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
