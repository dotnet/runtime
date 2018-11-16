// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Tests
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

        public const int NUM_ITERS_CONCAT = 1000;
        public const int NUM_ITERS_APPEND = 1000;
        public const int NUM_ITERS_TOSTRING = 1000;

        public static string s1 = "12345";
        public static string s2 = "1234567890";
        public static string s3 = "1234567890abcde";
        public static string s4 = "1234567890abcdefghij";
        public static string s5 = "1234567890abcdefghijklmno";
        public static string s6 = "1234567890abcdefghijklmnopqrst";
        public static string s7 = "1234567890abcdefghijklmnopqrstuvwxy";
        public static string s8 = "1234567890abcdefghijklmnopqrstuvwxyzABCD";
        public static string s9 = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHI";
        public static string s10 = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMN";

        [Benchmark]
        public static void StringConcat()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                string str = "";

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < NUM_ITERS_CONCAT; j++)
                        str += s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10;
                }
            }
        }

        [Benchmark]
        public static void StringBuilderAppend()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                StringBuilder sb = new StringBuilder();

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < NUM_ITERS_APPEND; j++)
                    {
                        sb.Append(s1);
                        sb.Append(s2);
                        sb.Append(s3);
                        sb.Append(s4);
                        sb.Append(s5);
                        sb.Append(s6);
                        sb.Append(s7);
                        sb.Append(s8);
                        sb.Append(s9);
                        sb.Append(s10);
                    }
                }
            }
        }

        [Benchmark]
        public static void StringBuilderToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int j = 0; j < NUM_ITERS_TOSTRING; j++)
            {
                sb.Append(s1);
                sb.Append(s2);
                sb.Append(s3);
                sb.Append(s4);
                sb.Append(s5);
                sb.Append(s6);
                sb.Append(s7);
                sb.Append(s8);
                sb.Append(s9);
                sb.Append(s10);
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    sb.ToString();
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        [InlineData(20)]
        [InlineData(200)]
        [InlineData(1000)]
        public void AppendMemoryAsMemory(int length)
        {
            PerfUtils utils = new PerfUtils();
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup - Create a string of the specified length
                string builtString = utils.CreateString(length);
                ReadOnlyMemory<char> memory = builtString.AsMemory();
                StringBuilder empty = new StringBuilder();

                // Actual perf testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        empty.Append(memory); // Appends a string of length "length" to an increasingly large StringBuilder
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        [InlineData(20)]
        [InlineData(200)]
        [InlineData(1000)]
        public void AppendMemoryAsObject(int length)
        {
            PerfUtils utils = new PerfUtils();
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup - Create a string of the specified length
                string builtString = utils.CreateString(length);
                object memoryObject = builtString.AsMemory(); // deliberately uses object to force use of memory.ToString() for comparison to AppendAsReadOnlyMemory
                StringBuilder empty = new StringBuilder();

                // Actual perf testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        empty.Append(memoryObject); 
            }
        }
    }
}
