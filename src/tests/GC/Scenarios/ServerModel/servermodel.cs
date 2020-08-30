// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//TBD: memory load (external or in process?) 
//     multiple threads

using System;

namespace ServerSimulator
{

    /// <summary>
    /// This is modeled after a server executing requests
    /// which pin some of their newly allocated objects. 
    /// </summary>
    internal static class ServerSimulator
    {

        internal static Parameters Params;
        internal static Random Rand;

        // message displayed to user when passed an incorrect parameter or "/?"
        public static int Usage()
        {
            Console.WriteLine();
            Console.WriteLine("SERVERSIMULATOR: [/randomseed:int] [/numpasses:int]");            
            Console.WriteLine("\t\t [/cachereplacementrate:float] [/survivalrate:float]");
            Console.WriteLine("\t\t [/finalizablerate:float] [/allocationvolume:int]");
            Console.WriteLine("\t\t [/cachesize:int] [/steadystatefactor:int]");
            Console.WriteLine("\t\t [/numrequests:int] [/staticdatavolume:int]");
            Console.WriteLine("\t\t [/fifocache:True|False] [/pinning:True|False]");
            Console.WriteLine();
            Console.WriteLine("Parameters not passed on the command line are read from the application config file.");
            return 1;
        }

        // entrypoint
        public static int Main(String[] args)
        {
            Params = new Parameters();

            if (args.Length > 0)
            {
                // check command-line params
                if (!Params.GetParams(args))
                {
                    return Usage();
                }              
            }

            int seed = 0;
            if (Params.RandomSeed != 0)
            {                
                // we were passed a random seed
                seed = Params.RandomSeed;
            }
            else
            {
                // default to current time
                seed = (int)DateTime.Now.Ticks;
            }

            Rand = new Random(seed);

            Console.WriteLine("Using {0} as random seed", seed);

            for (int n = 0; n != Params.NumPasses; n++)
            {
                Server server = new Server();
                server.OnePass();
                Console.WriteLine("Pass {0} done", n);
            }

            return 100;
            
        }
    }

}

