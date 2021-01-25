// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;


internal class GCSmall
{
    internal int i;
    internal int j;

    public static void Main(string[] p_args) 
    {
        long iterations = 200000000;
        GCSmall ns = new GCSmall();

        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (long i = 0; i < iterations; i++)
        {
            ns = new GCSmall();
            ns = new GCSmall();
            ns = new GCSmall();
            ns = new GCSmall();
            ns = new GCSmall();
            ns = new GCSmall();
            ns = new GCSmall();
            ns = new GCSmall();
            ns = new GCSmall();
            ns = new GCSmall();
        }

        if(ns == null)
            Console.WriteLine("Shouldn't get here");            
        
        GC.KeepAlive(ns);
        
        sw.Stop();
        Console.WriteLine("took {0} ms in total", sw.ElapsedMilliseconds);
    }
}
