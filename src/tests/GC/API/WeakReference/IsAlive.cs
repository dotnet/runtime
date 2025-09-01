// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests WeakReference.IsAlive : IsAlive=true if GC has not occurred on the object


using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public class Test_IsAlive {
    public static int[] array;
    public static WeakReference weak;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void CreateArray() {
        array = new int[50];
        // Create the weak reference inside of CreateArray to prevent a dangling 'array' reference
        // from surviving inside of TestEntryPoint.
        weak = new WeakReference(array);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void DestroyArray() {
        array = null;
    }

    [Fact]
    public static int TestEntryPoint() {
        CreateArray();

        bool ans1 = weak.IsAlive;
        Console.WriteLine(ans1);
        if (ans1 != true)
        {
            // This should be impossible; it would indicate that either the array was collected while reachable from
            //  our static field, or that the WeakReference failed to track the array even though it's still alive.
            Console.WriteLine("Test for WeakReference.IsAlive failed!");
            return 2;
        }

        // Release our strong reference (via static field) so that the collector will no longer see array as reachable.
        DestroyArray();
        // Perform a blocking full collection which will hopefully collect the array.
        GC.Collect();

        bool ans2 = weak.IsAlive;
        Console.WriteLine(ans2);

        if((ans1 == true) && (ans2==false)) {
            Console.WriteLine("Test for WeakReference.IsAlive passed!");
            return 100;
        }
        else {
            Console.WriteLine("Test for WeakReference.IsAlive failed!");
            return 1;
        }
    }
}
