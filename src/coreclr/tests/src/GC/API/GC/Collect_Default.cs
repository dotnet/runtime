// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

public class DefaultCollect
{
    public static void Usage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("{0} <0|1|2>", Process.GetCurrentProcess().ProcessName);
    }

    public static int Main(string[] args )
    {

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

        GC.Collect(gen, GCCollectionMode.Default);

        if (GC.CollectionCount(gen)>oldCollectionCount)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }

        Console.WriteLine("Test Failed");
        return 1;

    }
}
