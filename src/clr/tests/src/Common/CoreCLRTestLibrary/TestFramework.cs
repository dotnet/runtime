// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace TestLibrary
{
    public static class TestFramework
    {
        public static void LogInformation(string str)
        {
            Logging.WriteLine(str);
        }
        public static void LogVerbose(string str)
        {
            if (Utilities.Verbose) Logging.WriteLine(str);
        }
        public static void LogError(string id, string msg)
        {
            Logging.WriteLine("ERROR!!!-" + id + ": " + msg);
        }

        public static void BeginTestCase(string title)
        {
            int seed;
#if WINCORESYS
            seed = 20010415;
#else
            Random rand = new Random();

            if (Env.GetEnvVariable("CORECLR_SEED") != null)
            {
                try
                {
                    seed = int.Parse(Env.GetEnvVariable("CORECLR_SEED"));
                }
                catch (FormatException) { seed = rand.Next(); }
            }
            else
            {
                seed = rand.Next();
            }
#endif

            Generator.m_rand = new Random(seed);

            Logging.WriteLine("Beginning test case " + title + " at " + DateTime.Now);
            Logging.WriteLine("Random seed: " + seed + "; set environment variable CORECLR_SEED to this value to repro");
            Logging.WriteLine();
        }

        public static bool EndTestCase()
        {
            Logging.WriteLine();
            Logging.WriteLine("Ending test case at " + DateTime.Now);
            return true;
        }

        public static void BeginScenario(string name)
        {
            Logging.WriteLine("Beginning scenario: " + name);
        }
    }
}
