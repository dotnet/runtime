// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public struct SequentialStruct
{
    public short f0;
    public int f1;
    public float f2;
    public IntPtr f3;
}

public class Test_GitHub_18482
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int foo(SequentialStruct s)
    {
        if ((s.f0 != 100) || (s.f1 != 1) || ((int) s.f2 != 10) || ((int)s.f3 != 42))
        {
            Console.WriteLine(s.f0);
            Console.WriteLine(s.f1);
            Console.WriteLine(s.f2);
            Console.WriteLine(s.f3);
            return -1;
        }
        return 100;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        SequentialStruct ss = new SequentialStruct();
        ss.f0 = 100;
        ss.f1 = 1;
        ss.f2 = 10.0f;
        ss.f3 = new IntPtr(42);
        return foo(ss);
    }
}
