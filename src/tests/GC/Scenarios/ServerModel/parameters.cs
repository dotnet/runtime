// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ServerSimulator.Properties;

namespace ServerSimulator
{

    /// <summary>
    /// This class validates and stores parameters passed in from the command-line or config file
    /// </summary>
    internal sealed class Parameters
    {
        //     the number of requests in flight
        private int numRequests = 200;
        public int NumRequests
        {
            get { return numRequests; }
            set
            {
                if (value > 0)
                {
                    numRequests = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", "NumRequests must be > 0");
                }
            }
        }

        //     the fraction of requests that are finalizable
        private float finalizableRate = 0;
        public float FinalizableRate
        {
            get { return finalizableRate; }
            set
            {
                if ((value >= 0) && (value <= 1))
                {
                    finalizableRate = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", "FinalizableRate must be >=0 and <=1");
                }
            }
        }

        //     the replacement rate of the cache for each request
        private float cacheReplacementRate = 0.01f;
        public float CacheReplacementRate
        {
            get { return cacheReplacementRate; }
            set
            {
                if ((value > 0) && (value <= 1))
                {
                    cacheReplacementRate = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", "CacheReplacementRate must be >0 and <=1");
                }
            }
        }

        //     the fraction that survives for the life of the request
        private float survivalRate = 0.9f;
        public float SurvivalRate
        {
            get { return survivalRate; }
            set
            {
                if ((value > 0) && (value <= 1))
                {
                    survivalRate = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", "SurvivalRate must be >0 and <=1");
                }
            }
        }

        //     the number of times to loop (-1 for infinite)
        private int numPasses = -1;
        public int NumPasses
        {
            get { return numPasses; }
            set { numPasses = value; }
        }

        // the random seed for reproducibility
        private int randomSeed = 0;
        public int RandomSeed
        {
            get { return randomSeed; }
            set { randomSeed = value; }
        }

        //     the total allocation per request in byte
        private int allocationVolume = 100000;
        public int AllocationVolume
        {
            get { return allocationVolume; }
            set
            {
                if (value > 0)
                {
                    allocationVolume = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", "AllocationVolume must be >0");
                }
            }
        }

        //     the cache size in bytes
        private int cacheSize = 1024*1024*100;
        public int CacheSize
        {
            get { return cacheSize; }
            set
            {
                if (value > 0)
                {
                    cacheSize = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", "CacheSize must be > 0");
                }
            }
        }

        //     the number of times requests to executes after steady state is achieved.
        //     if it took 300 reqs to achieve steady state, then perform 300*steady_state_factor requests
        private int steadyStateFactor = 20;
        public int SteadyStateFactor
        {
            get { return steadyStateFactor; }
            set
            {
                if (value > 0)
                {
                    steadyStateFactor = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", "SteadyStateFactor must be > 0");
                }
            }
        }

        //     the amount of non changing static data in MB
        private int staticDataVolume = 500;
        public int StaticDataVolume
        {
            get { return staticDataVolume; }
            set
            {
                if (value >= 0)
                {
                    staticDataVolume = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", "StaticDataVolume must be >=0");
                }
            }
        }

        //      use fifo cache replacement strategy instead of random
        private bool fifoCache = false;
        public bool FifoCache
        {
            get { return fifoCache; }
            set { fifoCache = value; }
        }

        //      pin all requests
        private bool pinning = false;
        public bool Pinning
        {
            get { return pinning; }
            set { pinning = value; }
        }

        public Parameters()
        {
            numRequests = Settings.Default.NumRequests;
            finalizableRate = Settings.Default.FinalizableRate;
            cacheReplacementRate = Settings.Default.CacheReplacementRate;
            survivalRate = Settings.Default.SurvivalRate;
            numPasses = Settings.Default.NumPasses;
            randomSeed = Settings.Default.RandomSeed;
            allocationVolume = Settings.Default.AllocationVolume;
            cacheSize = Settings.Default.CacheSize;
            steadyStateFactor = Settings.Default.SteadyStateFactor;
            staticDataVolume = Settings.Default.StaticDataVolume;
            fifoCache = Settings.Default.FifoCache;
            pinning = Settings.Default.Pinning;
        }

        // gets and stores command-line parameters
        public bool GetParams(string[] args)
        {
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string str = args[i].ToLower();

                    if (str.StartsWith("/randomseed:"))
                    {
                        int randomSeed = 0;
                        if (!Int32.TryParse(str.Substring("/randomseed:".Length), out randomSeed))
                        {
                            Console.WriteLine("Invalid randomseed");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("randomseed: {0}", randomSeed);
                            RandomSeed = randomSeed;
                        }
                    }
                    else if (str.StartsWith("/finalizablerate:"))
                    {
                        float finalizableRate = 0;
                        if (!float.TryParse(str.Substring("/finalizablerate:".Length), out finalizableRate))
                        {
                            Console.WriteLine("Invalid finalizablerate");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("finalizablerate: {0}", finalizableRate);
                            this.finalizableRate = finalizableRate;
                        }
                    }
                    else if (str.StartsWith("/cachereplacementrate:"))
                    {
                        float cacheReplacementRate = 0;
                        if (!float.TryParse(str.Substring("/cachereplacementrate:".Length), out cacheReplacementRate))
                        {
                            Console.WriteLine("Invalid cachereplacementrate");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("cachereplacementrate: {0}", cacheReplacementRate);
                            this.cacheReplacementRate = cacheReplacementRate;
                        }
                    }
                    else if (str.StartsWith("/survivalrate:"))
                    {
                        float survivalRate = 0;
                        if (!float.TryParse(str.Substring("/survivalrate:".Length), out survivalRate))
                        {
                            Console.WriteLine("Invalid survivalrate");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("survivalrate: {0}", survivalRate);
                            this.survivalRate = survivalRate;
                        }
                    }
                    else if (str.StartsWith("/numpasses:"))
                    {
                        int numPasses = 0;
                        if (!Int32.TryParse(str.Substring("/numpasses:".Length), out numPasses))
                        {
                            Console.WriteLine("Invalid numpasses");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("numpasses: {0}", numPasses);
                            this.numPasses = numPasses;
                        }
                    }
                    else if (str.StartsWith("/allocationvolume:"))
                    {
                        int allocationVolume = 0;
                        if (!Int32.TryParse(str.Substring("/allocationvolume:".Length), out allocationVolume))
                        {
                            Console.WriteLine("Invalid allocationvolume");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("allocationvolume: {0}", allocationVolume);
                            this.allocationVolume = allocationVolume;
                        }
                    }
                    else if (str.StartsWith("/cachesize:"))
                    {
                        int cacheSize = 0;
                        if (!Int32.TryParse(str.Substring("/cachesize:".Length), out cacheSize))
                        {
                            Console.WriteLine("Invalid cachesize");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("cachesize: {0}", cacheSize);
                            this.cacheSize = cacheSize;
                        }
                    }
                    else if (str.StartsWith("/steadystatefactor:"))
                    {
                        int steadyStateFactor = 0;
                        if (!Int32.TryParse(str.Substring("/steadystatefactor:".Length), out steadyStateFactor))
                        {
                            Console.WriteLine("Invalid steadystatefactor");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("steadystatefactor: {0}", steadyStateFactor);
                            this.steadyStateFactor = steadyStateFactor;
                        }
                    }
                    else if (str.StartsWith("/numrequests:"))
                    {
                        int numRequests = 0;
                        if (!Int32.TryParse(str.Substring("/numrequests:".Length), out numRequests))
                        {
                            Console.WriteLine("Invalid numrequests");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("numrequests: {0}", numRequests);
                            this.numRequests = numRequests;
                        }
                    }
                    else if (str.StartsWith("/staticdatavolume:"))
                    {
                        int staticDataVolume = 0;
                        if (!Int32.TryParse(str.Substring("/staticdatavolume:".Length), out staticDataVolume))
                        {
                            Console.WriteLine("Invalid staticdatavolume");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("staticdatavolume: {0}", staticDataVolume);
                            this.staticDataVolume = staticDataVolume;
                        }
                    }
                    else if (str.StartsWith("/fifocache:"))
                    {
                        int fifoCache = 0;
                        if (!Int32.TryParse(str.Substring("/fifocache:".Length), out fifoCache))
                        {
                            Console.WriteLine("Invalid fifocache");
                            return false;
                        }
                        else
                        {
                            // Console.WriteLine("fifocache: {0}", fifoCache);
                            if (fifoCache == 0)
                            {
                                this.fifoCache = false;
                            }
                            else
                            {
                                this.fifoCache = true;
                            }
                        }
                    }
                    else if (str.StartsWith("/pinning:"))
                    {
                        int pinning = 0;
                        if (!Int32.TryParse(str.Substring("/pinning:".Length), out pinning))
                        {
                            Console.WriteLine("Invalid pinning");
                            return false;
                        }
                        else
                        {
                            //Console.WriteLine("pinning: {0}", pinning);
                            if (pinning == 0)
                            {
                                this.pinning = false;
                            }
                            else
                            {
                                this.pinning = true;
                            }
                        }
                    }
                    else if (str.Equals("/?"))
                    {
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("Invalid parameter");
                        return false;
                    }

                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }
    }
}
