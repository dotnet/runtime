// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
{
    public class Perf_StringBuilder
    {
        [Benchmark]
        public void ctor()
        {
            StringBuilder builder;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        builder = new StringBuilder(); builder = new StringBuilder(); builder = new StringBuilder();
                        builder = new StringBuilder(); builder = new StringBuilder(); builder = new StringBuilder();
                        builder = new StringBuilder(); builder = new StringBuilder(); builder = new StringBuilder();
                    }
        }

        [Benchmark]
        [InlineData(100)]
        [InlineData(1000)]
        public void ctor_string(int length)
        {
            PerfUtils utils = new PerfUtils();
            string input = utils.CreateString(length);
            StringBuilder builder;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        builder = new StringBuilder(input); builder = new StringBuilder(input); builder = new StringBuilder(input);
                        builder = new StringBuilder(input); builder = new StringBuilder(input); builder = new StringBuilder(input);
                        builder = new StringBuilder(input); builder = new StringBuilder(input); builder = new StringBuilder(input);
                    }
        }

        [Benchmark]
        [InlineData(0)]
        [InlineData(200)]
        public void Append(int length)
        {
            PerfUtils utils = new PerfUtils();
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup - Create a string of the specified length
                string builtString = utils.CreateString(length);
                StringBuilder empty = new StringBuilder();

                // Actual perf testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        empty.Append(builtString); // Appends a string of length "length" to an increasingly large StringBuilder
            }
        }
    }
}
