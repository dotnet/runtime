// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Collections.Tests
{
    public class Perf_List
    {
        /// <summary>
        /// Creates a list containing a number of elements equal to the specified size
        /// </summary>
        public static List<object> CreateList(int size)
        {
            Random rand = new Random(24565653);
            List<object> list = new List<object>();
            for (int i = 0; i < size; i++)
                list.Add(rand.Next());
            return list;
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Add(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
            {
                List<object> copyList = new List<object>(list);
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        copyList.Add(123555); copyList.Add(123555); copyList.Add(123555); copyList.Add(123555);
                        copyList.Add(123555); copyList.Add(123555); copyList.Add(123555); copyList.Add(123555);
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void AddRange(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 5000; i++)
                    {
                        List<object> emptyList = new List<object>();
                        emptyList.AddRange(list);
                    }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Clear(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup lists to clear
                List<object>[] listlist = new List<object>[5000];
                for (int i = 0; i < 5000; i++)
                    listlist[i] = new List<object>(list);

                // Clear the lists
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 5000; i++)
                        listlist[i].Clear();
            }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Contains(int size)
        {
            List<object> list = CreateList(size);
            object contained = list[list.Count / 2];
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 500; i++)
                    {
                        list.Contains(contained); list.Contains(contained); list.Contains(contained); list.Contains(contained);
                        list.Contains(contained); list.Contains(contained); list.Contains(contained); list.Contains(contained);
                        list.Contains(contained); list.Contains(contained); list.Contains(contained); list.Contains(contained);
                    }
                }
        }

        [Benchmark]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 20000; i++)
                    {
                        new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                        new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                        new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                    }
                }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void ctor_IEnumerable(int size)
        {
            List<object> list = CreateList(size);
            var array = list.ToArray();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        new List<object>(array);
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void GetCount(int size)
        {
            List<object> list = CreateList(size);
            int temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count;
                        temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count;
                        temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count;
                    }
                }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void GetItem(int size)
        {
            List<object> list = CreateList(size);
            object temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                        temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                        temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                        temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                    }
                }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Enumerator(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        foreach (var element in list) { }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void SetCapacity(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 100; i++)
                    {
                        // Capacity set back and forth between size+1 and size+2
                        list.Capacity = size + (i % 2) + 1;
                    }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void ToArray(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        list.ToArray();
        }
    }
}
