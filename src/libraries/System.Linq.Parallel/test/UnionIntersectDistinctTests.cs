// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class UnionIntersectDistinctTests
    {
        //
        // Union
        //
        [Fact]
        public static void RunUnionTests()
        {
            RunUnionTest1(0, 0);
            RunUnionTest1(1, 0);
            RunUnionTest1(0, 1);
            RunUnionTest1(4, 4);
            RunUnionTest1(1024, 4);
            RunUnionTest1(4, 1024);
            RunUnionTest1(1024, 1024);
            RunUnionTest1(1024 * 4, 1024);
            RunUnionTest1(1024, 1024 * 4);
            RunUnionTest1(1024 * 1024, 1024 * 1024);
        }

        private static void RunUnionTest1(int leftDataSize, int rightDataSize)
        {
            string[] names1 = new string[] { "balmer", "duffy", "gates", "jobs", "silva", "brumme", "gray", "grover", "yedlin" };
            string[] names2 = new string[] { "balmer", "duffy", "gates", "essey", "crocker", "smith", "callahan", "jimbob", "beebop" };
            string method = string.Format("RunUnionTest1(leftSize={0}, rightSize={1}) - union of names:  FAILED.", leftDataSize, rightDataSize);

            //Random r = new Random(33); // use constant seed for predictable test runs.
            string[] leftData = new string[leftDataSize];
            for (int i = 0; i < leftDataSize; i++)
            {
                int index = i % names1.Length;
                leftData[i] = names1[index];
            }
            string[] rightData = new string[rightDataSize];
            for (int i = 0; i < rightDataSize; i++)
            {
                int index = i % names2.Length;
                rightData[i] = names2[index];
            }

            // Just get the union of thw two sets. We expect every name in the left and right
            // to be found in the final set, with no dups.
            ParallelQuery<string> q = leftData.AsParallel().Union<string>(rightData.AsParallel());

            // Build a list of seen names, ensuring we don't see dups.
            List<string> seen = new List<string>();
            foreach (string n in q)
            {
                // Ensure we haven't seen this name before.
                if (seen.Contains(n))
                {
                    Assert.True(false, string.Format(method + "  ** NotUnique: {0} is not unique, already seen (failure)", n));
                }

                seen.Add(n);
            }

            // Now ensure we saw all unique elements from both.
            foreach (string n in leftData)
            {
                if (!seen.Contains(n))
                {
                    Assert.True(false, string.Format(method + "  ** NotSeen: {0} wasn't found in the query, though it was in the left data", n));
                }
            }
            foreach (string n in rightData)
            {
                if (!seen.Contains(n))
                {
                    Assert.True(false, string.Format(method + "  ** NotSeen: {0} wasn't found in the query, though it was in the right data", n));
                }
            }
        }

        // This test is failing on our CI machines, probably due to the VM's limited CPU.
        // To-do: Re-enable this test when we resolve the build machine issues.
        // [Fact(Skip="Issue #176")]
        public static void RunOrderedUnionTest1()
        {
            for (int len = 1; len <= 300; len += 3)
            {
                var data =
                    Enumerable.Repeat(0, len)
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 3 })
                    .Concat(Enumerable.Repeat(0, len));


                int[][] outputs = {
                    data.AsParallel().AsOrdered().Union(Enumerable.Empty<int>().AsParallel()).ToArray(),
                    Enumerable.Empty<int>().AsParallel().AsOrdered().Union(data.AsParallel().AsOrdered()).ToArray(),
                    data.AsParallel().AsOrdered().Union(data.AsParallel()).ToArray(),
                    Enumerable.Empty<int>().AsParallel().Union(data.AsParallel().AsOrdered()).OrderBy(i=>i).ToArray(),
                    data.AsParallel().Union(data.AsParallel()).OrderBy(i=>i).ToArray(),
                };

                foreach (var output in outputs)
                {
                    if (!Enumerable.Range(0, 4).SequenceEqual(output))
                    {
                        Assert.True(false, string.Format("RunOrderedUnionTest1:  FAILED.  ** Incorrect output"));
                    }
                }
            }
        }

        //
        // Intersect
        //
        [Fact]
        public static void RunIntersectTests()
        {
            RunIntersectTest1(0, 0);
            RunIntersectTest1(1, 0);
            RunIntersectTest1(0, 1);
            RunIntersectTest1(4, 4);
            RunIntersectTest1(1024, 4);
            RunIntersectTest1(4, 1024);
            RunIntersectTest1(1024, 1024);
            RunIntersectTest1(1024 * 4, 1024);
            RunIntersectTest1(1024, 1024 * 4);
            RunIntersectTest1(1024 * 1024, 1024 * 1024);
        }

        private static void RunIntersectTest1(int leftDataSize, int rightDataSize)
        {
            string[] names1 = new string[] { "balmer", "duffy", "gates", "jobs", "silva", "brumme", "gray", "grover", "yedlin" };
            string[] names2 = new string[] { "balmer", "duffy", "gates", "essey", "crocker", "smith", "callahan", "jimbob", "beebop" };
            string method = string.Format("RunIntersectTest1(leftSize={0}, rightSize={1}) - intersect of names", leftDataSize, rightDataSize);

            //Random r = new Random(33); // use constant seed for predictable test runs.
            string[] leftData = new string[leftDataSize];
            for (int i = 0; i < leftDataSize; i++)
            {
                int index = i % names1.Length;
                leftData[i] = names1[index];
            }
            string[] rightData = new string[rightDataSize];
            for (int i = 0; i < rightDataSize; i++)
            {
                int index = i % names2.Length;
                rightData[i] = names2[index];
            }

            // Just get the intersection of thw two sets. We expect every name in the left and right
            // to be found in the final set, with no dups.
            ParallelQuery<string> q = leftData.AsParallel().Intersect<string>(rightData.AsParallel());

            // Build a list of seen names, ensuring we don't see dups.
            List<string> seen = new List<string>();
            foreach (string n in q)
            {
                // Ensure we haven't seen this name before.
                if (seen.Contains(n))
                {
                    Assert.True(false, string.Format(method + "  ** FAILED.  NotUnique: {0} is not unique, already seen (failure)", n));
                }
                // Ensure the data exists in both sources.
                if (Array.IndexOf(leftData, n) == -1)
                {
                    Assert.True(false, string.Format(method + "  ** FAILED.  NotInLeft: {0} isn't in the left data source", n));
                }
                if (Array.IndexOf(rightData, n) == -1)
                {
                    Assert.True(false, string.Format(method + "  ** FAILED.  NotInRight: {0} isn't in the right data source", n));
                }

                seen.Add(n);
            }
        }

        /// <summary>
        /// Unordered Intersect with a custom equality comparer
        /// </summary>
        [Fact]
        public static void RunIntersectTest2()
        {
            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            string[] second = { "ekiM", "bBo" };

            var comparer = new AnagramEqualityComparer();

            string[] expected = first.Except(second, comparer).ToArray();
            string[] actual = first.AsParallel().AsOrdered().Except(second.AsParallel().AsOrdered(), comparer).ToArray();

            Assert.True(expected.SequenceEqual(actual), "RunIntersectTest2:  FAILED");
        }

        [Fact]
        public static void RunOrderedIntersectTest1()
        {
            for (int len = 1; len <= 300; len += 3)
            {
                var data =
                    Enumerable.Repeat(0, len)
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 3 })
                    .Concat(Enumerable.Repeat(0, len));

                var output = data.AsParallel().AsOrdered().Intersect(data.AsParallel()).ToArray();
                if (!Enumerable.Range(0, 4).SequenceEqual(output))
                {
                    Assert.True(false, string.Format("RunOrderedIntersectTest1: FAILED.  ** Incorrect output"));
                }
            }
        }

        /// <summary>
        /// Ordered Intersect with a custom equality comparer
        /// </summary>
        [Fact]
        public static void RunOrderedIntersectTest2()
        {
            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            string[] second = { "ekiM", "bBo" };

            var comparer = new AnagramEqualityComparer();

            string[] expected = first.Except(second, comparer).ToArray();
            string[] actual = first.AsParallel().AsOrdered().Except(second.AsParallel().AsOrdered(), comparer).ToArray();

            Assert.True(expected.SequenceEqual(actual), "RunOrderedIntersectTest2:  FAILED.");
        }

        //
        // Distinct
        //
        [Fact]
        public static void RunDistinctTests()
        {
            RunDistinctTest1(0);
            RunDistinctTest1(1);
            RunDistinctTest1(4);
            RunDistinctTest1(1024);
            RunDistinctTest1(1024 * 4);
            RunDistinctTest1(1024 * 1024);
        }

        private static void RunDistinctTest1(int dataSize)
        {
            string[] names1 = new string[] { "balmer", "duffy", "gates", "jobs", "silva", "brumme", "gray", "grover", "yedlin" };
            string method = string.Format("RunDistinctTest1(dataSize={0}) - distinct names", dataSize);

            //Random r = new Random(33); // use constant seed for predictable test runs.
            string[] data = new string[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                int index = i % names1.Length;
                data[i] = names1[index];
            }

            // Find the distinct elements.
            ParallelQuery<string> q = data.AsParallel().Distinct<string>();

            // Build a list of seen names, ensuring we don't see dups.
            List<string> seen = new List<string>();
            foreach (string n in q)
            {
                // Ensure we haven't seen this name before.
                if (seen.Contains(n))
                {
                    Assert.True(false, string.Format(method + "  ** FAILED.  NotUnique: {0} is not unique, already seen (failure)", n));
                }

                seen.Add(n);
            }

            // Now ensure we saw all elements at least once.
            foreach (string n in data)
            {
                if (!seen.Contains(n))
                {
                    Assert.True(false, string.Format(method + "  ** FAILED.  NotSeen: {0} wasn't found in the query, though it was in the data", n));
                }
            }
        }

        [Fact]
        public static void RunOrderedDistinctTest1()
        {
            for (int len = 1; len <= 300; len += 3)
            {
                var data =
                    Enumerable.Repeat(0, len)
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 3 })
                    .Concat(Enumerable.Repeat(0, len));

                var output = data.AsParallel().AsOrdered().Distinct().ToArray();
                if (!Enumerable.Range(0, 4).SequenceEqual(output))
                {
                    Assert.True(false, string.Format("RunOrderedDistinctTest1:  FAILED.  ** Incorrect output"));
                }
            }
        }

        //
        // A comparer that considers two strings equal if they are anagrams of each other
        //
        private class AnagramEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string a, string b)
            {
                return a.ToCharArray().OrderBy(c => c).SequenceEqual(b.ToCharArray().OrderBy(c => c));
            }

            public int GetHashCode(string str)
            {
                return new string(str.ToCharArray().OrderBy(c => c).ToArray()).GetHashCode();
            }
        }
    }
}
