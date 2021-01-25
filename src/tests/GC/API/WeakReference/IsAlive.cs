// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests WeakReference.IsAlive : IsAlive=true if GC has not occurred on the object 


using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class Test {
    public static int[] array;
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void CreateArray() {
        array = new int[50];
    }
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void DestroyArray() {
        array = null;
    }
    
    public static int Main() {
        CreateArray();

        WeakReference weak = new WeakReference(array);
                
        bool ans1 = weak.IsAlive;
        Console.WriteLine(ans1);

        if(ans1==false) { // GC.Collect() has already occurred..under GCStress
            Console.WriteLine("Test for WeakReference.IsAlive passed!");
            return 100;
        }

        //else, do an expicit collect.
        DestroyArray();
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
