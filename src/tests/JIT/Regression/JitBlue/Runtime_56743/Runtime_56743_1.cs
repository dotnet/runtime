// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

unsafe class Runtime_56743_1
{
    [MethodImpl(MethodImplOptions.NoOptimization)]
    static int Main()
    {
        int result = Foo(default);
        return result == 0 ? 100 : -1;
    }

    static S* s_s;
    static int Foo(S s)
    {
        s_s = &s;
        return Bar(s);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Bar(S h)
    {
        s_s->A = 10;
        return h.A;
    }

    struct S
    {
        public int A, B, C, D;
    }
}
