// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

struct S
{
    public Program p;
    public int i;
}

struct T
{
    public S s;
}

class Program
{
    public T t;
    public T t1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    int Test()
    {
        // The bug was that a field sequence annotation was lost
        // when morph was transforming ADDR(IND(tree)) into tree
        // when processing this.t.s.p.
        if (this.t.s.p == this.t1.s.p)
        {
            return 0;
        }
        this.t1.s = this.t.s;
        int result = Helper(this.t.s);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Helper(S s)
    {
        return s.i;
    }

    static int Main()
    {
        Program p = new Program();
        p.t.s.i = 100;
        p.t.s.p = p;

        return p.Test();
    }
}
