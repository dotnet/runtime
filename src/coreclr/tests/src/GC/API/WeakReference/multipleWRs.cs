// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

public class Test
{

    List<byte[]> strongRefs;
    List<WeakReference> weakRefs;

    public Test(int numElems, bool track)
    {
        strongRefs = new List<byte[]>();
        weakRefs = new List<WeakReference>();

        for (int i = 0; i < numElems; i++)
        {
            byte[] data = new byte[1000];
            data[0] = 0xC;

            strongRefs.Add(data);            
            weakRefs.Add(new WeakReference(data, track));
        }
    }


    public int Calculate()
    {
        int count = 0;
        foreach (WeakReference w in weakRefs)
        {
            if (w.Target!=null)
            {
                ++count;
            }
        }
        return count;
    }


    public static void Usage()
    {
        Console.WriteLine("USAGE: MultipleWR.exe <num objects> [track]");
    }


    public static int Main(string[] args)
    {

        int numElems = 0;
        if ((args.Length==0) || (!Int32.TryParse(args[0], out numElems)))
        {
            Usage();
            return 1;
        }
        
        bool track = false;
        if (args.Length==2)
        {
            track = (args[1].ToLower()=="track");
        }
        
        

        Test test = new Test(numElems, track);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        int count = test.Calculate();

        Console.WriteLine("Number of live references: {0}", numElems);
        Console.WriteLine("Number of live WeakReferences: {0}", count);
        
        // this KeepAlive is necessary so test isn't collected before we get the weakreference count
        GC.KeepAlive(test); 

        if (count!=numElems)
        {                
            Console.WriteLine("Test Failed");
            return 1;
        }            

        Console.WriteLine("Test Passed");
        return 100;            

    }

}

