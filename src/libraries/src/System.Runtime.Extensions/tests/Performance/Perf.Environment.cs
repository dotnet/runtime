// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Microsoft.Xunit.Performance;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class Perf_Environment
    {
        private volatile string str;
        private volatile string[] arr;
        private volatile IDictionary dict;

        [Benchmark(InnerIterationCount = 40000)]
        public void GetEnvironmentVariable()
        {
            PerfUtils utils = new PerfUtils();
            string env = utils.CreateString(15);
            try
            {
                // setup the environment variable so we can read it
                Environment.SetEnvironmentVariable(env, "value");

                // warmup
                for (int i = 0; i < 100; i++)
                {
                    str = Environment.GetEnvironmentVariable(env);
                }

                // read the valid environment variable for the test
                foreach (var iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            str = Environment.GetEnvironmentVariable(env); str = Environment.GetEnvironmentVariable(env); str = Environment.GetEnvironmentVariable(env);
                            str = Environment.GetEnvironmentVariable(env); str = Environment.GetEnvironmentVariable(env); str = Environment.GetEnvironmentVariable(env);
                            str = Environment.GetEnvironmentVariable(env); str = Environment.GetEnvironmentVariable(env); str = Environment.GetEnvironmentVariable(env);
                        }
            }
            finally
            {
                // clear the variable that we set
                Environment.SetEnvironmentVariable(env, null);
            }
        }

        [Benchmark(InnerIterationCount = 40000)]
        public void ExpandEnvironmentVariables()
        {
            PerfUtils utils = new PerfUtils();
            string env = utils.CreateString(15);
            string inputEnv = "%" + env + "%";

            try
            {
                // setup the environment variable so we can read it
                Environment.SetEnvironmentVariable(env, "value");

                // warmup
                for (int i = 0; i < 100; i++)
                {
                    str = Environment.ExpandEnvironmentVariables(inputEnv);
                }

                // read the valid environment variable
                foreach (var iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            str = Environment.ExpandEnvironmentVariables(inputEnv); str = Environment.ExpandEnvironmentVariables(inputEnv);
                            str = Environment.ExpandEnvironmentVariables(inputEnv); str = Environment.ExpandEnvironmentVariables(inputEnv);
                            str = Environment.ExpandEnvironmentVariables(inputEnv); str = Environment.ExpandEnvironmentVariables(inputEnv);
                            str = Environment.ExpandEnvironmentVariables(inputEnv); str = Environment.ExpandEnvironmentVariables(inputEnv);
                            str = Environment.ExpandEnvironmentVariables(inputEnv); str = Environment.ExpandEnvironmentVariables(inputEnv);
                        }
            }
            finally
            {
                // clear the variable that we set
                Environment.SetEnvironmentVariable(env, null);
            }
        }

        [Benchmark(InnerIterationCount = 2000)]
        public void GetEnvironmentVariables()
        {
            PerfUtils utils = new PerfUtils();
            string env = utils.CreateString(15);
            try
            {
                // setup the environment variable so we can read it
                Environment.SetEnvironmentVariable(env, "value");

                // warmup
                for (int i = 0; i < 100; i++)
                {
                    dict = Environment.GetEnvironmentVariables();
                }

                // read the valid environment variable for the test
                foreach (var iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            dict = Environment.GetEnvironmentVariables(); dict = Environment.GetEnvironmentVariables(); dict = Environment.GetEnvironmentVariables();
                            dict = Environment.GetEnvironmentVariables(); dict = Environment.GetEnvironmentVariables(); dict = Environment.GetEnvironmentVariables();
                            dict = Environment.GetEnvironmentVariables(); dict = Environment.GetEnvironmentVariables(); dict = Environment.GetEnvironmentVariables();
                        }
            }
            finally
            {
                // clear the variable that we set
                Environment.SetEnvironmentVariable(env, null);
            }
        }

        [Benchmark(InnerIterationCount = 20000)]
        [InlineData(Environment.SpecialFolder.System, Environment.SpecialFolderOption.None)]
        public void GetFolderPath(Environment.SpecialFolder folder, Environment.SpecialFolderOption option)
        {
            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Environment.GetFolderPath(folder, option);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Environment.GetFolderPath(folder, option); str = Environment.GetFolderPath(folder, option); str = Environment.GetFolderPath(folder, option);
                        str = Environment.GetFolderPath(folder, option); str = Environment.GetFolderPath(folder, option); str = Environment.GetFolderPath(folder, option);
                        str = Environment.GetFolderPath(folder, option); str = Environment.GetFolderPath(folder, option); str = Environment.GetFolderPath(folder, option);
                    }
        }

        [Benchmark(InnerIterationCount = 40000)]
        public void GetLogicalDrives()
        {
            // warmup
            for (int i = 0; i < 100; i++)
            {
                arr = Environment.GetLogicalDrives();
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        arr = Environment.GetLogicalDrives(); arr = Environment.GetLogicalDrives(); arr = Environment.GetLogicalDrives();
                        arr = Environment.GetLogicalDrives(); arr = Environment.GetLogicalDrives(); arr = Environment.GetLogicalDrives();
                        arr = Environment.GetLogicalDrives(); arr = Environment.GetLogicalDrives(); arr = Environment.GetLogicalDrives();
                    }
        }
    }
}
