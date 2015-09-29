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
                PerfUtils utils = new PerfUtils();
                _testData = new List<object[]>();
                _testData.Add(new object[] { CreateDictionary(utils, 1000) });
                _testData.Add(new object[] { CreateDictionary(utils, 10000) });
                _testData.Add(new object[] { CreateDictionary(utils, 100000) });
            }
            return _testData;
        }

        public static IEnumerable<object[]> TestDataIntString()
        {
            Random rand = new Random(12);
            yield return new object[] { CreateDictionaryIntInt(rand, 1000) };
            yield return new object[] { CreateDictionaryIntInt(rand, 10000) };
            yield return new object[] { CreateDictionaryIntInt(rand, 100000) };
        }

        /// <summary>
        /// Creates a Dictionary of string-string with the specified number of pairs
        /// </summary>
        public static Dictionary<string, string> CreateDictionary(PerfUtils utils, int size)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            while (dict.Count < size)
            {
                string key = utils.CreateString(50);
                while (dict.ContainsKey(key))
                    key = utils.CreateString(50);
                dict.Add(key, utils.CreateString(50));
            }
            return dict;
        }

        /// <summary>
        /// Creates a Dictionary of int-int with the specified number of pairs
        /// </summary>
        public static Dictionary<int, int> CreateDictionaryIntInt(Random rand, int size)
        {
            Dictionary<int, int> dict = new Dictionary<int, int>();
            while (dict.Count < size)
            {
                int key = rand.Next(500000, int.MaxValue);
                if (!dict.ContainsKey(key))
                    dict.Add(key, 0);
            }
           return dict1;
        }

        [Benchmark]
        [MemberData("TestDataIntString")]
        public void Add(Dictionary<int, int> dict)
        {
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
                    for (int i = 0; i <= 10000; i++)
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
                    for (int i = 0; i <= 10000; i++)
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
                    for (int i = 0; i <= 20000; i++)
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
            // Setup - utils needs a specific seed to prevent key collision with TestData
            string retrieved;
            PerfUtils utils = new PerfUtils(56334);
            string key = utils.CreateString(50);
            dict.Add(key, "value");

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

            // Teardown
            dict.Remove(key);
        }

        [Benchmark]
        [MemberData("TestData")]
        public void ContainsKey(Dictionary<string, string> dict)
        {
            // Setup - utils needs a specific seed to prevent key collision with TestData
            PerfUtils utils = new PerfUtils(152891);
            string key = utils.CreateString(50);
            dict.Add(key, "value");

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
            dict.Remove(key);
        }
    }
}
