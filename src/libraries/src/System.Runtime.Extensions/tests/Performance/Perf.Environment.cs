// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System.Runtime.InteropServices;

namespace System.Tests
{
    public class Perf_Environment
    {
        [Benchmark]
        public void GetEnvironmentVariable()
        {
            PerfUtils utils = new PerfUtils();
            string env = utils.CreateString(15);
            try
            {
                // setup the environment variable so we can read it
                Environment.SetEnvironmentVariable(env, "value");

                // read the valid environment variable for the test
                foreach (var iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        for (int i = 0; i < 40000; i++)
                        {
                            Environment.GetEnvironmentVariable(env); Environment.GetEnvironmentVariable(env); Environment.GetEnvironmentVariable(env);
                            Environment.GetEnvironmentVariable(env); Environment.GetEnvironmentVariable(env); Environment.GetEnvironmentVariable(env);
                            Environment.GetEnvironmentVariable(env); Environment.GetEnvironmentVariable(env); Environment.GetEnvironmentVariable(env);
                        }
            }
            finally
            {
                // clear the variable that we set
                Environment.SetEnvironmentVariable(env, null);
            }
        }

        [Benchmark]
        public void ExpandEnvironmentVariables()
        {
            PerfUtils utils = new PerfUtils();
            string env = utils.CreateString(15);
            string inputEnv = "%" + env + "%";
            try
            {
                // setup the environment variable so we can read it
                Environment.SetEnvironmentVariable(env, "value");

                // read the valid environment variable
                foreach (var iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        for (int i = 0; i < 40000; i++)
                        {
                            Environment.ExpandEnvironmentVariables(inputEnv); Environment.ExpandEnvironmentVariables(inputEnv);
                            Environment.ExpandEnvironmentVariables(inputEnv); Environment.ExpandEnvironmentVariables(inputEnv);
                            Environment.ExpandEnvironmentVariables(inputEnv); Environment.ExpandEnvironmentVariables(inputEnv);
                            Environment.ExpandEnvironmentVariables(inputEnv); Environment.ExpandEnvironmentVariables(inputEnv);
                            Environment.ExpandEnvironmentVariables(inputEnv); Environment.ExpandEnvironmentVariables(inputEnv);
                        }
            }
            finally
            {
                // clear the variable that we set
                Environment.SetEnvironmentVariable(env, null);
            }
        }
    }
}
