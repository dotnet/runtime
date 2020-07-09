// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime;
using System.Collections.Generic;
using System.Diagnostics;

public class OptimizedCollect
{

    public static void Usage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("{0} <0|1|2>", Process.GetCurrentProcess().ProcessName);
    }

    protected List<byte[]> b;
    protected int collectionCount;
    protected int newCollectionCount;


    public void PreTest()
    {
        b = new List<byte[]>();
        collectionCount = 0;
    }

    public void RunTest(int gen)
    {

        newCollectionCount = collectionCount = GC.CollectionCount(gen);
        while (collectionCount == newCollectionCount)
        {
            b.Add(new byte[1024]);
            GC.Collect(gen, GCCollectionMode.Optimized);
            newCollectionCount = GC.CollectionCount(gen);
        }

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

        OptimizedCollect test = new OptimizedCollect();
        test.PreTest();
        test.RunTest(gen);

        return 100;
    }


}
