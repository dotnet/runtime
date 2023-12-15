// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

// The jit should null check 'this' in NextElement

public unsafe struct GitHub_23791
{
    fixed byte A[10];

    [MethodImpl(MethodImplOptions.NoInlining)]
    byte NextElement(int i) => A[1+i];

    [Fact]
    public static int TestEntryPoint() 
    {
        int result = -1;
        GitHub_23791* x = null;
        bool threw = true;

        try
        {
            byte t = x->NextElement(100000);
            threw = false;
        }
        catch (NullReferenceException)
        {
            result = 100;
        }

        if (!threw)
        {
            Console.WriteLine($"FAIL: did not throw an exception");
        }

        return result;
    }
}
