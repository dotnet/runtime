// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//This is modeled after a server executing requests
//which pin some of their newly allocated objects. 
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Runtime;

class request
{
    Object[] survivors;
    GCHandle pin;
    static Random r = new Random(1234);
    public request(int alloc_volume, float surv_fraction)
    {
        survivors = new Object[1 + (int)(alloc_volume * surv_fraction) / 1000];
        int i = 0;
        int volume = 0;
        //allocate half of the request size. 
        while (volume < alloc_volume / 2)
        {
            int alloc_surv = r.Next(1000, 2000 + 2 * i);
            
            int alloc = (int)(alloc_surv / surv_fraction) - alloc_surv;
            
            int j = 0;
            while (j < alloc)
            {
                int s = r.Next(100, 200 + 2 * j);

                Object x = new byte[s];
                j += s;
            }
            survivors[i] = new byte[alloc_surv];
            i++;
            volume += alloc_surv + alloc;
        }
        //allocate one pinned buffer
        pin = GCHandle.Alloc (new byte [100], GCHandleType.Pinned);
        //allocate the rest of the request
        while (volume < alloc_volume)
        {
            int alloc_surv = r.Next(1000, 2000 + 2 * i);          
            int alloc = (int)(alloc_surv / surv_fraction) - alloc_surv;          
            int j = 0;
            while (j < alloc)
            {
                int s = r.Next(100, 200 + 2 * j);

                Object x = new byte[s];
                j += s;
            }
            survivors[i] = new byte[alloc_surv];
            i++;
            volume += alloc_surv + alloc;
        }

    }
    public void retire()
    {
        pin.Free();
    }

    static public int Main(String[] args)
    {
        int n_requests = 600;
        int allocation_volume = 100000; // 1 mil
        float survival_rate = 0.6f;
        request[] requests = new request[n_requests];
        int loop = 0;
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        double total_elapsed_ms = 0;

        //loop for about 3 min
        while (total_elapsed_ms < 3 * 60 * 1000)
        {
            for (loop = 0; loop < (n_requests * 100); loop++)
            {
                int i = r.Next(0, n_requests);
                if (requests[i] != null)
                {
                    requests[i].retire();
                }
                requests[i] = new request(allocation_volume, survival_rate);
                
            }

            Console.Write(" Cleaning up-------");
            Console.WriteLine("gen0: {0}, gen1: {1}; gen2: {2}, heap size: {3:N0} bytes",
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2),
                GC.GetTotalMemory(false));


            for (loop = 0; loop < n_requests; loop++)
            {
                if (requests[loop] != null)
                {
                    requests[loop].retire();
                    requests[loop] = null;
                }
            }

            total_elapsed_ms = stopwatch.Elapsed.TotalMilliseconds;
        }

        return 100;
    }
}


