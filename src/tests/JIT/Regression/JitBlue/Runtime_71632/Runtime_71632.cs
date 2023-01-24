// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

public class Runtime_71632
{
    public static int Main()
    {
        try
        {
            Problem(1);
            return 101;
        }
        catch (InvalidCastException) {}

        try
        {
            Problem2(1);
            return 102;
        }
        catch (InvalidCastException) {}

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte Problem(byte a)
    {
        object box = a;
        Use(ref box);
        return (sbyte)box;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte Problem2(byte a)
    {
        object box = a;
        return (sbyte)box;
    }

    static void Use<T>(ref T arg) { }
}
