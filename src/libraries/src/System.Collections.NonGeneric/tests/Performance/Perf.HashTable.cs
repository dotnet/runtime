// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Xunit.Performance;
using System.Collections.Generic;

namespace System.Collections.Tests
{
    public class Perf_HashTable
    {
        public static Hashtable CreateHashtable(int size)
        {
            Hashtable ht = new Hashtable();
            Random rand = new Random(341553);
            while (ht.Count < size)
            {
                int key = rand.Next(400000, int.MaxValue);
                if (!ht.ContainsKey(key))
                    ht.Add(key, rand.Next());
            }
            return ht;
        }

        [Benchmark]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 40000; i++)
                    {
                        new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable();
                        new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable();
                        new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable();
                        new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable(); new Hashtable();
                    }
                }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public void GetItem(int size)
        {
            Hashtable table = CreateHashtable(size);

            // Setup - utils needs a specific seed to prevent key collision with TestData
            object result;
            Random rand = new Random(3453);
            int key = rand.Next();
            while (table.Contains(key))
                key = rand.Next();
            table.Add(key, rand.Next());
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 40000; i++)
                    {
                        result = table[key]; result = table[key]; result = table[key]; result = table[key];
                        result = table[key]; result = table[key]; result = table[key]; result = table[key];
                        result = table[key]; result = table[key]; result = table[key]; result = table[key];
                        result = table[key]; result = table[key]; result = table[key]; result = table[key];
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public void SetItem(int size)
        {
            Hashtable table = CreateHashtable(size);
            Random rand = new Random(3453);
            int key = rand.Next();
            while (table.Contains(key))
                key = rand.Next();
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 40000; i++)
                    {
                        table[key] = "newValue"; table[key] = "newValue"; table[key] = "newValue";
                        table[key] = "newValue"; table[key] = "newValue"; table[key] = "newValue";
                        table[key] = "newValue"; table[key] = "newValue"; table[key] = "newValue";
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public void Add(int size)
        {
            Hashtable table = CreateHashtable(size);
            foreach (var iteration in Benchmark.Iterations)
            {
                Hashtable tableCopy = new Hashtable(table);
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 40000; i++)
                    {
                        tableCopy.Add(i * 10 + 1, "value"); tableCopy.Add(i * 10 + 2, "value"); tableCopy.Add(i * 10 + 3, "value");
                        tableCopy.Add(i * 10 + 4, "value"); tableCopy.Add(i * 10 + 5, "value"); tableCopy.Add(i * 10 + 6, "value");
                        tableCopy.Add(i * 10 + 7, "value"); tableCopy.Add(i * 10 + 8, "value"); tableCopy.Add(i * 10 + 9, "value");
                    }
                }
            }
        }
    }
}
