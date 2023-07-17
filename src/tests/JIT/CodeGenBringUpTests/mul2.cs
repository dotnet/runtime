// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Text;
using System.Runtime.CompilerServices;
using Xunit;

struct vc
{
    public int x;
    public int y;
    public int z;
    public vc (int xx, int yy, int zz) { x = xx; y = yy; z = zz; }
}

public class child
{
    const int Pass = 100;
    const int Fail = -1;

    [Fact]
    public static int TestEntryPoint()
    {
        int result = mul2(3);
        if (result == 15)
            return Pass;
        else
            return Fail;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)] 
    public static int mul2(int a)
    {
        return a*5;
    }
    
}

