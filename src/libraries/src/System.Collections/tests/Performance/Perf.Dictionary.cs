// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Collections.Tests
{
    public class Perf_Dictionary
    {
        /// <summary>
        /// Creates a Dictionary of int-int with the specified number of pairs
        /// </summary>
        public static Dictionary<int, int> CreateDictionary(int size)
        {
            Random rand = new Random(837322);
            Dictionary<int, int> dict = new Dictionary<int, int>();
            while (dict.Count < size)
            {
                int key = rand.Next(500000, int.MaxValue);
                if (!dict.ContainsKey(key))
                    dict.Add(key, 0);
            }
           return dict;
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Add(int size)
        {
            Dictionary<int, int> dict = CreateDictionary(size);
            foreach (var iteration in Benchmark.Iterations)
            {
                Dictionary<int, int> copyDict = new Dictionary<int, int>(dict);
                using (iteration.StartMeasurement())
                    for (int i = 0; i <= 20000; i++)
                    {
                        copyDict.Add(i * 10 + 1, 0); copyDict.Add(i * 10 + 2, 0); copyDict.Add(i * 10 + 3, 0);
                        copyDict.Add(i * 10 + 4, 0); copyDict.Add(i * 10 + 5, 0); copyDict.Add(i * 10 + 6, 0);
                        copyDict.Add(i * 10 + 7, 0); copyDict.Add(i * 10 + 8, 0); copyDict.Add(i * 10 + 9, 0);
                    }
            }
        }

        [Benchmark]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i <= 20000; i++)
                    {
                        new Dictionary<int, string>(); new Dictionary<int, string>(); new Dictionary<int, string>();
                        new Dictionary<int, string>(); new Dictionary<int, string>(); new Dictionary<int, string>();
                        new Dictionary<int, string>(); new Dictionary<int, string>(); new Dictionary<int, string>();
                    }
        }

        [Benchmark]
        [InlineData(0)]
        [InlineData(1024)]
        [InlineData(4096)]
        [InlineData(16384)]
        public void ctor_int(int size)
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i <= 500; i++)
                    {
                        new Dictionary<int, string>(size); new Dictionary<int, string>(size); new Dictionary<int, string>(size);
                        new Dictionary<int, string>(size); new Dictionary<int, string>(size); new Dictionary<int, string>(size);
                        new Dictionary<int, string>(size); new Dictionary<int, string>(size); new Dictionary<int, string>(size);
                    }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void GetItem(int size)
        {
            Dictionary<int, int> dict = CreateDictionary(size);

            // Setup
            int retrieved;
            for (int i = 1; i <= 9; i++)
                dict.Add(i, 0);

            // Actual perf testing
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                    for (int i = 0; i <= 10000; i++)
                    {
                        retrieved = dict[1]; retrieved = dict[2]; retrieved = dict[3];
                        retrieved = dict[4]; retrieved = dict[5]; retrieved = dict[6];
                        retrieved = dict[7]; retrieved = dict[8]; retrieved = dict[9];
                    }
            }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void SetItem(int size)
        {
            Dictionary<int, int> dict = CreateDictionary(size);
            // Setup
            for (int i = 1; i <= 9; i++)
                dict.Add(i, 0);

            // Actual perf testing
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                    for (int i = 0; i <= 10000; i++)
                    {
                        dict[1] = 0; dict[2] = 0; dict[3] = 0;
                        dict[4] = 0; dict[5] = 0; dict[6] = 0;
                        dict[7] = 0; dict[8] = 0; dict[9] = 0;
                    }
            }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void GetKeys(int size)
        {
            Dictionary<int, int> dict = CreateDictionary(size);
            IEnumerable<int> result;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i <= 20000; i++)
                    {
                        result = dict.Keys; result = dict.Keys; result = dict.Keys;
                        result = dict.Keys; result = dict.Keys; result = dict.Keys;
                        result = dict.Keys; result = dict.Keys; result = dict.Keys;
                    }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void TryGetValue(int size)
        {
            Dictionary<int, int> dict = CreateDictionary(size);
            // Setup - utils needs a specific seed to prevent key collision with TestData
            int retrieved;
            Random rand = new Random(837322);
            int key = rand.Next(0, 400000);
            dict.Add(key, 12);

            // Actual perf testing
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i <= 1000; i++)
                    {
                        dict.TryGetValue(key, out retrieved); dict.TryGetValue(key, out retrieved);
                        dict.TryGetValue(key, out retrieved); dict.TryGetValue(key, out retrieved);
                        dict.TryGetValue(key, out retrieved); dict.TryGetValue(key, out retrieved);
                        dict.TryGetValue(key, out retrieved); dict.TryGetValue(key, out retrieved);
                    }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void ContainsKey(int size)
        {
            Dictionary<int, int> dict = CreateDictionary(size);

            // Setup - utils needs a specific seed to prevent key collision with TestData
            Random rand = new Random(837322);
            int key = rand.Next(0, 400000);
            dict.Add(key, 12);

            // Actual perf testing
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i <= 10000; i++)
                    {
                        dict.ContainsKey(key); dict.ContainsKey(key); dict.ContainsKey(key);
                        dict.ContainsKey(key); dict.ContainsKey(key); dict.ContainsKey(key);
                        dict.ContainsKey(key); dict.ContainsKey(key); dict.ContainsKey(key);
                        dict.ContainsKey(key); dict.ContainsKey(key); dict.ContainsKey(key);
                    }
        }
    }
}
