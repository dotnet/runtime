// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Xunit.Performance;
using Xunit;

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
                dict.TryAdd(key, 0);
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
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(1000)]
        public static void Remove_ValueType(long size)
        {
            Dictionary<long?, long?> collection = new Dictionary<long?, long?>();
            long?[] items;

            items = new long?[size * 10];

            for (long i = 0; i < size * 10; ++i)
            {
                items[i] = i;
                collection.Add(items[i], items[i]);
            }


            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (long i = 1; i < size; ++i)
                        collection.Remove(items[i]);
        }

        [Benchmark]
        public static void Indexer_get_ValueType()
        {
            int size = 1024;
            int? item;
            Dictionary<int?, int?> collection = new Dictionary<int?, int?>();
            for (int i = 0; i < size; ++i)
            {
                collection.Add(i, i);
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < size; ++j)
                    {
                        item = (int)collection[j];
                        item = (int)collection[j];
                        item = (int)collection[j];
                        item = (int)collection[j];
                        item = (int)collection[j];
                        item = (int)collection[j];
                        item = (int)collection[j];
                        item = (int)collection[j];
                        item = (int)collection[j];
                        item = (int)collection[j];
                    }
                }
            }
        }

        [Benchmark]
        public static void Enumeration_ValueType()
        {
            int size = 1024;
            int? key;
            int? value;
            Dictionary<int?, int?> collection = new Dictionary<int?, int?>();

            for (int i = 0; i < size; ++i)
            {
                collection.Add(i, i);
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    foreach (KeyValuePair<int?, int?> tempItem in collection)
                    {
                        key = tempItem.Key;
                        value = tempItem.Value;
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public static void Dictionary_ContainsValue_Int_True(int sampleLength)
        {
            Dictionary<int, int> dictionary = new Dictionary<int, int>();

            for (int i = 0; i < sampleLength; i++)
            {
                dictionary.Add(i, i);
            }

            bool result = false;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int j = 0; j < sampleLength; j++)
                        result = dictionary.ContainsValue(j);   //Every value searched for is present in the dictionary.
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public static void Dictionary_ContainsValue_Int_False(int sampleLength)
        {
            Dictionary<int, int> dictionary = new Dictionary<int, int>();

            for (int i = 0; i < sampleLength; i++)
            {
                dictionary.Add(i, i);
            }

            bool result = false;

            int missingValue = sampleLength;   //The value sampleLength is not present in the dictionary.

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int j = 0; j < sampleLength; j++)
                        result = dictionary.ContainsValue(missingValue);
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public static void Dictionary_ContainsValue_String_True(int sampleLength)
        {
            string[] sampleValues = new string[sampleLength];

            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            for (int i = 0; i < sampleLength; i++)
            {
                sampleValues[i] = i.ToString();

                dictionary.Add(sampleValues[i], sampleValues[i]);
            }

            bool result = false;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int j = 0; j < sampleLength; j++)
                        result = dictionary.ContainsValue(sampleValues[j]);   //Every value searched for is present in the dictionary.
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public static void Dictionary_ContainsValue_String_False(int sampleLength)
        {
            string sampleValue;

            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            for (int i = 0; i < sampleLength; i++)
            {
                sampleValue = i.ToString();

                dictionary.Add(sampleValue, sampleValue);
            }

            bool result = false;

            string missingValue = sampleLength.ToString();   //The string representation of sampleLength is not present in the dictionary.

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int j = 0; j < sampleLength; j++)
                        result = dictionary.ContainsValue(missingValue);
        }


        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public static void Dictionary_ContainsKey_Int_True(int sampleLength)
        {
            Dictionary<int, int> dictionary = new Dictionary<int, int>();

            for (int i = 0; i < sampleLength; i++)
            {
                dictionary.Add(i, i);
            }

            bool result = false;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int j = 0; j < sampleLength; j++)
                        result = dictionary.ContainsKey(j);
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public static void Dictionary_ContainsKey_Int_False(int sampleLength)
        {
            Dictionary<int, int> dictionary = new Dictionary<int, int>();

            for (int i = 0; i < sampleLength; i++)
            {
                dictionary.Add(i, i);
            }

            bool result = false;

            int missingKey = sampleLength;   //The key sampleLength is not present in the dictionary.

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int j = 0; j < sampleLength; j++)
                        result = dictionary.ContainsKey(missingKey);
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public static void Dictionary_ContainsKey_String_True(int sampleLength)
        {
            string[] sampleKeys = new string[sampleLength];

            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            for (int i = 0; i < sampleLength; i++)
            {
                sampleKeys[i] = i.ToString();

                dictionary.Add(sampleKeys[i], sampleKeys[i]);
            }

            bool result = false;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int j = 0; j < sampleLength; j++)
                        result = dictionary.ContainsKey(sampleKeys[j]);  //Every key searched for is present in the dictionary.
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public static void Dictionary_ContainsKey_String_False(int sampleLength)
        {
            string sampleKey;

            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            for (int i = 0; i < sampleLength; i++)
            {
                sampleKey = i.ToString();

                dictionary.Add(sampleKey, sampleKey);
            }

            bool result = false;

            string missingKey = sampleLength.ToString();  //The string representation of sampleLength is not present in the dictionary.

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int j = 0; j < sampleLength; j++)
                        result = dictionary.ContainsKey(missingKey);
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public static void Dictionary_ContainsKey_String_False_IgnoreCase(int sampleLength)
        {
            string sampleKey;

            Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < sampleLength; i++)
            {
                sampleKey = i.ToString();

                dictionary.Add(sampleKey, sampleKey);
            }

            bool result = false;

            string missingKey = sampleLength.ToString();  //The string representation of sampleLength is not present in the dictionary.

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int j = 0; j < sampleLength; j++)
                        result = dictionary.ContainsKey(missingKey);
        }
    }
}
