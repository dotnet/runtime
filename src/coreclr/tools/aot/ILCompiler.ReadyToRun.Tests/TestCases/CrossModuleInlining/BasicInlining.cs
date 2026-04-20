// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public static class BasicInlining
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestGetValue()
    {
        return InlineableLib.GetValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string TestGetString()
    {
        return InlineableLib.GetString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestAdd()
    {
        return InlineableLib.Add(10, 32);
    }
}
