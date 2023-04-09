// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime.CompilerServices;
using Xunit;

// This test case is meant to test an optimization in morph that
// transforms helper call JIT_Stelem_Ref(a, null, i) to a[i] = null,
// which further gets transformed into an array address and bounds 
// check nodes with references to the array local and the index
// local.  It is expected while doing such a transform, array
// local and index local are appropriately ref counted and Value
// number is updated post-global-morph and jit compilation 
// won't run into any asserts.
public class DevDiv_142976
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static String Foo()
    {
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Bar()
    {
        String[] args = new String[10];
        if (args != null) 
        {
            throw new Exception();
        }

        String s = Foo();
        if (s == null)
        {
            // This will result in JIT_Stelem_Ref(args, null, 0) call
            // which gets re-morphed into args[0] = null.
            args[0] = s;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Bar();
        }
        catch (Exception)
        {
        }
      
        Console.WriteLine("Pass");
        return 100;
    }
}
