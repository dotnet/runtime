// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Repro case for a bug involving byref-typed appearances of int-typed lclVars.

using System.Runtime.CompilerServices;

struct S
{
    int i;

    void N()
    {
        i = 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe S* Test(S* s)
    {
        s->N();
        return s;
    }

    static unsafe int Main()
    {
        S s;
        return Test(&s)->i;
    }
}
