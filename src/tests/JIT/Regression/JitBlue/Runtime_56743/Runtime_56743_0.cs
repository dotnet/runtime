// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Runtime_56743_0
{
    [MethodImpl(MethodImplOptions.NoOptimization)]
    static int Main()
    {
        int result = Foo(default, default);
        return result == 0 ? 100 : -1;
    }

    static int Foo(S s, Span<S> span)
    {
        span = MemoryMarshal.CreateSpan(ref s, 1);
        return Bar(s, span);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Bar(S h, Span<S> s)
    {
        s[0].A = 10;
        return h.A;
    }

    struct S
    {
        public int A, B, C, D;
    }
}
