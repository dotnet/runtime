// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests WeakReference.IsAlive : IsAlive=true if GC has not occurred on the object 


using System;
using System.Runtime.InteropServices;

public class Test {
    public static int Main() {
        int[] array = new int[50];

        WeakReference weak = new WeakReference(array);
                
        bool ans1 = weak.IsAlive;
        Console.WriteLine(ans1);

        if(ans1==false) { // GC.Collect() has already occurred..under GCStress
            Console.WriteLine("Test for WeakReference.IsAlive passed!");
            return 100;
        }

        //else, do an expicit collect.
        array=null;
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
