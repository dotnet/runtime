// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class DeadEH
{

    // This is the method that exposes the JIT bug
    // #1 - We have a 'live' parameter (i.e. it has real uses)
    // #2 - All EH is unreachable, but it requires some
    //      some optimization to discover (in sccprop.cpp)
    // #3 - There is at least on use of the parameter in the EH
    // #4 - All other uses of parameter are enregisterable
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Foo(object o)
    {
        // Use outside EH, that can be enregistered
        Console.WriteLine(o);
        bool bad = false;
        if (bad)
        {
            // Unreachable EH
            try
            {
                Console.WriteLine("in try");
            }
            catch
            {
                // Use inside handler
                Console.WriteLine("in handler");
                Console.WriteLine(o);
            }
        }
        // This goes boom
        GC.Collect();
        Console.WriteLine("almost done");
        Console.WriteLine(o);
    }

    // This is simply here so we can write something to the stack
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Bar(long l)
    {
        Console.WriteLine(l.ToString());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DoIt()
    {
        // Put a non-GC pointer in the to-be-mis-reported slot
        Bar(0x5a5a5a5a5a5a5a5aL);
        // Expose the bug
        Foo("testing");
    }

    // Get everything jitted before we call
    [Fact]
    public static int TestEntryPoint()
    {
        Foo("prep");
        Bar(0);
        DoIt();
        return 100;
    }
}
