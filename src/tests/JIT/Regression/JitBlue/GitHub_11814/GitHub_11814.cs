// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Repro case for a bug involving failure to rewrite all references
// to a promoted implicit byref struct parameter.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MutateStructArg
{
    public struct P
    {
        public string S;
        public int X;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        P l1 = new P();
        l1.S = "Hello World";
        l1.X = 42;
        P l2 = foo(l1);
        Console.WriteLine(l2.S); // Print modified value "Goodbye World"
        if ((l2.S == "Goodbye World") && (l2.X == 100))
        {
            return 100;   // success
        }
        else
        {
            Console.WriteLine("**** Test FAILED ***");
            return 1;  // failure
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static P foo(P a)
    {
        Console.WriteLine(a.S);   // Print the incoming value "Hello World"
        a.S = "Goodbye World";    // Mutate the incoming value
        a.X = 100;
        return a;                 // Copy the modified value to the return value (bug was that this was returning original unmodified arg)
    }
}
