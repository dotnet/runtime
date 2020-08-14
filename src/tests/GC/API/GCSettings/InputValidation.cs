// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime;

//Testing what GC Latency Modes can be set, depending on what is the original GC setting
public class InputValidation
{
    static GCLatencyMode initialMode = GCSettings.LatencyMode;
    static bool server = false;
    static bool nonConcurrent = false;

    public static int Main()
    {
        //Detect on what config we are running
        if (!DetectInitialMode())
            return 25;

        InputValidationTest test = new InputValidationTest(server, nonConcurrent);

        return test.Run();

    }

    static bool DetectInitialMode()
    {
        if (System.Runtime.GCSettings.IsServerGC)
        {
            Console.Write("Server GC ");
            server = true;
        }
        else
        {
            Console.Write("Workstation ");
        }


        if (initialMode == GCLatencyMode.Batch)
        {
            nonConcurrent = true;
            Console.WriteLine("Non Concurrent ");
        }
        else if (initialMode == GCLatencyMode.Interactive)
        {
            Console.WriteLine("Concurrent ");
        }
        else
        {
            Console.WriteLine("Unexpected GC mode");
            return false;
        }
        return true;
    }

    class InputValidationTest
    {

        public List<GCLatencyMode> totalInputs = new List<GCLatencyMode>(new GCLatencyMode[] { GCLatencyMode.Batch, GCLatencyMode.Interactive, GCLatencyMode.LowLatency, GCLatencyMode.SustainedLowLatency });
        public List<GCLatencyMode> validInputs = new List<GCLatencyMode>();
        public List<GCLatencyMode> invalidInputs = new List<GCLatencyMode>();
        public List<GCLatencyMode> outOfRangeInputs = new List<GCLatencyMode>(new GCLatencyMode[] { (GCLatencyMode)(GCLatencyMode.Batch - 1), (GCLatencyMode)(GCLatencyMode.SustainedLowLatency + 1) });

        public InputValidationTest(bool server, bool nonconcurrent)
        {
            //set the valid inputs and invalid inputs 
            if (server)
            {
                invalidInputs.Add(GCLatencyMode.LowLatency);
            }
            if (nonConcurrent)
            {
                invalidInputs.Add(GCLatencyMode.SustainedLowLatency);
            }
            foreach (GCLatencyMode latency in totalInputs)
            {
                if (!invalidInputs.Contains(latency))
                    validInputs.Add(latency);
            }
        }

        public int Run()
        {
            int errorCount = 0;
            Console.WriteLine("Initial mode is {0}", initialMode);
            for (int i = 0; i < validInputs.Count; i++)
            {
                Console.WriteLine("Setting latency mode to {0}", validInputs[i]);
                GCSettings.LatencyMode = validInputs[i];

                if (GCSettings.LatencyMode != validInputs[i])
                {
                    Console.WriteLine("{0} Latency mode doesn't match", validInputs[i]);
                    errorCount++;
                }
                GCSettings.LatencyMode = initialMode;
            }

            for (int i = 0; i < outOfRangeInputs.Count; i++)
            {
                try
                {
                    Console.WriteLine("Setting latency mode to {0}", outOfRangeInputs[i]);
                    GCSettings.LatencyMode = outOfRangeInputs[i];
                    Console.WriteLine("Should not have been able to set latency mode to {0}", invalidInputs[i]);
                    errorCount++;
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("ArgumentOutOfRangeException (expected)");
                }
            }

            GCSettings.LatencyMode = initialMode;
            for (int i = 0; i < invalidInputs.Count; i++)
            {
                Console.WriteLine("Setting latency mode to {0}", invalidInputs[i]);
                GCSettings.LatencyMode = invalidInputs[i];

                if (GCSettings.LatencyMode != initialMode)
                {
                    Console.WriteLine("Latency mode should not have changed to {0}", GCSettings.LatencyMode);
                    errorCount++;
                }
                GCSettings.LatencyMode = initialMode;
            }

            if (errorCount > 0)
            {
                Console.WriteLine("{0} errors", errorCount);
                Console.WriteLine("Test Failed");
                return errorCount;
            }
            Console.WriteLine("Test Passed");
            return 100;

        }

    }

}




