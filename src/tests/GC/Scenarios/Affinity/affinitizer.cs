// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Diagnostics;

public class Affinitizer
{

    public static void Usage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("affinitizer.exe <num procs (0 for random)> <\"assembly.exe arg list\"> [random seed]");
    }

    public static int Main(string[] args)
    {

        if ((args.Length < 2) || (args.Length > 3))
        {
            Usage();
            return 0;
        }

        int numProcessors = Environment.ProcessorCount;

        // get affinity
        IntPtr affinity = IntPtr.Zero;
        int a = 0;
        if ( (!int.TryParse(args[0], out a)) || (a < 0) )
        {
            Usage();
            return 0;        
        }
        // cap the number of procs to the max on the machine
        affinity = new IntPtr(Math.Min(a, numProcessors));

        // get process name and args
        string processName = null;
        string processArgs = null;
        int firstSpaceIndex = args[1].Trim().IndexOf(' ');
        if (firstSpaceIndex < 0)
        {
            // no args
            processName = args[1];
        }
        else
        {
            processName = args[1].Substring(0, firstSpaceIndex);
            processArgs = args[1].Substring(firstSpaceIndex + 1);
        }

        // get random seed
        int seed = 0;
        if (args.Length == 3)
        {
            if (!int.TryParse(args[2], out seed))
            {
                Usage();
                return 0;
            }
        }
        else
        {
            seed = (int)DateTime.Now.Ticks;
        }
        
        Console.WriteLine("Running on a {0}-processor machine", numProcessors);
        
        return RunTest(affinity, processName, processArgs, seed);
    }
    

    public static int RunTest(IntPtr affinity, string processName, string processArgs, int seed)
    {

        // run the test
        Random rand = null;

        Process p = Process.Start(processName, processArgs);
        
        // cannot set the affinity before the process starts in managed code
        // This code executes so quickly that the GC heaps have not yet been initialized,
        // so it works.
        if (affinity != IntPtr.Zero)
        {
            // set affinity to (2^n)-1, where n=affinity
            int newAffinity = (int)Math.Pow(2, affinity.ToInt32())-1;
            p.ProcessorAffinity = new IntPtr(newAffinity);
            Console.WriteLine("Affinitizing to {0}", newAffinity); 
        }
        else
        {
            rand = new Random(seed);
            Console.WriteLine("Using random seed: {0}", seed);
        }

        while (!p.HasExited)
        {
            // change affinity randomly every 5 seconds
            Thread.Sleep(5000);
            if (affinity == IntPtr.Zero)
            {
                try
                {
                    // randomly change the affinity between 1 and (2^n)-1, where n=numProcessors
                    int newAffinity = rand.Next(1, (int)Math.Pow(2, Environment.ProcessorCount)-1);
                    p.ProcessorAffinity = new IntPtr(newAffinity);
                    Console.WriteLine("Affinitizing to {0}", newAffinity);
                }
                // we couldn't set the affinity, so just exit
                catch (InvalidOperationException)
                {
                    break;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    break;
                }
            }

        }

        Console.WriteLine("Exiting with exit code {0}", p.ExitCode);
        return p.ExitCode;
    }
}
