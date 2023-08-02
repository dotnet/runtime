// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for Switch recognition optimization

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class CSwitchRecognitionTest
{
    // Test sorted char cases
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool RecSwitchSortedChar(char c)
    {
        return (c == 'a' || c == 'b' || c == 'd' || c == 'f');
    }

    [Theory]
    [InlineData('a', true)]
    [InlineData('b', true)]
    [InlineData('c', false)]
    [InlineData('d', true)]
    [InlineData('e', false)]
    [InlineData('f', true)]
    [InlineData('z', false)]
    [InlineData('A', false)]
    [InlineData('Z', false)]
    [InlineData('?', false)]
    public static void TestRecSwitchSortedChar(char arg1,bool expected) => Assert.Equal(RecSwitchSortedChar(arg1), expected);

    // Test unsorted char cases
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool RecSwitchUnsortedChar(char c)
    {
        return (c == 'd' || c == 'f' || c == 'a' || c == 'b');
    }

    [Theory]
    [InlineData('a', true)]
    [InlineData('b', true)]
    [InlineData('c', false)]
    [InlineData('d', true)]
    [InlineData('e', false)]
    [InlineData('f', true)]
    [InlineData('z', false)]
    [InlineData('A', false)]
    [InlineData('Z', false)]
    [InlineData('?', false)]
    public static void TestRecSwitchUnsortedChar(char arg1, bool expected) => Assert.Equal(RecSwitchUnsortedChar(arg1), expected);

    // Test sorted int cases
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool RecSwitchSortedInt(int i)
    {
        return (i == -10 || i == -20 || i == 30 || i == 40);
    }

    [Theory]
    [InlineData(-100, false)]
    [InlineData(-10, true)]
    [InlineData(-20, true)]
    [InlineData(0, false)]
    [InlineData(30, true)]
    [InlineData(35, false)]
    [InlineData(40, true)]
    [InlineData(70, false)]
    [InlineData(100, false)]
    public static void TestRecSwitchSortedInt(int arg1, bool expected) => Assert.Equal(RecSwitchSortedInt(arg1), expected);

    // Test unsorted int cases
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool RecSwitchUnsortedInt(int i)
    {
        return (i == 30 || i == 40 || i == -10 || i == -20);
    }

    [Theory]
    [InlineData(-100, false)]
    [InlineData(-10, true)]
    [InlineData(-20, true)]
    [InlineData(0, false)]
    [InlineData(30, true)]
    [InlineData(35, false)]
    [InlineData(40, true)]
    [InlineData(70, false)]
    [InlineData(100, false)]
    public static void TestRecSwitchUnsortedInt(int arg1, bool expected) => Assert.Equal(RecSwitchUnsortedInt(arg1), expected);

    // Test min limits
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool RecSwitchMinLimits(int i)
    {
        return (i == -9223372036854775807 || i == -9223372036854775806
                || i == -9223372036854775804 || i == -9223372036854775802);
    }

    [Theory]
    [InlineData(-9223372036854775807, true)]
    [InlineData(-9223372036854775806, true)]
    [InlineData(-9223372036854775805, false)]
    [InlineData(-9223372036854775804, true)]
    [InlineData(-9223372036854775802, true)]
    public static void TestRecSwitchMinLimits(int arg1, bool expected) => Assert.Equal(RecSwitchMinLimits(arg1), expected);

    // Test max limits
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool RecSwitchMaxLimits(int i)
    {
        return (i == 9223372036854775807 || i == 9223372036854775806
                || i == 9223372036854775804 || i == 9223372036854775802);
    }

    [Theory]
    [InlineData(9223372036854775807, true)]
    [InlineData(9223372036854775806, true)]
    [InlineData(9223372036854775805, false)]
    [InlineData(9223372036854775804, true)]
    [InlineData(9223372036854775802, true)]
    public static void TestRecSwitchMaxLimits(int arg1, bool expected) => Assert.Equal(RecSwitchMaxLimits(arg1), expected);

    // Test <= 64 switch cases
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool RecSwitch64JumpTables(int i)
    {
        return (i == 0 || i == 4 || i == 6 || i == 63);
    }

    [Theory]
    [InlineData(-63, false)]
    [InlineData(60, false)]
    [InlineData(63, true)]
    [InlineData(64, false)]
    public static void TestRecSwitch64JumpTables(int arg1, bool expected) => Assert.Equal(RecSwitch64JumpTables(arg1), expected);

    //
    // Skip optimization
    //

    // Test > 64 Switch cases (should skip Switch Recognition optimization)
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool RecSwitch128JumpTables(int i)
    {
        return (i == 0 || i == 4 || i == 6 || i == 127);
    }

    [Theory]
    [InlineData(-127, false)]
    [InlineData(6, true)]
    [InlineData(127, true)]
    [InlineData(128, false)]
    public static void TestRecSwitch128JumpTables(int arg1, bool expected) => Assert.Equal(RecSwitch128JumpTables(arg1), expected);

    // Skips `bit test` conversion because Switch jump targets are > 2 (should skip Switch Recognition optimization)
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int RecSwitchSkipBitTest(int arch)
    {
        if (arch == 1)
            return 2;
        else if (arch == 2 || arch == 6)
            return 4;
        else
            return 1;
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(6, 4)]
    [InlineData(10, 1)]
    public static void TestRecSwitchSkipBitTest(int arg1, int expected) => Assert.Equal(RecSwitchSkipBitTest(arg1), expected);
}
