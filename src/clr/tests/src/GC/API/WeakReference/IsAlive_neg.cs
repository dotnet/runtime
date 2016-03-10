// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Negative Test for WeakReference.IsAlive
// IsAlive=false if GC occurs on object with only a weakreference.

using System;
using System.Runtime.InteropServices;

public class Test {
    public static int Main() {
        int[] array = new int[50];

        WeakReference weak = new WeakReference(array); // array has ONLY a weakreference

        // ensuring that GC happens even with /debug mode
        array=null;

        GC.Collect();

        bool ans = weak.IsAlive;
        Console.WriteLine(ans);

        if(ans == false) {
            Console.WriteLine("Negative Test for WeakReference.IsAlive passed!");
            return 100;
        }
        else {
            Console.WriteLine("Negative Test for WeakReference.IsAlive failed!");
            return 1;
        }
    }
}
