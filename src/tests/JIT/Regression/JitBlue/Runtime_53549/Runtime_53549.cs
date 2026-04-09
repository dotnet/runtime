// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

interface I
{
    public Decimal F();
}

public class Runtime_53549 : I
{
    Decimal z;

    public Decimal F() => z;

    public static bool G(object o) 
    {
        return ((decimal) o).Equals(100M);
    }

    // This method will have bad codegen if
    // we allow GDV on i.F().
    //
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int H(I i)
    {
        return G(i.F()) ? 100 : -1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Runtime_53549 x = new Runtime_53549();
        x.z = 100M;

        for (int i = 0; i < 100; i++)
        {
            _ = H(x);
            Thread.Sleep(15);
        }

        return H(x);
    }
}


    
