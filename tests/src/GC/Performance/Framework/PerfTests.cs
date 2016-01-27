// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GCPerfTestFramework.Metrics;
using Microsoft.Xunit.Performance;
using System.Collections.Generic;

[assembly: CollectGCMetrics]

namespace GCPerfTestFramework
{
    public class PerfTests
    {
        const string ConcurrentGC = "COMPLUS_gcConcurrent";
        const string ServerGC = "COMPLUS_gcServer";

        [Benchmark]
        public void ClientSimulator_Concurrent()
        {
            var exe = ProcessFactory.ProbeForFile("GCSimulator.exe");
            var env = new Dictionary<string, string>()
            {
                [ConcurrentGC] = "1"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, "-i 100", env);
                }
            }
        }

        [Benchmark]
        public void ClientSimulator_Server()
        {
            var exe = ProcessFactory.ProbeForFile("GCSimulator.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "1"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, "-i 100", env);
                }
            }
        }

        [Benchmark]
        public void ClientSimulator_Server_One_Thread()
        {
            var exe = ProcessFactory.ProbeForFile("GCSimulator.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "1"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, "-i 10 -notimer -dp 0.0", env);
                }
            }
        }

        [Benchmark]
        public void ClientSimulator_Server_Two_Threads()
        {
            var exe = ProcessFactory.ProbeForFile("GCSimulator.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "1"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, "-i 10 -notimer -dp 0.0 -t 2", env);
                }
            }
        }

        [Benchmark]
        public void ClientSimulator_Server_Four_Threads()
        {
            var exe = ProcessFactory.ProbeForFile("GCSimulator.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "1"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, "-i 10 -notimer -dp 0.0 -t 4", env);
                }
            }
        }


        [Benchmark]
        public void LargeStringConcat()
        {
            var exe = ProcessFactory.ProbeForFile("LargeStrings.exe");
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe);
                }
            }
        }

        [Benchmark]
        public void LargeStringConcat_Server()
        {
            var exe = ProcessFactory.ProbeForFile("LargeStrings.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "1"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, environmentVariables: env);
                }
            }
        }

        [Benchmark]
        public void LargeStringConcat_Workstation()
        {
            var exe = ProcessFactory.ProbeForFile("LargeStrings.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "0"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, environmentVariables: env);
                }
            }
        }

        [Benchmark]
        public void MidLife_Concurrent()
        {
            var exe = ProcessFactory.ProbeForFile("MidLife.exe");
            var env = new Dictionary<string, string>()
            {
                [ConcurrentGC] = "1"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, environmentVariables: env);
                }
            }
        }

        [Benchmark]
        public void MidLife_Server()
        {
            var exe = ProcessFactory.ProbeForFile("MidLife.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "1"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, environmentVariables: env);
                }
            }
        }

        [Benchmark]
        public void MidLife_Workstation()
        {
            var exe = ProcessFactory.ProbeForFile("MidLife.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "0"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, environmentVariables: env);
                }
            }
        }

        [Benchmark]
        public void ConcurrentSpin()
        {
            var exe = ProcessFactory.ProbeForFile("ConcurrentSpin.exe");
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe);
                }
            }
        }

        [Benchmark]
        public void ConcurrentSpin_Server()
        {
            var exe = ProcessFactory.ProbeForFile("ConcurrentSpin.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "1"
            };
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, environmentVariables: env);
                }
            }
        }

        [Benchmark]
        public void ConcurrentSpin_Server_NonConcurrent()
        {
            var exe = ProcessFactory.ProbeForFile("ConcurrentSpin.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "1",
                [ConcurrentGC] = "0"
            };

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, environmentVariables: env);
                }
            }
        }

        [Benchmark]
        public void ConcurrentSpin_Workstation()
        {
            var exe = ProcessFactory.ProbeForFile("ConcurrentSpin.exe");
            var env = new Dictionary<string, string>()
            {
                [ServerGC] = "0",
            };

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    ProcessFactory.LaunchProcess(exe, environmentVariables: env);
                }
            }
        }
    }
}
