// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public class BringUpTest_Unbox
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    unsafe public static int Unbox(object o)
    {
        return (int)o;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int r = 3;
        object o = r;
        int y = Unbox(o);
        if (y == 3) return Pass;
        else return Fail;
    }
}
