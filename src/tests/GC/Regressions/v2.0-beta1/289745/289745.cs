// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 *  DESCRIPTION:    GC clobbers read-only frozen segments
 *  NOTE:           if unable to repro, tweak the array lengths depending on the amount of physical ram in your machine
 */


using System;

public class Test
{
    public static int FACTOR = 1024;

    public class Dummy
    {
        public int[] data;
        public Dummy()
        {
            data = new int[FACTOR * FACTOR];
        }
    }

    public static int Main(String[] args)
    {
        int iterations = 250;

        try
        {
            iterations = int.Parse(args[0]);
        }
        catch
        {
            Console.WriteLine("Using default number of iterations: 250");
        }

        Console.WriteLine("Creating arrays...");
        Console.WriteLine("test fails if asserts or hangs here");

        try
        {
            Dummy[] arr = new Dummy[iterations];
            for (int i = 0; i < arr.Length; i++)
            {
                // test fails if asserts or hangs here
                Console.WriteLine(i);
                arr[i] = new Dummy();
            }
        }
        catch (OutOfMemoryException)
        {
            // need to bail here
        }

        Console.WriteLine("Test Passed");
        return 100;
    }
}


