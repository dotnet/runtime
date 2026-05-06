// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public class child
{
    [Fact]
    public static int TestEntryPoint()
    {
        int b = 5;
        const int Pass = 100;
        const int Fail = -1;

        int result = divref(12, ref b);
        if (result == 2)
            return Pass;
        else
            return Fail;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int divref(int a, ref int b)
    {
        return a / b;
    }
}

