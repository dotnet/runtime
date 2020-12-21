// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GC.TotalMemory

using System;

public class Test {

    public static int Main() {

        GC.Collect();
        GC.Collect();

        int[] array1 = new int[20000];
        int memold = (int) GC.GetTotalMemory(false);
        Console.WriteLine("Total Memory: " + memold);
        
        array1=null;
        GC.Collect();
        
        int[] array2 = new int[40000];
        int memnew = (int) GC.GetTotalMemory(false);
        Console.WriteLine("Total Memory: " + memnew);
        GC.KeepAlive(array2);

        if(memnew >= memold) {
            Console.WriteLine("Test for GC.TotalMemory passed!");
            return 100;
        }
        else {
            Console.WriteLine("Test for GC.TotalMemory failed!");
            return 1;
        }
    }
}
 
