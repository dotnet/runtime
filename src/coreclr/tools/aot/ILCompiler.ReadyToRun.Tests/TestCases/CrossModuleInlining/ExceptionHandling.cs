// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public static class ExceptionHandling
{
    public static int CallDependency() => InlineableLib.GetValue() + MethodWithSwitchTable(5);

    public static int MethodWithExceptionInfo(int value)
    {
        try
        {
            return 100 / value;
        }
        catch (DivideByZeroException)
        {
            return -1;
        }
    }

    public static int MethodWithSwitchTable(int value)
    {
        return value switch
        {
            0 => Case0(),
            1 => Case1(),
            2 => Case2(),
            3 => Case3(),
            4 => Case4(),
            5 => Case5(),
            6 => Case6(),
            _ => CaseDefault(),
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Case0() => 10;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Case1() => 11;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Case2() => 12;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Case3() => 13;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Case4() => 14;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Case5() => 15;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Case6() => 16;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CaseDefault() => 17;
}
