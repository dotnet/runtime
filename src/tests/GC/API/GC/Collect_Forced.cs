// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

public class DefaultCollect
{
    static string ProcessName;

    public static void Usage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("{0} <0|1|2>", ProcessName);
    }

    public static int Main(string[] args )
    {

        ProcessName = Process.GetCurrentProcess().ProcessName;
        int gen = -1;
        if ( (args.Length!=1) || (!Int32.TryParse(args[0], out gen)) )
        {
            Usage();
            return 0;
        }

        if ( (gen < 0) || (gen>2) )
        {
            Usage();
            return 0;
        }

        byte[] b = new byte[1024*1024*10];
        int oldCollectionCount = GC.CollectionCount(gen);
        b = null;

        GC.Collect(gen, GCCollectionMode.Forced);

        if (GC.CollectionCount(gen)>oldCollectionCount)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }

        Console.WriteLine("Test Failed");
        return 1;

    }
}
