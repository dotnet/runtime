// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Microsoft.Xunit.Performance;
using System.IO;
using System.Collections.Generic;

namespace System.Diagnostics.Tests
{
    public class Perf_Process : ProcessTestBase
    {
        [Benchmark(Skip="Issue 16653")]
        public void Kill()
        {
            const int inneriterations = 500;
            foreach (var iteration in Benchmark.Iterations)
            {
                // Create several processes to test on
                Process[] processes = new Process[inneriterations];
                for (int i = 0; i < inneriterations; i++)
                {
                    processes[i] = CreateProcessLong();
                    processes[i].Start();
                }

                // Begin Testing - Kill all of the processes
                using (iteration.StartMeasurement())
                    for (int i = 0; i < inneriterations; i++)
                        processes[i].Kill();

                // Cleanup the processes
                foreach (Process proc in processes)
                {
                    proc.WaitForExit();
                    proc.Dispose();
                }
            }
        }

        [Benchmark(Skip="Issue 16653")]
        public void GetProcessesByName()
        {
            // To offset a different number of processes on a different machine, I create dummy processes
            // until our baseline number is reached.
            const int baseline = 300;
            int numberOfProcesses = Process.GetProcesses().Length;
            List<Process> processes = new List<Process>();
            while (numberOfProcesses++ <= baseline)
            {
                Process proc = CreateProcess();
                proc.Start();
                processes.Add(proc);
            }

            // Begin testing
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    Process.GetProcessesByName("1"); Process.GetProcessesByName("1"); Process.GetProcessesByName("1");
                    Process.GetProcessesByName("1"); Process.GetProcessesByName("1"); Process.GetProcessesByName("1");
                    Process.GetProcessesByName("1"); Process.GetProcessesByName("1"); Process.GetProcessesByName("1");
                }

            // Cleanup
            foreach (Process proc in processes)
            {
                if (!proc.HasExited)
                    proc.Kill();
                proc.WaitForExit();
                proc.Dispose();
            }
        }

        [Benchmark(Skip="Issue 16653")]
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

                foreach (Process proc in processes)
                {
                    if (!proc.HasExited)
                        proc.Kill();
                    proc.WaitForExit();
                    proc.Dispose();
                }
            }
        }

        [Benchmark(Skip="Issue 16653")]
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
                foreach (Process proc in processes)
                {
                    if (!proc.HasExited)
                        proc.Kill();
                    proc.WaitForExit();
                    proc.Dispose();
                }
            }
        }

        [Benchmark(Skip="Issue 16653")]
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
                foreach (Process proc in processes)
                {
                    if (!proc.HasExited)
                        proc.Kill();
                    proc.WaitForExit();
                    proc.Dispose();
                }
            }
        }

        [Benchmark(Skip="Issue 16653")]
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
                foreach (Process proc in processes)
                {
                    proc.Dispose();
                }
            }
        }

        [Benchmark(Skip="Issue 16653")]
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
                    processes[i].Start();
                }

                // Begin Testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 500; i++)
                        result = processes[i].StartInfo;

                // Cleanup the processes
                foreach (Process proc in processes)
                {
                    if (!proc.HasExited)
                        proc.Kill();
                    proc.WaitForExit();
                    proc.Dispose();
                }
            }
        }

        [Benchmark(Skip="Issue 16653")]
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
                foreach (Process proc in processes)
                {
                    if (!proc.HasExited)
                        proc.Kill();
                    proc.WaitForExit();
                    proc.Dispose();
                }
            }
        }
    }
}
