// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ServerSimulator
{
    /// <summary>
    /// This class simulates the server, which allocates static data for its lifetime,
    /// fills the cache, creates and retires requests
    /// </summary>
    internal sealed class Server
    {
        private Object[] static_data;

        public Server()
        {
            int volume = 0;

            // static_data size in Mb
            static_data = new Object[1 + ServerSimulator.Params.StaticDataVolume * 1000];

            for (int i = 0; volume < static_data.Length; i++)
            {
                int alloc_surv = ServerSimulator.Rand.Next(1000, 20000 + 2 * i);
                static_data[i] = new byte[alloc_surv];
                volume += alloc_surv / 1000;
            }

        }

        // creates requests until we reach the steady state with a full cache
        public void OnePass()
        {
            int inst_requests = 0;
            int total_reqs = 0;
            int nreqs_to_steady = 0;
            Request[] requests = new Request[ServerSimulator.Params.NumRequests];
            Cache cache = new Cache(ServerSimulator.Params.FifoCache);
            int start = Environment.TickCount;
            int split = start;

            while (true)
            {
                total_reqs++;

                int i = ServerSimulator.Rand.Next(0, ServerSimulator.Params.NumRequests);
                if (requests[i] != null)
                {
                    requests[i].Retire();
                }
                else
                {
                    inst_requests++;
                }

                // make every nth request finalizable
                if (total_reqs % (1 / ServerSimulator.Params.FinalizableRate) == 0)
                {
                    requests[i] = new FinalizableRequest();
                }
                else
                {
                    requests[i] = new Request();
                }

                cache.Encache();

                int stop = Environment.TickCount;

                if ((stop - split) > 4000)
                {
                    Console.WriteLine("{0} reqs/sec", (total_reqs * 1000) / (stop - start));
                    split = stop;
                }

                if (cache.IsFull && (inst_requests == ServerSimulator.Params.NumRequests))
                {
                    if (nreqs_to_steady == 0)
                    {
                        nreqs_to_steady = total_reqs;
                        Console.WriteLine("took {0} iteration to reach steady state", nreqs_to_steady);
                    }
                    else if (total_reqs == ServerSimulator.Params.SteadyStateFactor * nreqs_to_steady)
                    {
                        break;
                    }
                }
            }

            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i] != null)
                {
                    requests[i].Retire();
                }
            }

            int fstop = Environment.TickCount;
            Console.WriteLine("{0} reqs/sec", (total_reqs * 1000) / (fstop - start));

            //cleanup
            static_data = null;
            cache.Clear();

        }
    }
}
