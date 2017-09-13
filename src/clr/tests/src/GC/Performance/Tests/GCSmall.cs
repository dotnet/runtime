// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class GCSmall
{
    internal int i;
    internal int j;

    public static void Main(string[] p_args) 
    {
        long iterations = 200;
        GCSmall ns = new GCSmall();

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
    }
}
