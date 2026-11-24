// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class HeadMergeException
{
    [Fact]
    public static int TestEntryPoint()
    {
        int local = 100;
        try
        {
            // Ensure head merging does not move the write to 'local' out here.
            if (Throws())
            {
                local = local + 1;
                local *= 2;
            }
            else
            {
                local = local + 1;
                local *= 3;
            }
        }
        catch (Exception)
        {
        }

        return local;
    }

    private class Receiver
    {
        public int Value = 3;
    }

    private sealed class OtherReceiver : Receiver
    {
    }

    private static int s_marker;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int ReadAfterTypeTest(Receiver receiver)
    {
        int value;
        // Both branches read receiver.Value after GetType has checked receiver.
        // Moving that read before the condition invalidates its non-null proof.
        if (receiver.GetType() == typeof(Receiver))
        {
            value = receiver.Value;
            s_marker = 1;
        }
        else
        {
            value = receiver.Value;
            s_marker = 2;
        }
        return value;
    }

    [Fact]
    public static void OrderedHeadKeepsTypeCheck()
    {
        Assert.Equal(3, ReadAfterTypeTest(new Receiver()));
        Assert.Equal(1, s_marker);
        Assert.Equal(3, ReadAfterTypeTest(new OtherReceiver()));
        Assert.Equal(2, s_marker);

        s_marker = 0;
        Assert.Throws<NullReferenceException>(() => ReadAfterTypeTest(null));
        Assert.Equal(0, s_marker);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int ReadAfterDivisionAndTypeTest(Receiver receiver, int divisor)
    {
        int value;
        // Both branches start with value = receiver.Value. Moving that assignment
        // before the condition would throw NullReferenceException instead of
        // DivideByZeroException when receiver is null and divisor is zero.
        if ((10 / divisor > 0) == (receiver.GetType() == typeof(Receiver)))
        {
            value = receiver.Value;
            s_marker = 1;
        }
        else
        {
            value = receiver.Value;
            s_marker = 2;
        }
        return value;
    }

    [Fact]
    public static void OrderedHeadKeepsExceptionOrder()
    {
        Assert.Equal(3, ReadAfterDivisionAndTypeTest(new Receiver(), 1));
        Assert.Equal(1, s_marker);
        Assert.Equal(3, ReadAfterDivisionAndTypeTest(new OtherReceiver(), 1));
        Assert.Equal(2, s_marker);

        s_marker = 0;
        Assert.Throws<DivideByZeroException>(() => ReadAfterDivisionAndTypeTest(null, 0));
        Assert.Equal(0, s_marker);
        Assert.Throws<NullReferenceException>(() => ReadAfterDivisionAndTypeTest(null, 1));
        Assert.Equal(0, s_marker);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Throws() => throw new Exception();
}
