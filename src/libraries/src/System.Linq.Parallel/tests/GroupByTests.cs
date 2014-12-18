// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class GroupByTests
    {
        //
        // GroupBy
        //

        [Fact]
        public static void RunGroupByTest1()
        {
            RunGroupByTest1Core(0, 7);
            RunGroupByTest1Core(1, 7);
            RunGroupByTest1Core(7, 7);
            RunGroupByTest1Core(8, 7);
            RunGroupByTest1Core(1024, 7);
            RunGroupByTest1Core(1024 * 8, 7);
            RunGroupByTest1Core(1024 * 1024, 7);
        }

        [Fact]
        public static void RunGroupByTest2()
        {
            RunGroupByTest2Core(0, 7);
            RunGroupByTest2Core(1, 7);
            RunGroupByTest2Core(7, 7);
            RunGroupByTest2Core(8, 7);
            RunGroupByTest2Core(1024, 7);
            RunGroupByTest2Core(1024 * 8, 7);
            RunGroupByTest2Core(1024 * 1024, 7);
        }

        [Fact]
        public static void RunGroupByTest3()
        {
            RunGroupByTest3Core(1024);
            RunGroupByTest3Core(1024 * 8);
        }

        [Fact]
        public static void RunOrderByThenGroupByTest()
        {
            RunOrderByThenGroupByTestCore(1024);
            RunOrderByThenGroupByTestCore(1024 * 8);
        }

        [Fact]
        public static void RunGroupByTest4()
        {
            RunGroupByTest4Core(1000);
        }

        private static void RunGroupByTest1Core(int dataSize, int modNumber)
        {
            string methodFailed = string.Format("RunGroupByTest1({0}) - not pipelined:  FAILED.", dataSize);

            int[] left = new int[dataSize];
            for (int i = 0; i < dataSize; i++) left[i] = i + 1;

            // We will group by the number mod the 'modNumber' argument.
            ParallelQuery<IGrouping<int, int>> q = left.AsParallel<int>().GroupBy<int, int>(
                delegate (int x) { return x % modNumber; });

            List<IGrouping<int, int>> r = q.ToList();

            // We expect the size to be less than or equal to the mod number.
            if (r.Count > modNumber)
                Assert.True(false, string.Format(methodFailed + "  > Expected total count to <= {0}, actual == {1}", modNumber, r.Count));

            List<int> seen = new List<int>();
            foreach (IGrouping<int, int> g in r)
            {
                // Ensure each number in the grouping has the same mod. We also remember the
                // groupings seen, so we are sure there aren't any dups.
                if (seen.Contains(g.Key))
                {
                    Assert.True(false, string.Format(methodFailed + "  > saw a grouping by this key already: {0}", g.Key));
                }

                foreach (int x in g)
                {
                    if ((x % modNumber) != g.Key)
                    {
                        Assert.True(false, string.Format(methodFailed + "  > {0} was grouped under {1}, but when modded it == {2}", x, g.Key, x % modNumber));
                    }
                }

                seen.Add(g.Key);
            }
        }

        private static void RunGroupByTest2Core(int dataSize, int modNumber)
        {
            string methodFailed = string.Format("* RunGroupByTest1({0}) - WITH pipelining:  FAILED. ", dataSize);

            int[] left = new int[dataSize];
            for (int i = 0; i < dataSize; i++) left[i] = i + 1;

            // We will group by the number mod the 'modNumber' argument.
            ParallelQuery<IGrouping<int, int>> q = left.AsParallel<int>().GroupBy<int, int>(
                delegate (int x) { return x % modNumber; });

            int cnt = 0;
            List<int> seen = new List<int>();
            foreach (IGrouping<int, int> g in q)
            {
                cnt++;
                // Ensure each number in the grouping has the same mod. We also remember the
                // groupings seen, so we are sure there aren't any dups.
                if (seen.Contains(g.Key))
                {
                    Assert.True(false, string.Format(methodFailed + "  > saw a grouping by this key already: {0}", g.Key));
                }

                foreach (int x in g)
                {
                    if ((x % modNumber) != g.Key)
                    {
                        Assert.True(false, string.Format(methodFailed + "  > {0} was grouped under {1}, but when modded it == {2}", x, g.Key, x % modNumber));
                    }
                }

                seen.Add(g.Key);
            }

            // We expect the cnt to be less than or equal to the mod number.
            if (cnt > modNumber)
                Assert.True(false, string.Format("  > Expected total count to <= {0}, actual == {1}", modNumber, cnt));
        }

        private static void RunGroupByTest3Core(int dataSize)
        {
            string methodFailed = string.Format("RunGroupByTest3({0}) - names, grouped by 1st character:  FAILED.", dataSize);

            string[] names = new string[] { "balmer", "duffy", "gates", "jobs", "silva", "brumme", "gray", "grover", "yedlin" };

            //Random r = new Random(33); // use constant seed for predictable test runs.
            string[] data = new string[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                int index = i % names.Length;
                data[i] = names[index];
            }

            // We will group by the 1st character.
            ParallelQuery<IGrouping<char, string>> q = data.AsParallel<string>().GroupBy<string, char>(
                delegate (string x) { return x[0]; });

            List<char> seen = new List<char>();
            foreach (IGrouping<char, string> g in q)
            {
                // Ensure each number in the grouping has the same 1st char. We also remember the
                // groupings seen, so we are sure there aren't any dups.
                if (seen.Contains(g.Key))
                {
                    Assert.True(false, string.Format(methodFailed + "  > saw a grouping by this key already: {0}", g.Key));
                }

                foreach (string x in g)
                {
                    if (x[0] != g.Key)
                    {
                        Assert.True(false, string.Format(methodFailed + "  > {0} was grouped under {1}, but its 1st char is {2}", x, g.Key, x[0]));
                    }
                }

                seen.Add(g.Key);
            }
        }

        private static void RunGroupByTest4Core(int dataSize)
        {
            string method = string.Format("RunGroupByTest4({0}):  ", dataSize);

            ParallelQuery<int> parallelSrc = Enumerable.Range(0, dataSize).AsParallel().AsOrdered();

            IEnumerable<IGrouping<int, int>>[] queries = new[] {
                parallelSrc.GroupBy(x => x%2),
                parallelSrc.GroupBy(x => x%2, x => x)
            };

            foreach (IEnumerable<IGrouping<int, int>> query in queries)
            {
                int expectKey = 0;
                foreach (IGrouping<int, int> group in query)
                {
                    if (group.Key != expectKey)
                    {
                        Assert.True(false, string.Format(method + "FAIL: expected key {0} got {1}", expectKey, group.Key));
                    }

                    int expectVal = expectKey;
                    foreach (int elem in group)
                    {
                        if (expectVal != elem)
                        {
                            Assert.True(false, string.Format(method + "FAIL: expected value {0} got {1}", expectVal, elem));
                        }
                        expectVal += 2;
                    }

                    expectKey++;
                }
            }
        }

        private static void RunOrderByThenGroupByTestCore(int dataSize)
        {
            string methodFailed = string.Format("RunOrderByThenGroupByTest({0}) - sort names, grouped by 1st character:  FAILED.", dataSize);

            string[] names = new string[] { "balmer", "duffy", "gates", "jobs", "silva", "brumme", "gray", "grover", "yedlin" };

            //Random r = new Random(33); // use constant seed for predictable test runs.
            string[] data = new string[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                int index = i % names.Length;
                data[i] = names[index];
            }

            // We will sort and then group by the 1st character.
            ParallelQuery<IGrouping<char, string>> q = data.AsParallel<string>().OrderBy(
                (e) => e).GroupBy<string, char>(delegate (string x) { return x[0]; });

            List<char> seen = new List<char>();
            foreach (IGrouping<char, string> g in q)
            {
                // Ensure each number in the grouping has the same 1st char. We also remember the
                // groupings seen, so we are sure there aren't any dups.
                if (seen.Contains(g.Key))
                {
                    Assert.True(false, string.Format(methodFailed + "  > saw a grouping by this key already: {0}", g.Key));
                }

                foreach (string x in g)
                {
                    if (x[0] != g.Key)
                    {
                        Assert.True(false, string.Format(methodFailed + "  > {0} was grouped under {1}, but its 1st char is {2}", x, g.Key, x[0]));
                    }
                }

                seen.Add(g.Key);
            }
        }
    }
}
