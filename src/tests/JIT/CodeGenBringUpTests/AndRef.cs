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
        const int Pass = 100;
        const int Fail = -1;

        int x = 13;
        int result = AndRef(15, ref x);

        if (result == 13)
            return Pass;
        else
            return Fail;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)] 
    public static int AndRef(int x, ref int a)
    {
        x &= a;
        return x;
    }
    
}

