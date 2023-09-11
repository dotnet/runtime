// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public struct MyClass
{
    public int x;
    public int y;
    public int z;
}

public class BringUpTest_InitObj
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool InitObj()
    {
        MyClass c = new MyClass();
        return c.x == c.y &&
               c.y == c.z &&
               c.z == 0;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        if (InitObj())
        {
            return Pass;
        }
        else
        {
            return Fail;
        }
    }
}
