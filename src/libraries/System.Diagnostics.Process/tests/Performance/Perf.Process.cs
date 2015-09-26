// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Xunit.Performance;
using System.IO;

namespace System.Diagnostics.ProcessTests
{
    public class Perf_Process : ProcessTestBase
    {
        [Benchmark]
        public void Kill()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (Process proc = CreateProcessInfinite())
                {
                    proc.Start();
                    using (iteration.StartMeasurement())
                        proc.Kill();
                }
            }
        }

        [Benchmark]
        public void GetProcessesByName()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    Process.GetProcessesByName("1");
        }

        [Benchmark]
        public void GetId()
        {
            int id;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (Process proc = CreateProcess())
                {
                    proc.Start();
                    using (iteration.StartMeasurement())
                        id = proc.Id;
                    proc.Kill();
                }
            }
        }

        [Benchmark]
        public void Start()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (Process proc = CreateProcess())
                {
                    using (iteration.StartMeasurement())
                    {
                        proc.Start();
                    }
                    proc.Kill();
                }
        }

        [Benchmark]
        public void GetHasExited()
        {
            bool result;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (Process proc = CreateProcess())
                {
                    proc.Start();
                    using (iteration.StartMeasurement())
                        result = proc.HasExited;
                    proc.Kill();
                }
            }
        }

        [Benchmark]
        public void GetExitCode()
        {
            int result;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (Process proc = CreateProcess())
                {
                    proc.Start();
                    proc.WaitForExit();
                    using (iteration.StartMeasurement())
                        result = proc.ExitCode;
                }
            }
        }

        [Benchmark]
        public void GetStartInfo()
        {
            ProcessStartInfo result;
            foreach (var iteration in Benchmark.Iterations)
                using (Process proc = CreateProcess())
                using (iteration.StartMeasurement())
                    result = proc.StartInfo;
        }

        [Benchmark]
        public void GetStandardOutput()
        {
            StreamReader result;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (Process proc = CreateProcess())
                {
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.Start();
                    
                    using (iteration.StartMeasurement())
                        result = proc.StandardOutput;
                    proc.Kill();
                }
            }
        }
    }
}
