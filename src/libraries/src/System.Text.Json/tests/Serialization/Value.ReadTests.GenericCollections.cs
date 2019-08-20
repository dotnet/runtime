// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ValueTests
    {
        [Fact]
        public static void ReadListOfList()
        {
            List<List<int>> result = JsonSerializer.Deserialize<List<List<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));

            Assert.Equal(1, result[0][0]);
            Assert.Equal(2, result[0][1]);
            Assert.Equal(3, result[1][0]);
            Assert.Equal(4, result[1][1]);

            GenericListWrapper<StringListWrapper> result2 = JsonSerializer.Deserialize<GenericListWrapper<StringListWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");

            Assert.Equal("1", result2[0][0]);
            Assert.Equal("2", result2[0][1]);
            Assert.Equal("3", result2[1][0]);
            Assert.Equal("4", result2[1][1]);
        }

        [Fact]
        public static void ReadListOfArray()
        {
            List<int[]> result = JsonSerializer.Deserialize<List<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));

            Assert.Equal(1, result[0][0]);
            Assert.Equal(2, result[0][1]);
            Assert.Equal(3, result[1][0]);
            Assert.Equal(4, result[1][1]);

            GenericListWrapper<string[]> result2 = JsonSerializer.Deserialize<GenericListWrapper<string[]>>(@"[[""1"",""2""],[""3"",""4""]]");

            Assert.Equal("1", result2[0][0]);
            Assert.Equal("2", result2[0][1]);
            Assert.Equal("3", result2[1][0]);
            Assert.Equal("4", result2[1][1]);
        }

        [Fact]
        public static void ReadArrayOfList()
        {
            List<int>[] result = JsonSerializer.Deserialize<List<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));

            Assert.Equal(1, result[0][0]);
            Assert.Equal(2, result[0][1]);
            Assert.Equal(3, result[1][0]);
            Assert.Equal(4, result[1][1]);

            StringListWrapper[] result2 = JsonSerializer.Deserialize<StringListWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]");
            Assert.Equal("1", result2[0][0]);
            Assert.Equal("2", result2[0][1]);
            Assert.Equal("3", result2[1][0]);
            Assert.Equal("4", result2[1][1]);
        }

        [Fact]
        public static void ReadSimpleList()
        {
            List<int> i = JsonSerializer.Deserialize<List<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            Assert.Equal(1, i[0]);
            Assert.Equal(2, i[1]);

            i = JsonSerializer.Deserialize<List<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, i.Count);

            StringListWrapper i2 = JsonSerializer.Deserialize<StringListWrapper>(@"[""1"",""2""]");
            Assert.Equal("1", i2[0]);
            Assert.Equal("2", i2[1]);

            i2 = JsonSerializer.Deserialize<StringListWrapper>(@"[]");
            Assert.Equal(0, i2.Count);
        }

        [Fact]
        public static void ReadGenericIEnumerableOfGenericIEnumerable()
        {
            IEnumerable<IEnumerable<int>> result = JsonSerializer.Deserialize<IEnumerable<IEnumerable<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IEnumerable<int> ie in result)
            {
                foreach (int i in ie)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<GenericIEnumerableWrapper<StringIEnumerableWrapper>>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public static void ReadIEnumerableTOfArray()
        {
            IEnumerable<int[]> result = JsonSerializer.Deserialize<IEnumerable<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<GenericIEnumerableWrapper<int[]>>(@"[[1,2],[3, 4]]"));
        }

        [Fact]
        public static void ReadArrayOfIEnumerableT()
        {
            IEnumerable<int>[] result = JsonSerializer.Deserialize<IEnumerable<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IEnumerable<int> arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<StringIEnumerableWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public static void ReadSimpleGenericIEnumerable()
        {
            IEnumerable<int> result = JsonSerializer.Deserialize<IEnumerable<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Deserialize<IEnumerable<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());

            // There is no way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<StringIEnumerableWrapper>(@"[""1"",""2""]"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<StringIEnumerableWrapper>(@"[]"));
        }

        [Fact]
        public static void ReadIListTOfIListT()
        {
            IList<IList<int>> result = JsonSerializer.Deserialize<IList<IList<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IList<int> ie in result)
            {
                foreach (int i in ie)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericIListWrapper<StringIListWrapper> result2 = JsonSerializer.Deserialize<GenericIListWrapper<StringIListWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 1;

            foreach (StringIListWrapper il in result2)
            {
                foreach (string str in il)
                {
                    Assert.Equal($"{expected++}", str);
                }
            }
        }

        [Fact]
        public static void ReadGenericIListOfArray()
        {
            IList<int[]> result = JsonSerializer.Deserialize<IList<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericIListWrapper<string[]> result2 = JsonSerializer.Deserialize<GenericIListWrapper<string[]>>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 1;

            foreach (string[] arr in result2)
            {
                foreach (string str in arr)
                {
                    Assert.Equal($"{expected++}", str);
                }
            }
        }

        [Fact]
        public static void ReadArrayOfIListT()
        {
            IList<int>[] result = JsonSerializer.Deserialize<IList<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IList<int> arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            StringIListWrapper[] result2 = JsonSerializer.Deserialize<StringIListWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 1;

            foreach (StringIListWrapper il in result2)
            {
                foreach (string str in il)
                {
                    Assert.Equal($"{expected++}", str);
                }
            }
        }

        [Fact]
        public static void ReadSimpleGenericIList()
        {
            IList<int> result = JsonSerializer.Deserialize<IList<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Deserialize<IList<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());

            StringIListWrapper result2 = JsonSerializer.Deserialize<StringIListWrapper>(@"[""1"",""2""]");
            expected = 1;

            foreach (string str in result2)
            {
                Assert.Equal($"{expected++}", str);
            }

            result2 = JsonSerializer.Deserialize<StringIListWrapper>(@"[]");
            Assert.Equal(0, result2.Count());
        }

        [Fact]
        public static void ReadGenericICollectionOfGenericICollection()
        {
            ICollection<ICollection<int>> result = JsonSerializer.Deserialize<ICollection<ICollection<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (ICollection<int> ie in result)
            {
                foreach (int i in ie)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericICollectionWrapper<StringICollectionWrapper> result2 = JsonSerializer.Deserialize<GenericICollectionWrapper<StringICollectionWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 1;

            foreach (StringICollectionWrapper ic in result2)
            {
                foreach (string str in ic)
                {
                    Assert.Equal($"{expected++}", str);
                }
            }
        }

        [Fact]
        public static void ReadGenericICollectionOfArray()
        {
            ICollection<int[]> result = JsonSerializer.Deserialize<ICollection<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericICollectionWrapper<string[]> result2 = JsonSerializer.Deserialize<GenericICollectionWrapper<string[]>>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 1;

            foreach (string[] arr in result2)
            {
                foreach (string str in arr)
                {
                    Assert.Equal($"{expected++}", str);
                }
            }
        }

        [Fact]
        public static void ReadArrayOfGenericICollection()
        {
            ICollection<int>[] result = JsonSerializer.Deserialize<ICollection<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (ICollection<int> arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public static void ReadSimpleGenericICollection()
        {
            ICollection<int> result = JsonSerializer.Deserialize<ICollection<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Deserialize<ICollection<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());

            StringICollectionWrapper result2 = JsonSerializer.Deserialize<StringICollectionWrapper>(@"[""1"",""2""]");
            expected = 1;

            foreach (string str in result2)
            {
                Assert.Equal($"{expected++}", str);
            }

            result2 = JsonSerializer.Deserialize<StringICollectionWrapper>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result2.Count());
        }

        [Fact]
        public static void ReadGenericIReadOnlyCollectionOfGenericIReadOnlyCollection()
        {
            IReadOnlyCollection<IReadOnlyCollection<int>> result = JsonSerializer.Deserialize<IReadOnlyCollection<IReadOnlyCollection<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IReadOnlyCollection<int> ie in result)
            {
                foreach (int i in ie)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // There's no way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<GenericIReadOnlyCollectionWrapper<StringIReadOnlyCollectionWrapper>>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public static void ReadGenericIReadOnlyCollectionOfArray()
        {
            IReadOnlyCollection<int[]> result = JsonSerializer.Deserialize<IReadOnlyCollection<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<GenericIReadOnlyCollectionWrapper<int[]>>(@"[[1,2],[3,4]]"));
        }

        [Fact]
        public static void ReadArrayOfIReadOnlyCollectionT()
        {
            IReadOnlyCollection<int>[] result = JsonSerializer.Deserialize<IReadOnlyCollection<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IReadOnlyCollection<int> arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<StringIReadOnlyCollectionWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public static void ReadGenericSimpleIReadOnlyCollection()
        {
            IReadOnlyCollection<int> result = JsonSerializer.Deserialize<IReadOnlyCollection<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Deserialize<IReadOnlyCollection<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());

            // No way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<StringIReadOnlyCollectionWrapper>(@"[""1"",""2""]"));
        }

        [Fact]
        public static void ReadGenericIReadOnlyListOfGenericIReadOnlyList()
        {
            IReadOnlyList<IReadOnlyList<int>> result = JsonSerializer.Deserialize<IReadOnlyList<IReadOnlyList<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IReadOnlyList<int> ie in result)
            {
                foreach (int i in ie)
                {
                    Assert.Equal(expected++, i);
                }
            }

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<GenericIReadOnlyListWrapper<StringIReadOnlyListWrapper>>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public static void ReadGenericIReadOnlyListOfArray()
        {
            IReadOnlyList<int[]> result = JsonSerializer.Deserialize<IReadOnlyList<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<GenericIReadOnlyListWrapper<string[]>>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public static void ReadArrayOfGenericIReadOnlyList()
        {
            IReadOnlyList<int>[] result = JsonSerializer.Deserialize<IReadOnlyList<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IReadOnlyList<int> arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<StringIReadOnlyListWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public static void ReadSimpleGenericIReadOnlyList()
        {
            IReadOnlyList<int> result = JsonSerializer.Deserialize<IReadOnlyList<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Deserialize<IReadOnlyList<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());

            // No way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<StringIReadOnlyListWrapper>(@"[""1"",""2""]"));
        }

        [Fact]
        public static void ReadGenericISetOfGenericISet()
        {
            ISet<ISet<int>> result = JsonSerializer.Deserialize<ISet<ISet<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));

            if (result.First().Contains(1))
            {
                Assert.Equal(new HashSet<int> { 1, 2 }, result.First());
                Assert.Equal(new HashSet<int> { 3, 4 }, result.Last());
            }
            else
            {
                Assert.Equal(new HashSet<int> { 3, 4 }, result.First());
                Assert.Equal(new HashSet<int> { 1, 2 }, result.Last());
            }

            GenericISetWrapper<StringISetWrapper> result2 = JsonSerializer.Deserialize<GenericISetWrapper<StringISetWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");

            if (result2.First().Contains("1"))
            {
                Assert.Equal(new HashSet<string> { "1", "2" }, (ISet<string>)result2.First());
                Assert.Equal(new HashSet<string> { "3", "4" }, (ISet<string>)result2.Last());
            }
            else
            {
                Assert.Equal(new HashSet<string> { "3", "4" }, (ISet<string>)result.First());
                Assert.Equal(new HashSet<string> { "1", "2" }, (ISet<string>)result.Last());
            }
        }

        [Fact]
        public static void ReadISetTOfHashSetT()
        {
            ISet<HashSet<int>> result = JsonSerializer.Deserialize<ISet<HashSet<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));

            if (result.First().Contains(1))
            {
                Assert.Equal(new HashSet<int> { 1, 2 }, result.First());
                Assert.Equal(new HashSet<int> { 3, 4 }, result.Last());
            }
            else
            {
                Assert.Equal(new HashSet<int> { 3, 4 }, result.First());
                Assert.Equal(new HashSet<int> { 1, 2 }, result.Last());
            }
        }

        [Fact]
        public static void ReadHashSetTOfISetT()
        {
            HashSet<ISet<int>> result = JsonSerializer.Deserialize<HashSet<ISet<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));

            if (result.First().Contains(1))
            {
                Assert.Equal(new HashSet<int> { 1, 2 }, result.First());
                Assert.Equal(new HashSet<int> { 3, 4 }, result.Last());
            }
            else
            {
                Assert.Equal(new HashSet<int> { 3, 4 }, result.First());
                Assert.Equal(new HashSet<int> { 1, 2 }, result.Last());
            }
        }

        [Fact]
        public static void ReadISetTOfArray()
        {
            ISet<int[]> result = JsonSerializer.Deserialize<ISet<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));

            if (result.First().Contains(1))
            {
                Assert.Equal(new HashSet<int> { 1, 2 }, result.First());
                Assert.Equal(new HashSet<int> { 3, 4 }, result.Last());
            }
            else
            {
                Assert.Equal(new HashSet<int> { 3, 4 }, result.First());
                Assert.Equal(new HashSet<int> { 1, 2 }, result.Last());
            }
        }

        [Fact]
        public static void ReadArrayOfISetT()
        {
            ISet<int>[] result = JsonSerializer.Deserialize<ISet<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));

            Assert.Equal(new HashSet<int> { 1, 2 }, result.First());
            Assert.Equal(new HashSet<int> { 3, 4 }, result.Last());
        }

        [Fact]
        public static void ReadSimpleISetT()
        {
            ISet<int> result = JsonSerializer.Deserialize<ISet<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));

            Assert.Equal(new HashSet<int> { 1, 2 }, result);

            result = JsonSerializer.Deserialize<ISet<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void StackTOfStackT()
        {
            Stack<Stack<int>> result = JsonSerializer.Deserialize<Stack<Stack<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 4;

            foreach (Stack<int> st in result)
            {
                foreach (int i in st)
                {
                    Assert.Equal(expected--, i);
                }
            }

            GenericStackWrapper<StringStackWrapper> result2 = JsonSerializer.Deserialize<GenericStackWrapper<StringStackWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 4;

            foreach (StringStackWrapper st in result2)
            {
                foreach (string str in st)
                {
                    Assert.Equal($"{expected--}", str);
                }
            }
        }

        [Fact]
        public static void ReadGenericStackOfArray()
        {
            Stack<int[]> result = JsonSerializer.Deserialize<Stack<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 3;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }

                expected = 1;
            }

            GenericStackWrapper<string[]> result2 = JsonSerializer.Deserialize<GenericStackWrapper<string[]>>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 3;

            foreach (string[] arr in result2)
            {
                foreach (string str in arr)
                {
                    Assert.Equal($"{expected++}", str);
                }

                expected = 1;
            }
        }

        [Fact]
        public static void ReadArrayOfGenericStack()
        {
            Stack<int>[] result = JsonSerializer.Deserialize<Stack<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 2;

            foreach (Stack<int> st in result)
            {
                foreach (int i in st)
                {
                    Assert.Equal(expected--, i);
                }

                expected = 4;
            }

            StringStackWrapper[] result2 = JsonSerializer.Deserialize<StringStackWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 2;

            foreach (StringStackWrapper st in result2)
            {
                foreach (string str in st)
                {
                    Assert.Equal($"{expected--}", str);
                }

                expected = 4;
            }
        }

        [Fact]
        public static void ReadSimpleGenericStack()
        {
            Stack<int> result = JsonSerializer.Deserialize<Stack<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 2;

            foreach (int i in result)
            {
                Assert.Equal(expected--, i);
            }

            result = JsonSerializer.Deserialize<Stack<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());

            StringStackWrapper result2 = JsonSerializer.Deserialize<StringStackWrapper>(@"[""1"",""2""]");
            expected = 2;

            foreach (string str in result2)
            {
                Assert.Equal($"{expected--}", str);
            }

            result2 = JsonSerializer.Deserialize<StringStackWrapper>(@"[]");
            Assert.Equal(0, result2.Count());
        }

        [Fact]
        public static void ReadQueueTOfQueueT()
        {
            Queue<Queue<int>> result = JsonSerializer.Deserialize<Queue<Queue<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (Queue<int> q in result)
            {
                foreach (int i in q)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericQueueWrapper<StringQueueWrapper> result2 = JsonSerializer.Deserialize<GenericQueueWrapper<StringQueueWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 1;

            foreach (StringQueueWrapper q in result2)
            {
                foreach (string str in q)
                {
                    Assert.Equal($"{expected++}", str);
                }
            }
        }

        [Fact]
        public static void ReadQueueTOfArray()
        {
            Queue<int[]> result = JsonSerializer.Deserialize<Queue<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public static void ReadArrayOfIQueueT()
        {
            Queue<int>[] result = JsonSerializer.Deserialize<Queue<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (Queue<int> q in result)
            {
                foreach (int i in q)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public static void ReadSimpleQueueT()
        {
            Queue<int> result = JsonSerializer.Deserialize<Queue<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }
            result = JsonSerializer.Deserialize<Queue<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());

        }

        [Fact]
        public static void ReadHashSetTOfHashSetT()
        {
            HashSet<HashSet<int>> result = JsonSerializer.Deserialize<HashSet<HashSet<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (HashSet<int> hs in result)
            {
                foreach (int i in hs)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericHashSetWrapper<StringHashSetWrapper> result2 = JsonSerializer.Deserialize<GenericHashSetWrapper<StringHashSetWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 1;

            foreach (StringHashSetWrapper hs in result2)
            {
                foreach (string str in hs)
                {
                    Assert.Equal($"{expected++}", str);
                }
            }
        }

        [Fact]
        public static void ReadHashSetTOfArray()
        {
            HashSet<int[]> result = JsonSerializer.Deserialize<HashSet<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public static void ReadArrayOfIHashSetT()
        {
            HashSet<int>[] result = JsonSerializer.Deserialize<HashSet<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (HashSet<int> hs in result)
            {
                foreach (int i in hs)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public static void ReadSimpleHashSetT()
        {
            HashSet<int> result = JsonSerializer.Deserialize<HashSet<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Deserialize<HashSet<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadGenericLinkedListOfGenericLinkedList()
        {
            LinkedList<LinkedList<int>> result = JsonSerializer.Deserialize<LinkedList<LinkedList<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (LinkedList<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericLinkedListWrapper<StringLinkedListWrapper> result2 = JsonSerializer.Deserialize<GenericLinkedListWrapper<StringLinkedListWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 1;

            foreach (StringLinkedListWrapper l in result2)
            {
                foreach (string str in l)
                {
                    Assert.Equal($"{expected++}", str);
                }
            }
        }

        [Fact]
        public static void ReadLinkedListTOfArray()
        {
            LinkedList<int[]> result = JsonSerializer.Deserialize<LinkedList<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public static void ReadArrayOfILinkedListT()
        {
            LinkedList<int>[] result = JsonSerializer.Deserialize<LinkedList<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (LinkedList<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public static void ReadSimpleLinkedListT()
        {
            LinkedList<int> result = JsonSerializer.Deserialize<LinkedList<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Deserialize<LinkedList<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadArrayOfSortedSetT()
        {
            SortedSet<int>[] result = JsonSerializer.Deserialize<SortedSet<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (SortedSet<int> s in result)
            {
                foreach (int i in s)
                {
                    Assert.Equal(expected++, i);
                }
            }

            StringSortedSetWrapper[] result2 = JsonSerializer.Deserialize<StringSortedSetWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 1;

            foreach (StringSortedSetWrapper s in result2)
            {
                foreach (string str in s)
                {
                    Assert.Equal($"{expected++}", str);
                }
            }
        }

        [Fact]
        public static void ReadSimpleSortedSetT()
        {
            SortedSet<int> result = JsonSerializer.Deserialize<SortedSet<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Deserialize<SortedSet<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadSimpleKeyValuePairFail()
        {
            // Invalid form: no Value
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<string, int>>(@"{""Key"": 123}"));

            // Invalid form: extra property
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<string, int>>(@"{""Key"": ""Key"", ""Value"": 123, ""Value2"": 456}"));

            // Invalid form: does not contain both Key and Value properties
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<string, int>>(@"{""Key"": ""Key"", ""Val"": 123"));
        }

        [Fact]
        public static void ReadListOfKeyValuePair()
        {
            List<KeyValuePair<string, int>> input = JsonSerializer.Deserialize<List<KeyValuePair<string, int>>>(@"[{""Key"": ""123"", ""Value"": 123},{""Key"": ""456"", ""Value"": 456}]");

            Assert.Equal(2, input.Count);
            Assert.Equal("123", input[0].Key);
            Assert.Equal(123, input[0].Value);
            Assert.Equal("456", input[1].Key);
            Assert.Equal(456, input[1].Value);
        }

        [Fact]
        public static void ReadKeyValuePairOfList()
        {
            KeyValuePair<string, List<int>> input = JsonSerializer.Deserialize<KeyValuePair<string, List<int>>>(@"{""Key"":""Key"", ""Value"":[1, 2, 3]}");

            Assert.Equal("Key", input.Key);
            Assert.Equal(3, input.Value.Count);
            Assert.Equal(1, input.Value[0]);
            Assert.Equal(2, input.Value[1]);
            Assert.Equal(3, input.Value[2]);
        }

        [Fact]
        public static void ReadKeyValuePairOfKeyValuePair()
        {
            KeyValuePair<string, KeyValuePair<int, int>> input = JsonSerializer.Deserialize<KeyValuePair<string, KeyValuePair<int, int>>>(@"{""Key"":""Key"", ""Value"":{""Key"":1, ""Value"":2}}");

            Assert.Equal("Key", input.Key);
            Assert.Equal(1, input.Value.Key);
            Assert.Equal(2, input.Value.Value);
        }

        [Theory]
        [InlineData(@"{""Key"":""Key"", ""Value"":{""Key"":1, ""Value"":2}}")]
        [InlineData(@"{""Key"":""Key"", ""Value"":{""Value"":2, ""Key"":1}}")]
        [InlineData(@"{""Value"":{""Key"":1, ""Value"":2}, ""Key"":""Key""}")]
        [InlineData(@"{""Value"":{""Value"":2, ""Key"":1}, ""Key"":""Key""}")]
        public static void ReadKeyValuePairOfKeyValuePair(string json)
        {
            KeyValuePair<string, KeyValuePair<int, int>> input = JsonSerializer.Deserialize<KeyValuePair<string, KeyValuePair<int, int>>>(json);

            Assert.Equal("Key", input.Key);
            Assert.Equal(1, input.Value.Key);
            Assert.Equal(2, input.Value.Value);
        }

        [Fact]
        public static void ReadKeyValuePairWithNullValues()
        {
            {
                KeyValuePair<string, string> kvp = JsonSerializer.Deserialize<KeyValuePair<string, string>>(@"{""Key"":""key"",""Value"":null}");
                Assert.Equal("key", kvp.Key);
                Assert.Null(kvp.Value);
            }

            {
                KeyValuePair<string, object> kvp = JsonSerializer.Deserialize<KeyValuePair<string, object>>(@"{""Key"":""key"",""Value"":null}");
                Assert.Equal("key", kvp.Key);
                Assert.Null(kvp.Value);
            }

            {
                KeyValuePair<string, SimpleClassWithKeyValuePairs> kvp = JsonSerializer.Deserialize<KeyValuePair<string, SimpleClassWithKeyValuePairs>>(@"{""Key"":""key"",""Value"":null}");
                Assert.Equal("key", kvp.Key);
                Assert.Null(kvp.Value);
            }

            {
                KeyValuePair<string, KeyValuePair<string, string>> kvp = JsonSerializer.Deserialize<KeyValuePair<string, KeyValuePair<string, string>>>(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}");
                Assert.Equal("key", kvp.Key);
                Assert.Equal("key", kvp.Value.Key);
                Assert.Null(kvp.Value.Value);
            }

            {
                KeyValuePair<string, KeyValuePair<string, object>> kvp = JsonSerializer.Deserialize<KeyValuePair<string, KeyValuePair<string, object>>>(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}");
                Assert.Equal("key", kvp.Key);
                Assert.Equal("key", kvp.Value.Key);
                Assert.Null(kvp.Value.Value);
            }

            {
                KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>> kvp = JsonSerializer.Deserialize<KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>>>(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}");
                Assert.Equal("key", kvp.Key);
                Assert.Equal("key", kvp.Value.Key);
                Assert.Null(kvp.Value.Value);
            }
        }

        [Fact]
        public static void ReadClassWithNullKeyValuePairValues()
        {
            string json =
                    @"{" +
                        @"""KvpWStrVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":null" +
                        @"}," +
                        @"""KvpWObjVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":null" +
                        @"}," +
                        @"""KvpWClassVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":null" +
                        @"}," +
                        @"""KvpWStrKvpVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":{" +
                                @"""Key"":""key""," +
                                @"""Value"":null" +
                            @"}" +
                        @"}," +
                        @"""KvpWObjKvpVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":{" +
                                @"""Key"":""key""," +
                                @"""Value"":null" +
                            @"}" +
                        @"}," +
                        @"""KvpWClassKvpVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":{" +
                                @"""Key"":""key""," +
                                @"""Value"":null" +
                            @"}" +
                        @"}" +
                    @"}";
            SimpleClassWithKeyValuePairs obj = JsonSerializer.Deserialize<SimpleClassWithKeyValuePairs>(json);

            Assert.Equal("key", obj.KvpWStrVal.Key);
            Assert.Equal("key", obj.KvpWObjVal.Key);
            Assert.Equal("key", obj.KvpWClassVal.Key);
            Assert.Equal("key", obj.KvpWStrKvpVal.Key);
            Assert.Equal("key", obj.KvpWObjKvpVal.Key);
            Assert.Equal("key", obj.KvpWClassKvpVal.Key);
            Assert.Equal("key", obj.KvpWStrKvpVal.Value.Key);
            Assert.Equal("key", obj.KvpWObjKvpVal.Value.Key);
            Assert.Equal("key", obj.KvpWClassKvpVal.Value.Key);

            Assert.Null(obj.KvpWStrVal.Value);
            Assert.Null(obj.KvpWObjVal.Value);
            Assert.Null(obj.KvpWClassVal.Value);
            Assert.Null(obj.KvpWStrKvpVal.Value.Value);
            Assert.Null(obj.KvpWObjKvpVal.Value.Value);
            Assert.Null(obj.KvpWClassKvpVal.Value.Value);
        }

        [Fact]
        public static void ReadSimpleTestClass_GenericCollectionWrappers()
        {
            SimpleTestClassWithGenericCollectionWrappers obj = JsonSerializer.Deserialize<SimpleTestClassWithGenericCollectionWrappers>(SimpleTestClassWithGenericCollectionWrappers.s_json);
            obj.Verify();
        }

        [Fact]
        public static void ReadSimpleTestClass_GenericWrappers_NoAddMethod_Throws()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<SimpleTestClassWithStringIEnumerableWrapper>(SimpleTestClassWithStringIEnumerableWrapper.s_json));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<SimpleTestClassWithStringIReadOnlyCollectionWrapper>(SimpleTestClassWithStringIReadOnlyCollectionWrapper.s_json));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<SimpleTestClassWithStringIReadOnlyListWrapper>(SimpleTestClassWithStringIReadOnlyListWrapper.s_json));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper>(SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper.s_json));
        }

        [Fact]
        public static void ReadPrimitiveStringCollection_Throws()
        {
            Assert.Throws<InvalidCastException>(() => JsonSerializer.Deserialize<StringCollection>(@"[""1"", ""2""]"));
        }

        [Fact]
        public static void ReadReadOnlyCollections_Throws()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ReadOnlyStringIListWrapper>(@"[""1"", ""2""]"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ReadOnlyStringICollectionWrapper>(@"[""1"", ""2""]"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ReadOnlyStringToStringIDictionaryWrapper>(@"{""Key"":""key"",""Value"":""value""}"));
        }
    }
}
