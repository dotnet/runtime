// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Xunit.Performance;
using System.IO;

namespace System.Diagnostics.Tests
{
    public class Perf_Process : ProcessTestBase
    {
        [Benchmark]
        public void Kill()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                // Create several processes to test on
                Process[] processes = new Process[500];
                for (int i = 0; i < 500; i++)
                {
                    processes[i] = CreateProcessInfinite();
                    processes[i].Start();
                }

                // Begin Testing - Kill all of the processes
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 500; i++)
                        processes[i].Kill();

                // Cleanup the processes
                for (int i = 0; i < 500; i++)
                    processes[i].Dispose();
            }
        }

        [Benchmark]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void GetProcessesByName(int innerIterations)
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Process.GetProcessesByName("1"); Process.GetProcessesByName("1"); Process.GetProcessesByName("1");
                        Process.GetProcessesByName("1"); Process.GetProcessesByName("1"); Process.GetProcessesByName("1");
                        Process.GetProcessesByName("1"); Process.GetProcessesByName("1"); Process.GetProcessesByName("1");
                    }
                }
        }

        [Benchmark]
        public void GetId()
        {
            int id;
            foreach (var iteration in Benchmark.Iterations)
            {
                // Create several processes to test on
                Process[] processes = new Process[500];
                for (int i = 0; i < 500; i++)
                {
                    processes[i] = CreateProcess();
                    processes[i].Start();
                }

                // Begin Testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 500; i++)
                        id = processes[i].Id;

                // Cleanup the processes
                for (int i = 0; i < 500; i++)
                    processes[i].Dispose();
            }
        }

        [Benchmark]
        public void Start()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                // Create several processes to test on
                Process[] processes = new Process[500];
                for (int i = 0; i < 500; i++)
                {
                    processes[i] = CreateProcess();
                }

                // Begin Testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 500; i++)
                        processes[i].Start();

                // Cleanup the processes
                for (int i = 0; i < 500; i++)
                    processes[i].Dispose();
            }
        }

        [Benchmark]
        public void GetHasExited()
        {
            bool result;
            foreach (var iteration in Benchmark.Iterations)
            {
                // Create several processes to test on
                Process[] processes = new Process[500];
                for (int i = 0; i < 500; i++)
                {
                    processes[i] = CreateProcess();
                    processes[i].Start();
                }

                // Begin Testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 500; i++)
                        result = processes[i].HasExited;

                // Cleanup the processes
                for (int i = 0; i < 500; i++)
                    processes[i].Dispose();
            }
        }

        [Benchmark]
        public void GetExitCode()
        {
            int result;
            foreach (var iteration in Benchmark.Iterations)
            {
                // Create several processes to test on
                Process[] processes = new Process[500];
                for (int i = 0; i < 500; i++)
                {
                    processes[i] = CreateProcess();
                    processes[i].Start();
                    processes[i].WaitForExit();
                }

                // Begin Testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 500; i++)
                        result = processes[i].ExitCode;

                // Cleanup the processes
                for (int i = 0; i < 500; i++)
                    processes[i].Dispose();
            }
        }

        [Benchmark]
        public void GetStartInfo()
        {
            ProcessStartInfo result;
            foreach (var iteration in Benchmark.Iterations)
            {
                // Create several processes to test on
                Process[] processes = new Process[500];
                for (int i = 0; i < 500; i++)
                {
                    processes[i] = CreateProcess();
                }

                // Begin Testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 500; i++)
                        result = processes[i].StartInfo;

                // Cleanup the processes
                for (int i = 0; i < 500; i++)
                    processes[i].Dispose();
            }
        }

        [Benchmark]
        public void GetStandardOutput()
        {
            const int innerIterations = 200;
            foreach (var iteration in Benchmark.Iterations)
            {
                // Create several processes to test on
                Process[] processes = new Process[innerIterations];

                Func<int> method = () =>
                {
                    for (int j = 0; j < innerIterations; j++)
                        Console.WriteLine("Redirected String");
                    return SuccessExitCode;
                };

                for (int i = 0; i < innerIterations; i++)
                {
                    processes[i] = CreateProcess(method);
                    processes[i].StartInfo.RedirectStandardOutput = true;
                    processes[i].Start();
                }

                // Begin Testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                        processes[i].StandardOutput.ReadToEnd();

                // Cleanup the processes
                for (int i = 0; i < innerIterations; i++)
                    processes[i].Dispose();
            }
        }
    }
}
