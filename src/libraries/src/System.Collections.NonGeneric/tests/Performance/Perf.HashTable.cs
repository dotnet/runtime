// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Xunit.Performance;
using System.Collections.Generic;

namespace System.Collections.Tests
{
    public class Perf_HashTable
    {
        private static List<object[]> _testData;

        public static IEnumerable<object[]> TestData()
        {
            if (_testData == null)
            {
                _testData = new List<object[]>();
                _testData.Add(new object[] { CreateHashtable(100) });
                _testData.Add(new object[] { CreateHashtable(1000) });
            }
            return _testData;
        }

        public static Hashtable CreateHashtable(int size)
        {
            Hashtable ht = new Hashtable();
            for (int i = 0; i < size; i++)
                ht.Add(PerfUtils.CreateString(50), PerfUtils.CreateString(50));
            return ht;
        }

        [Benchmark]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable();
                    new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable();
                    new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable();
                    new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable();
                }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void GetItem(Hashtable table)
        {
            object result;
            string key = PerfUtils.CreateString(50);
            table.Add(key, "value");
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    result = table[key]; result = table[key]; result = table[key]; result = table[key];
                    result = table[key]; result = table[key]; result = table[key]; result = table[key];
                    result = table[key]; result = table[key]; result = table[key]; result = table[key];
                    result = table[key]; result = table[key]; result = table[key]; result = table[key];
                }
            }
            table.Remove(key);
        }

        [Benchmark]
        [MemberData("TestData")]
        public void Add(Hashtable table)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    table.Add("key1", "value"); table.Add("key2", "value"); table.Add("key3", "value");
                    table.Add("key4", "value"); table.Add("key5", "value"); table.Add("key6", "value");
                    table.Add("key7", "value"); table.Add("key8", "value"); table.Add("key9", "value");
                }
                for (int i = 1; i <= 9; i++)
                    table.Remove("key" + i);
            }
        }
    }
}
