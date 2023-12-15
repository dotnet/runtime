// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    enum LongEnum : long
    {
        Option0, Option1, Option2
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string Test(LongEnum v)
    {
        string s;
        switch (v)
        {
        case LongEnum.Option0: s = "Option0"; break;
        case LongEnum.Option1: s = "Option1"; break;
        case LongEnum.Option2: s = "Option2"; break;
        default: throw new Exception();
        }
        return s;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return (Test(LongEnum.Option0) == "Option0") ? 100 : 1;
    }
}
