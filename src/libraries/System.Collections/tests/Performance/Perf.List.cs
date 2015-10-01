// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Collections.Tests
{
    public class Perf_List
    {
        private static List<object[]> _testData;

        /// <summary>
        /// Yields several Lists containing increasing amounts of strings
       ///  can be used as MemberData input to performance tests for Dictionary
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
                _testData.Add(new object[] { CreateList(utils, 1000) });
                _testData.Add(new object[] { CreateList(utils, 10000) });
                _testData.Add(new object[] { CreateList(utils, 100000) });
            }
            return _testData;
        }

        /// <summary>
        /// Creates a list containing a number of elements equal to the specified size
        /// </summary>
        public static List<object> CreateList(PerfUtils utils, int size)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < size; i++)
                list.Add(utils.CreateString(100));
            return list;
        }

        [Benchmark]
        [MemberData("TestData")]
        public void Add(List<object> list)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                List<object> copyList = new List<object>(list);
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        copyList.Add("TestString1"); copyList.Add("TestString2"); copyList.Add("TestString3"); copyList.Add("TestString4");
                        copyList.Add("TestString5"); copyList.Add("TestString6"); copyList.Add("TestString7"); copyList.Add("TestString8");
                    }
                }
            }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void AddRange(List<object> list)
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 5000; i++)
                    {
                        List<object> emptyList = new List<object>();
                        emptyList.AddRange(list);
                    }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void Clear(List<object> list)
        {
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
        [MemberData("TestData")]
        public void Contains(List<object> list)
        {
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
                    for (int i = 0; i < 10000; i++)
                    {
                        new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                        new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                        new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                    }
                }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void ctor_IEnumerable(List<object> list)
        {
            var array = list.ToArray();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        new List<object>(array);
        }

        [Benchmark]
        [MemberData("TestData")]
        public void GetCount(List<object> list)
        {
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
        [MemberData("TestData")]
        public void GetItem(List<object> list)
        {
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
        [MemberData("TestData")]
        public void Enumerator(List<object> list)
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        foreach (var element in list) { }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void ToArray(List<object> list)
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        list.ToArray();
        }
    }
}
