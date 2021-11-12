// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests WeakReference.TrackResurrection
// Retrieves a boolean indicating whether objects are tracked.

// TRUE: The reference will refer to the target until it is reclaimed by the Runtime
//     (until collection).
// FALSE: The reference will refer to the target until the first time it is detected
//        to be unreachable by Runtime (until Finalization).


using System;

public class Test_TrackResurrection {
    public static int Main() {
        int[] array = new int[50];
        Object obj = new Object();

        WeakReference weak1 = new WeakReference(array,true);
        WeakReference weak2 = new WeakReference(obj,false);

        
        bool ans1 = weak1.TrackResurrection;        
        bool ans2 = weak2.TrackResurrection;    


        if((ans1 == true) && (ans2 == false)) {
            Console.WriteLine("Test for WeakReference.TrackResurrection passed!");
            return 100;
        }
        else {
            Console.WriteLine("Test for WeakReference.TrackResurrection failed!");
            return 1;
        }
    }
}
