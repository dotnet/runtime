// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.CompilerServices;

// The jit should null check 'this' in NextElement

unsafe struct GitHub_23791
{
    fixed byte A[10];

    [MethodImpl(MethodImplOptions.NoInlining)]
    byte NextElement(int i) => A[1+i];

    static int Main() 
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
