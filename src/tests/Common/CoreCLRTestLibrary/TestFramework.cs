// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

namespace TestLibrary
{
    public static class TestFramework
    {
        private const int DEFAULT_SEED = 20010415;

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
            int seed = DEFAULT_SEED;

            if (Environment.GetEnvironmentVariable("CORECLR_SEED") != null)
            {
                string CORECLR_SEED = Environment.GetEnvironmentVariable("CORECLR_SEED");

                if (!int.TryParse(CORECLR_SEED, out seed))
                {
                    if (string.Equals(CORECLR_SEED, "random", StringComparison.OrdinalIgnoreCase))
                    {
                        seed = new Random().Next();
                    }
                    else
                    {
                        seed = DEFAULT_SEED;
                    }
                }
            }

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
