// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//This is modeled after a server executing requests
//which pin some of their newly allocated objects.
using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security;

public class one_pass
{
    public Random r = new Random(request.RandomSeed);

[SecuritySafeCritical]
public one_pass ()
    {

        int n_requests = 1200;
        int allocation_volume = 100000;
        float survival_rate = 0.6f;
        int steady_state_factor = 5;
        request[] requests = new request[n_requests];
        int inst_requests = 0;
        int total_reqs = 0;
        int nreqs_to_steady = 0;
        while (true)
        {
            total_reqs++;
            int i = r.Next (0, n_requests);
            if (requests [i] != null)
            {
                requests [i].retire();
            }
            else
            {
                inst_requests++;
            }
            requests [i] = new request (allocation_volume, survival_rate);

            if (inst_requests == n_requests)
            {
                if (nreqs_to_steady == 0)
                {
                    nreqs_to_steady = total_reqs;
                    Console.WriteLine ("took {0} iteration to reach steady state",
                                       nreqs_to_steady);
                } else if (total_reqs == steady_state_factor*nreqs_to_steady)
                {
                    break;
                }
            }
        }

        for (int i = 0; i < n_requests; i++)
        {
            requests[i].retire();
        }

    }
}


public class request
{
    Object[] survivors;
    GCHandle pin;
    public Random r = new Random(request.RandomSeed);

    [SecuritySafeCritical]
    public request (int alloc_volume, float surv_fraction)
    {
        survivors = new Object [1 + (int)(alloc_volume*surv_fraction)/100];
        int i = 0;
        int volume = 0;
        //allocate half of the request size.
        while (volume < alloc_volume/2)
        {
            int alloc_surv = r.Next (100, 2000 + 2*i);
            //Console.WriteLine ("alloc_surv {0}", alloc_surv);
            int alloc = (int)(alloc_surv / surv_fraction) - alloc_surv;
            //Console.WriteLine ("alloc {0}", alloc);
            int j = 0;
            while (j < alloc)
            {
                int s = r.Next (10, 200+2*j);

                Object x = new byte [s];
                j+=s;
            }
            survivors [i] = new byte [alloc_surv];
            i++;
            volume += alloc_surv + alloc;
        }
        //allocate one pinned buffer
        pin = GCHandle.Alloc (new byte [100], GCHandleType.Pinned);
        //allocate the rest of the request
        while (volume < alloc_volume)
        {
            int alloc_surv = r.Next (100, 2000 + 2*i);
            //Console.WriteLine ("alloc_surv {0}", alloc_surv);
            int alloc = (int)(alloc_surv / surv_fraction) - alloc_surv;
            //Console.WriteLine ("alloc {0}", alloc);

            survivors [i] = new byte [alloc_surv];

            int j = 0;
            while (j < alloc)
            {
                int s = r.Next (10, 200+2*j);

                Object x = new byte [s];
                j+=s;
            }
            i++;
            volume += alloc_surv + alloc;
        }

    }

    [SecuritySafeCritical]
    public void retire()
    {
        pin.Free();
    }

    public static void Usage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("Fragment <num threads> [random seed]");
    }

    static public int RandomSeed;

    static public int Main (String[] args)
    {

        int numThreads = 0;


        switch (args.Length)
        {
            case 0:
                // use defaults
                numThreads = 4;
                RandomSeed = (int)DateTime.Now.Ticks;
                Console.WriteLine("Using defaults: {0}", numThreads);
                break;
            case 1:
            case 2:
                if (!Int32.TryParse(args[0], out numThreads))
                {
                    goto default;
                }
                if (args.Length==2)
                {
                    if (!Int32.TryParse(args[1], out RandomSeed))
                    {
                        goto default;
                    }
                }
                else
                {
                    RandomSeed = (int)DateTime.Now.Ticks;
                }
                break;
            default:
                Usage();
                return 1;
        }

        Console.WriteLine("Using random seed: {0}", RandomSeed );

        Console.WriteLine("Starting Threads...");
/*        Thread[] threads = new Thread[numThreads];
        for (int i=0; i<threads.Length; i++)
        {
            threads[i] = new Thread(new ThreadStart(delegate{ one_pass r = new one_pass();  }));
            threads[i].Start();
        }

        Console.WriteLine("Joining Threads...");
        for (int i=0; i<threads.Length; i++)
        {
            threads[i].Join();
        }
*/
new one_pass();
        Console.WriteLine("Test Passed");
        return 100;
    }
}


