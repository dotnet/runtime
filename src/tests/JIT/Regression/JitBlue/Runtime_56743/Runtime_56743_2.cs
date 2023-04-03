// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_56743_2
{
    [MethodImpl(MethodImplOptions.NoOptimization)]
    [Fact]
    public static int TestEntryPoint()
    {
        int result = Foo(default);
        return result == 0 ? 100 : -1;
    }

    static int Foo(S h)
    {
        h.H = &h;
        return Bar(h);
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Bar(S h)
    {
        h.H->A = 10;
        return h.A;
    }
    
    unsafe struct S
    {
        public int A, B;
        public S* H;
    }
}
