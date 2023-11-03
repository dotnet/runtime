// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class BringUpTest_ArrayObj
{
    const int Pass = 100;
    const int Fail = -1;

    class Dummy
    {
        public int field;
        public Dummy(int f)
        {
            field = f;
        }
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int ArrayObj(int i)
    {
        Dummy[] a = {new Dummy(0), new Dummy(1), new Dummy(2), new Dummy(3), new Dummy(4),
                     new Dummy(5), new Dummy(6), new Dummy(7), new Dummy(8), new Dummy(9)};
        return a[i].field;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (ArrayObj(1) != 1) return Fail;
        return Pass;
    }
}
