// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Collections.Tests
{
    public class Perf_Dictionary
    {
        private static List<object[]> _testData;

        /// <summary>
        /// Yields several Dictionaries containing increasing amounts of string-string
        /// pairs can be used as MemberData input to performance tests for Dictionary
        /// </summary>
        /// <remarks>Any changes made to the returned collections MUST be undone. Collections
        /// used as MemberData are cached and reused in other perf tests.
        /// </remarks>
        public static List<object[]> TestData()
        {
            if (_testData == null)
            {
                _testData = new List<object[]>();
                _testData.Add(new object[] { CreateDictionary(100) });
                _testData.Add(new object[] { CreateDictionary(1000) });
            }
            return _testData;
        }

        /// <summary>
        /// Creates a Dictionary of string-string with the specified number of pairs
        /// </summary>
        public static Dictionary<string, string> CreateDictionary(int size)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            while (dict.Count < size)
            {
                string key = PerfUtils.CreateString(50);
                while (dict.ContainsKey(key))
                    key = PerfUtils.CreateString(50);
                dict.Add(key, PerfUtils.CreateString(50));
            }
            return dict;
        }

        [Benchmark]
        [MemberData("TestData")]
        public void Add(Dictionary<string, string> dict)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    dict.Add("key1", "string"); dict.Add("key2", "string"); dict.Add("key3", "string");
                    dict.Add("key4", "string"); dict.Add("key5", "string"); dict.Add("key6", "string");
                    dict.Add("key7", "string"); dict.Add("key8", "string"); dict.Add("key9", "string");
                }
                for (int i = 1; i <= 9; i++)
                    Assert.True(dict.Remove("key" + i));
            }
        }

        [Benchmark]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
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
                {
                    new Dictionary<int, string>(size); new Dictionary<int, string>(size); new Dictionary<int, string>(size);
                    new Dictionary<int, string>(size); new Dictionary<int, string>(size); new Dictionary<int, string>(size);
                    new Dictionary<int, string>(size); new Dictionary<int, string>(size); new Dictionary<int, string>(size);
                }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void GetItem(Dictionary<string, string> dict)
        {
            // Setup
            string retrieved;
            for (int i = 1; i <= 9; i++)
                dict.Add("key" + i, "value");

            // Actual perf testing
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    retrieved = dict["key1"]; retrieved = dict["key2"]; retrieved = dict["key3"];
                    retrieved = dict["key4"]; retrieved = dict["key5"]; retrieved = dict["key6"];
                    retrieved = dict["key7"]; retrieved = dict["key8"]; retrieved = dict["key9"];
                }
            }

            // Teardown
            for (int i = 1; i <= 9; i++)
                dict.Remove("key" + i);
        }

        [Benchmark]
        [MemberData("TestData")]
        public void SetItem(Dictionary<string, string> dict)
        {
            // Setup
            for (int i = 1; i <= 9; i++)
                dict.Add("key" + i, "value");

            // Actual perf testing
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    dict["key1"] = "string"; dict["key2"] = "string"; dict["key3"] = "string";
                    dict["key4"] = "string"; dict["key5"] = "string"; dict["key6"] = "string";
                    dict["key7"] = "string"; dict["key8"] = "string"; dict["key9"] = "string";
                }
            }

            // Teardown
            for (int i = 1; i <= 9; i++)
                dict.Remove("key" + i);
        }

        [Benchmark]
        [MemberData("TestData")]
        public void GetKeys(Dictionary<string, string> dict)
        {
            IEnumerable<string> result;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    result = dict.Keys; result = dict.Keys; result = dict.Keys;
                    result = dict.Keys; result = dict.Keys; result = dict.Keys;
                    result = dict.Keys; result = dict.Keys; result = dict.Keys;
                }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void TryGetValue(Dictionary<string, string> dict)
        {
            // Setup
            string retrieved;
            string key = PerfUtils.CreateString(50);
            dict.Add(key, "value");

            // Actual perf testing
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    dict.TryGetValue(key, out retrieved); dict.TryGetValue(key, out retrieved);
                    dict.TryGetValue(key, out retrieved); dict.TryGetValue(key, out retrieved);
                    dict.TryGetValue(key, out retrieved); dict.TryGetValue(key, out retrieved);
                    dict.TryGetValue(key, out retrieved); dict.TryGetValue(key, out retrieved);
                }
            }

            // Teardown
            dict.Remove(key);
        }

        [Benchmark]
        [MemberData("TestData")]
        public void ContainsKey(Dictionary<string, string> dict)
        {
            // Setup
            string key = PerfUtils.CreateString(50);
            dict.Add(key, "value");

            // Actual perf testing
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    dict.ContainsKey(key); dict.ContainsKey(key); dict.ContainsKey(key);
                    dict.ContainsKey(key); dict.ContainsKey(key); dict.ContainsKey(key);
                    dict.ContainsKey(key); dict.ContainsKey(key); dict.ContainsKey(key);
                    dict.ContainsKey(key); dict.ContainsKey(key); dict.ContainsKey(key);
                }
            }
            dict.Remove(key);
        }
    }
}
