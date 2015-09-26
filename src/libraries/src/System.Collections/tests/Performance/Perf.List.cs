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
                _testData = new List<object[]>();
                _testData.Add(new object[] { CreateList(100) });
                _testData.Add(new object[] { CreateList(1000) });
            }
            return _testData;
        }

        /// <summary>
        /// Creates a list containing a number of elements equal to the specified size
        /// </summary>
        public static List<object> CreateList(int size)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < size; i++)
                list.Add(PerfUtils.CreateString(100));
            return list;
        }

        [Benchmark]
        [MemberData("TestData")]
        public void Add(List<object> list)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    list.Add("TestString1"); list.Add("TestString2"); list.Add("TestString3"); list.Add("TestString4");
                    list.Add("TestString5"); list.Add("TestString6"); list.Add("TestString7"); list.Add("TestString8");
                }
                list.RemoveRange(list.Count - 8, 8);
            }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void AddRange(List<object> list)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                List<object> emptyList = new List<object>();
                using (iteration.StartMeasurement())
                    emptyList.AddRange(list);
            }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void Clear(List<object> list)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                // Create a local hard copy so that future iterations aren't affected
                var copy = new List<object>(list);
                using (iteration.StartMeasurement())
                    copy.Clear();
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
                    list.Contains(contained); list.Contains(contained); list.Contains(contained); list.Contains(contained);
                    list.Contains(contained); list.Contains(contained); list.Contains(contained); list.Contains(contained);
                    list.Contains(contained); list.Contains(contained); list.Contains(contained); list.Contains(contained);
                }
        }

        [Benchmark]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                    new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                    new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void ctor_IEnumerable(List<object> list)
        {
            var array = list.ToArray();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
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
                    temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count;
                    temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count;
                    temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count;
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
                    temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                    temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                    temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                    temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void Enumerator(List<object> list)
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    foreach (var element in list) { }
        }

        [Benchmark]
        [MemberData("TestData")]
        public void ToArray(List<object> list)
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    list.ToArray();
        }
    }
}
