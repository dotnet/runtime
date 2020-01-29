// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// Repro case for https://github.com/dotnet/coreclr/pull/17398

class X
{
    static int v;

    string s;

    public override string ToString() => s;

    [MethodImpl(MethodImplOptions.NoInlining)]
    X(int x)
    {
        s = "String" + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void F() { }

    public static void T0(object o, int x)
    {
        GC.Collect(2);
        throw new Exception(o.ToString());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test()
    {
        object x1 = new X(1);
        object x2 = new X(2);
        
        if (v == 1)
        {
            // Generate enough pressure here to 
            // kill ESI so in linear flow it is dead
            // at the call to T0
            int w = v;
            int x = v;
            int y = v;
            int z = v;

            // Unbounded loop here forces fully interruptible GC
            for (int i = 0; i < v; i++)
            {
                w++;
            }

            T0(x2, w + x + y + z);
        }

        // Encourage x1 to be in callee save (ESI)
        F();

        if (v == 2)
        {
            T0(x1, 0);
        }
    }

    public static int Main()
    {
        v = 1;
        int r = 0;

        try
        {
            Test();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            r = 100;
        }

        return r;
    }
}
