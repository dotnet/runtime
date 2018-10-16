// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class Perf_Regex_Cache
    {
        private const int MaxConcurrency = 4;
        private volatile bool _isMatch;

        private static string[] CreatePatterns(int total, int unique)
        {
            var regexps = new string[total];
            // create: 
            {
                var i = 0;
                for (; i < unique; i++)
                {
                    // "(0+)" "(1+)" ..  "(9+)(9+)(8+)" ..
                    var sb = new StringBuilder();
                    foreach (var c in i.ToString())
                        sb.Append("(" + c + "+)");
                    regexps[i] = sb.ToString();
                }
                for (; i < total; i++) regexps[i] = regexps[i % unique];
            }

            // shuffle:
            const int someSeed = 101;  // seed for reproducability
            var random = new Random(someSeed);
            for (var i = 0; i < total; i++)
            {
                var r = random.Next(i, total);
                var t = regexps[i];
                regexps[i] = regexps[r];
                regexps[r] = t;
            }

            return regexps;
        }

        [Benchmark]
        [MeasureGCAllocations]
        [InlineData(400_000, 7, 15)]         // default size, most common
        [InlineData(400_000, 1, 15)]         // default size, to test LRU
        [InlineData(40_000, 7, 0)]          // cache turned off
        [InlineData(40_000, 1_600, 15)]    // default size, to compare when cache used
        [InlineData(40_000, 1_600, 800)]    // larger size, to test cache is not O(n)
        [InlineData(40_000, 1_600, 3_200)]  // larger size, to test cache always hit
        public void IsMatch(int total, int unique, int cacheSize)
        {
            var cacheSizeOld = Regex.CacheSize;
            string[] patterns = CreatePatterns(total, unique);

            try
            {
                Regex.CacheSize = 0; // clean up cache
                Regex.CacheSize = cacheSize;
                foreach (BenchmarkIteration iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        RunTest(0, total, patterns);
            }
            finally
            {
                Regex.CacheSize = cacheSizeOld;
            }
        }

        private void RunTest(int start, int total, string[] regexps)
        {
            for (var i = 0; i < total; i++)
                _isMatch = Regex.IsMatch("0123456789", regexps[start + i]);
        }

        [Benchmark]
        [MeasureGCAllocations]
        [InlineData(400_000, 7, 15)]         // default size, most common
        [InlineData(400_000, 1, 15)]         // default size, to test LRU
        [InlineData(40_000, 7, 0)]          // cache turned off
        [InlineData(40_000, 1_600, 15)]    // default size, to compare when cache used
        [InlineData(40_000, 1_600, 800)]    // larger size, to test cache is not O(n)
        [InlineData(40_000, 1_600, 3_200)]  // larger size, to test cache always hit
        public async Task IsMatch_Multithreading(int total, int unique, int cacheSize)
        {
            int cacheSizeOld = Regex.CacheSize;
            string[] patterns = CreatePatterns(total, unique);

            try
            {
                Regex.CacheSize = 0; // clean up cache
                Regex.CacheSize = cacheSize;
                foreach (BenchmarkIteration iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        int sliceLength = total / MaxConcurrency;
                        var tasks = new Task[MaxConcurrency];

                        for (int i = 0; i < MaxConcurrency; i++)
                        {
                            int start = i * sliceLength;
                            tasks[i] = Task.Run(() => RunTest(start, sliceLength, patterns));
                        }

                        await Task.WhenAll(tasks);
                    }
                }
            }
            finally
            {
                Regex.CacheSize = cacheSizeOld;
            }
        }
    }
}
