// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.IO.Tests
{
    public class Perf_StreamWriter
    {
        [Benchmark(InnerIterationCount = 20000)]
        public void Write_Format_OneArg()
        {
            // Don't want MemoryStream to be expandable to avoid measuring resize
            MemoryStream memory = new MemoryStream(new byte[1024 * 1024]);
            StreamWriter writer = new StreamWriter(memory);

            foreach (var iteration in Benchmark.Iterations)
            {
                memory.Position = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        writer.Write("Performance test iteration {0}", i);
                    }
                }
                writer.Flush();
            }
        }

        [Benchmark(InnerIterationCount = 20000)]
        public void Write_Format_OneNoBoxArg()
        {
            // Don't want MemoryStream to be expandable to avoid measuring resize
            MemoryStream memory = new MemoryStream(new byte[1024 * 1024]);
            StreamWriter writer = new StreamWriter(memory);

            foreach (var iteration in Benchmark.Iterations)
            {
                memory.Position = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        writer.Write("Performance test: {0}", nameof(Write_Format_OneNoBoxArg));
                    }
                }
                writer.Flush();
            }
        }

        [Benchmark(InnerIterationCount = 20000)]
        public void WriteLine_Format_OneArg()
        {
            // Don't want MemoryStream to be expandable to avoid measuring resize
            MemoryStream memory = new MemoryStream(new byte[1024 * 1024]);
            StreamWriter writer = new StreamWriter(memory);

            foreach (var iteration in Benchmark.Iterations)
            {
                memory.Position = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        writer.WriteLine("Performance test iteration {0}", i);
                    }
                }
                writer.Flush();
            }
        }

        [Benchmark(InnerIterationCount = 20000)]
        public void Write_Format_Params()
        {
            // Don't want MemoryStream to be expandable to avoid measuring resize
            MemoryStream memory = new MemoryStream(new byte[1024 * 1024]);
            StreamWriter writer = new StreamWriter(memory);

            foreach (var iteration in Benchmark.Iterations)
            {
                memory.Position = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        writer.Write("Performance test iteration {0}", i, i, i, i, i);
                    }
                }
                writer.Flush();
            }
        }

        [Benchmark(InnerIterationCount = 20000)]
        public void WriteLine_Format_Params()
        {
            // Don't want MemoryStream to be expandable to avoid measuring resize
            MemoryStream memory = new MemoryStream(new byte[1024 * 1024]);
            StreamWriter writer = new StreamWriter(memory);

            foreach (var iteration in Benchmark.Iterations)
            {
                memory.Position = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        writer.WriteLine("Performance test iteration {0}", i, i, i, i, i);
                    }
                }
                writer.Flush();
            }
        }
    }
}
