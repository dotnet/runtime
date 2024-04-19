// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for Switch recognition optimization

using System.Runtime.CompilerServices;
using Xunit;

namespace optSwitchRecognition;

public class CSwitchRecognitionTest
{
    // Test sorted char cases
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool RecSwitchSortedChar(char c)
    {
        return (c == 'a' || c == 'b' || c == 'd' || c == 'f') ? true : false;
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
    public static void TestRecSwitchSortedChar(char arg1, bool expected) => Assert.Equal(expected, RecSwitchSortedChar(arg1));

    // Test unsorted char cases
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool RecSwitchUnsortedChar(char c)
    {
        return (c == 'd' || c == 'f' || c == 'a' || c == 'b') ? true : false;
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
    public static void TestRecSwitchUnsortedChar(char arg1, bool expected) => Assert.Equal(expected, RecSwitchUnsortedChar(arg1));

    // Test sorted int cases
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool RecSwitchSortedInt(int i)
    {
        return (i == -10 || i == -20 || i == 30 || i == 40) ? true : false;
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
    public static void TestRecSwitchSortedInt(int arg1, bool expected) => Assert.Equal(expected, RecSwitchSortedInt(arg1));

    // Test unsorted int cases
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool RecSwitchUnsortedInt(int i)
    {
        return (i == 30 || i == 40 || i == -10 || i == -20) ? true : false;
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
    public static void TestRecSwitchUnsortedInt(int arg1, bool expected) => Assert.Equal(expected, RecSwitchUnsortedInt(arg1));

    // Test <= 64 switch cases
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool RecSwitch64JumpTables(int i)
    {
        return (i == 0 || i == 4 || i == 6 || i == 63) ? true : false;
    }

    [Theory]
    [InlineData(-63, false)]
    [InlineData(60, false)]
    [InlineData(63, true)]
    [InlineData(64, false)]
    public static void TestRecSwitch64JumpTables(int arg1, bool expected) => Assert.Equal(expected, RecSwitch64JumpTables(arg1));

    //
    // Skip optimization
    //

    // Test > 64 Switch cases (should skip Switch Recognition optimization)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool RecSwitch128JumpTables(int i)
    {
        return (i == 0 || i == 4 || i == 6 || i == 127);
    }

    [Theory]
    [InlineData(-127, false)]
    [InlineData(6, true)]
    [InlineData(127, true)]
    [InlineData(128, false)]
    public static void TestRecSwitch128JumpTables(int arg1, bool expected) => Assert.Equal(expected, RecSwitch128JumpTables(arg1));

    // Skips `bit test` conversion because Switch jump targets are > 2 (should skip Switch Recognition optimization)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RecSwitchSkipBitTest(int arch)
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
    public static void TestRecSwitchSkipBitTest(int arg1, int expected) => Assert.Equal(expected, RecSwitchSkipBitTest(arg1));
}
