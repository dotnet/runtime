// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Runtime_54842
{
    public static int Main()
    {
        try
        {
            DoubleCheckedConvert(uint.MaxValue);
        }
        catch (OverflowException)
        {
            return 100;
        }

        return 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint DoubleCheckedConvert(ulong a)
    {
        var b = (int)checked((uint)a);

        // Make sure the importer spills "b" to a local.
        Use(b);

        return checked((uint)b);
    }

    private static void Use(int value) { }
}
