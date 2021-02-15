// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
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
        public static void ReadGenericStructIList()
        {
            string json = "[10,20,30]";
            var wrapper = JsonSerializer.Deserialize<GenericStructIListWrapper<int>>(json);
            Assert.Equal(3, wrapper.Count);
            Assert.Equal(10, wrapper[0]);
            Assert.Equal(20, wrapper[1]);
            Assert.Equal(30, wrapper[2]);
        }

        [Fact]
        public static void ReadNullableGenericStructIList()
        {
            string json = "[10,20,30]";
            var wrapper = JsonSerializer.Deserialize<GenericStructIListWrapper<int>?>(json);
            Assert.True(wrapper.HasValue);
            Assert.Equal(3, wrapper.Value.Count);
            Assert.Equal(10, wrapper.Value[0]);
            Assert.Equal(20, wrapper.Value[1]);
            Assert.Equal(30, wrapper.Value[2]);
        }

        [Fact]
        public static void ReadNullableGenericStructIListWithNullJson()
        {
            var wrapper = JsonSerializer.Deserialize<GenericStructIListWrapper<int>?>("null");
            Assert.False(wrapper.HasValue);
        }

        [Fact]
        public static void ReadGenericStructICollection()
        {
            string json = "[10,20,30]";
            var wrapper = JsonSerializer.Deserialize<GenericStructICollectionWrapper<int>>(json);
            Assert.Equal(3, wrapper.Count);
            Assert.Equal(10, wrapper.ElementAt(0));
            Assert.Equal(20, wrapper.ElementAt(1));
            Assert.Equal(30, wrapper.ElementAt(2));
        }

        [Fact]
        public static void ReadNullableGenericStructICollection()
        {
            string json = "[10,20,30]";
            var wrapper = JsonSerializer.Deserialize<GenericStructICollectionWrapper<int>?>(json);
            Assert.True(wrapper.HasValue);
            Assert.Equal(3, wrapper.Value.Count);
            Assert.Equal(10, wrapper.Value.ElementAt(0));
            Assert.Equal(20, wrapper.Value.ElementAt(1));
            Assert.Equal(30, wrapper.Value.ElementAt(2));
        }

        [Fact]
        public static void ReadNullableGenericStructICollectionWithNullJson()
        {
            var wrapper = JsonSerializer.Deserialize<GenericStructICollectionWrapper<int>?>("null");
            Assert.False(wrapper.HasValue);
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

            GenericICollectionWrapper<GenericICollectionWrapper<string>> result2 =
                JsonSerializer.Deserialize<GenericICollectionWrapper<GenericICollectionWrapper<string>>>(@"[[""1"",""2""],[""3"",""4""]]");
            expected = 1;

            foreach (GenericICollectionWrapper<string> ic in result2)
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

            GenericICollectionWrapper<string> result2 = JsonSerializer.Deserialize<GenericICollectionWrapper<string>>(@"[""1"",""2""]");
            expected = 1;

            foreach (string str in result2)
            {
                Assert.Equal($"{expected++}", str);
            }

            result2 = JsonSerializer.Deserialize<GenericICollectionWrapper<string>>(Encoding.UTF8.GetBytes(@"[]"));
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
            Assert.Throws<NotSupportedException>(
                () => JsonSerializer.Deserialize<GenericIReadOnlyCollectionWrapper<WrapperForIReadOnlyCollectionOfT<string>>>(@"[[""1"",""2""],[""3"",""4""]]"));
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
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<WrapperForIReadOnlyCollectionOfT<string>[]>(@"[[""1"",""2""],[""3"",""4""]]"));
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
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<WrapperForIReadOnlyCollectionOfT<string>>(@"[""1"",""2""]"));
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
        public static void ReadGenericStructISet()
        {
            string json = "[10, 20, 30]";
            var wrapper = JsonSerializer.Deserialize<GenericStructISetWrapper<int>>(json);
            Assert.Equal(3, wrapper.Count);
            Assert.Equal(10, wrapper.ElementAt(0));
            Assert.Equal(20, wrapper.ElementAt(1));
            Assert.Equal(30, wrapper.ElementAt(2));
        }

        [Fact]
        public static void ReadNullableGenericStructISet()
        {
            string json = "[10, 20, 30]";
            var wrapper = JsonSerializer.Deserialize<GenericStructISetWrapper<int>?>(json);
            Assert.True(wrapper.HasValue);
            Assert.Equal(3, wrapper.Value.Count);
            Assert.Equal(10, wrapper.Value.ElementAt(0));
            Assert.Equal(20, wrapper.Value.ElementAt(1));
            Assert.Equal(30, wrapper.Value.ElementAt(2));
        }

        [Fact]
        public static void ReadNullableGenericStructISetWithNullJson()
        {
            var wrapper = JsonSerializer.Deserialize<GenericStructISetWrapper<int>?>("null");
            Assert.False(wrapper.HasValue);
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
        public static void ReadClass_WithGenericStructCollectionWrapper_NullJson_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithGenericStructIListWrapper>(@"{ ""List"": null }"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithGenericStructICollectionWrapper>(@"{ ""Collection"": null }"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithGenericStructIDictionaryWrapper>(@"{ ""Dictionary"": null }"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithGenericStructISetWrapper>(@"{ ""Set"": null }"));
        }

        [Fact]
        public static void ReadSimpleTestClass_GenericStructCollectionWrappers()
        {
            SimpleTestClassWithGenericStructCollectionWrappers obj = JsonSerializer.Deserialize<SimpleTestClassWithGenericStructCollectionWrappers>(SimpleTestClassWithGenericStructCollectionWrappers.s_json);
            obj.Verify();
        }

        [Fact]
        public static void ReadSimpleTestStruct_NullableGenericStructCollectionWrappers()
        {
            {
                SimpleTestStructWithNullableGenericStructCollectionWrappers obj = JsonSerializer.Deserialize<SimpleTestStructWithNullableGenericStructCollectionWrappers>(SimpleTestStructWithNullableGenericStructCollectionWrappers.s_json);
                obj.Verify();
            }

            {
                string json =
                        @"{" +
                        @"""List"" : null," +
                        @"""Collection"" : null," +
                        @"""Set"" : null," +
                        @"""Dictionary"" : null" +
                        @"}";
                SimpleTestStructWithNullableGenericStructCollectionWrappers obj = JsonSerializer.Deserialize<SimpleTestStructWithNullableGenericStructCollectionWrappers>(json);
                Assert.False(obj.List.HasValue);
                Assert.False(obj.Collection.HasValue);
                Assert.False(obj.Set.HasValue);
                Assert.False(obj.Dictionary.HasValue);
            }
        }

        [Fact]
        public static void ReadSimpleTestClass_GenericCollectionWrappers()
        {
            SimpleTestClassWithGenericCollectionWrappers obj = JsonSerializer.Deserialize<SimpleTestClassWithGenericCollectionWrappers>(SimpleTestClassWithGenericCollectionWrappers.s_json);
            obj.Verify();
        }

        [Theory]
        [MemberData(nameof(ReadSimpleTestClass_GenericWrappers_NoAddMethod))]
        public static void ReadSimpleTestClass_GenericWrappers_NoAddMethod_Throws(Type type, string json, Type exceptionMessageType)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(json, type));
            Assert.Contains(exceptionMessageType.ToString(), ex.Message);
        }

        public static IEnumerable<object[]> ReadSimpleTestClass_GenericWrappers_NoAddMethod
        {
            get
            {
                yield return new object[]
                {
                    typeof(SimpleTestClassWithStringIEnumerableWrapper),
                    SimpleTestClassWithStringIEnumerableWrapper.s_json,
                    typeof(StringIEnumerableWrapper)
                };
                yield return new object[]
                {
                    typeof(SimpleTestClassWithStringIReadOnlyCollectionWrapper),
                    SimpleTestClassWithStringIReadOnlyCollectionWrapper.s_json,
                    typeof(WrapperForIReadOnlyCollectionOfT<string>)
                };
                yield return new object[]
                {
                    typeof(SimpleTestClassWithStringIReadOnlyListWrapper),
                    SimpleTestClassWithStringIReadOnlyListWrapper.s_json,
                    typeof(StringIReadOnlyListWrapper)
                };
                yield return new object[]
                {
                    typeof(SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper),
                    SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper.s_json,
                    typeof(GenericIReadOnlyDictionaryWrapper<string, string>)
                };
            }
        }

        [Theory]
        [InlineData(typeof(ReadOnlyWrapperForIList), @"[""1"", ""2""]")]
        [InlineData(typeof(ReadOnlyStringIListWrapper), @"[""1"", ""2""]")]
        [InlineData(typeof(ReadOnlyStringICollectionWrapper), @"[""1"", ""2""]")]
        [InlineData(typeof(ReadOnlyStringISetWrapper), @"[""1"", ""2""]")]
        [InlineData(typeof(ReadOnlyWrapperForIDictionary), @"{""Key"":""key"",""Value"":""value""}")]
        [InlineData(typeof(ReadOnlyStringToStringIDictionaryWrapper), @"{""Key"":""key"",""Value"":""value""}")]
        public static void ReadReadOnlyCollections_Throws(Type type, string json)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(json, type));
            Assert.Contains(type.ToString(), ex.Message);
        }

        [Fact]
        public static void Read_HigherOrderCollectionInheritance_Works()
        {
            const string json = "[\"test\"]";
            Assert.Equal("test", JsonSerializer.Deserialize<string[]>(json)[0]);
            Assert.Equal("test", JsonSerializer.Deserialize<List<string>>(json).First());
            Assert.Equal("test", JsonSerializer.Deserialize<StringListWrapper>(json).First());
            Assert.Equal("test", JsonSerializer.Deserialize<GenericListWrapper<string>>(json).First());
            Assert.Equal("test", JsonSerializer.Deserialize<MyMyList<string>>(json).First());
            Assert.Equal("test", JsonSerializer.Deserialize<MyListString>(json).First());
        }

        [Theory]
        [InlineData(typeof(GenericIEnumerableWrapperPrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericIEnumerableWrapperInternalConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericICollectionWrapperPrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericICollectionWrapperInternalConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericIListWrapperPrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericIListWrapperInternalConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericISetWrapperPrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericISetWrapperInternalConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericIDictionaryWrapperPrivateConstructor<string, string>), @"{""Key"":""Value""}")]
        [InlineData(typeof(GenericIDictionaryWrapperInternalConstructor<string, string>), @"{""Key"":""Value""}")]
        [InlineData(typeof(StringToStringIReadOnlyDictionaryWrapperPrivateConstructor), @"{""Key"":""Value""}")]
        [InlineData(typeof(StringToStringIReadOnlyDictionaryWrapperInternalConstructor), @"{""Key"":""Value""}")]
        [InlineData(typeof(GenericListWrapperPrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericListWrapperInternalConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericQueueWrapperPrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericQueueWrapperInternalConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericStackWrapperPrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericStackWrapperInternalConstructor<string>), @"[""1""]")]
        [InlineData(typeof(StringToGenericDictionaryWrapperPrivateConstructor<string>), @"{""Key"":""Value""}")]
        [InlineData(typeof(StringToGenericDictionaryWrapperInternalConstructor<string>), @"{""Key"":""Value""}")]
        public static void Read_Generic_NoPublicConstructor_Throws(Type type, string json)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(json, type));
            Assert.Contains(type.ToString(), ex.Message);
        }

        [Fact]
        public static void DoesNotCall_CollectionPropertyGetter_EveryTimeElementIsAdded()
        {
            var networkList = new List<string> { "Network1", "Network2" };

            string serialized = JsonSerializer.Serialize(new NetworkWrapper { NetworkList = networkList });
            Assert.Equal(@"{""NetworkList"":[""Network1"",""Network2""]}", serialized);

            NetworkWrapper obj = JsonSerializer.Deserialize<NetworkWrapper>(serialized);

            int i = 0;
            foreach (string network in obj.NetworkList)
            {
                Assert.Equal(networkList[i], network);
                i++;
            }
        }

        public class NetworkWrapper
        {
            private string _Networks = string.Empty;

            [JsonIgnore]
            public string Networks
            {
                get => _Networks;
                set => _Networks = value ?? string.Empty;
            }

            public IEnumerable<string> NetworkList
            {
                get => Networks.Split(',');
                set => Networks = value != null ? string.Join(",", value) : "";
            }
        }

        [Fact]
        public static void CollectionWith_BackingField_CanRoundtrip()
        {
            string json = "{\"AllowedGrantTypes\":[\"client_credentials\"]}";

            Client obj = JsonSerializer.Deserialize<Client>(json);
            Assert.Equal("client_credentials", obj.AllowedGrantTypes.First());

            string serialized = JsonSerializer.Serialize(obj);
            Assert.Equal(json, serialized);
        }

        private class Client
        {
            private ICollection<string> _allowedGrantTypes = new HashSetWithBackingCollection();

            public ICollection<string> AllowedGrantTypes
            {
                get { return _allowedGrantTypes; }
                set { _allowedGrantTypes = new HashSetWithBackingCollection(value); }
            }
        }

        [Theory]
        [MemberData(nameof(CustomInterfaces_Enumerables))]
        public static void CustomInterfacesNotSupported_Enumerables(Type type)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize("[]", type));
            Assert.Contains(type.ToString(), ex.ToString());
        }

        [Theory]
        [MemberData(nameof(CustomInterfaces_Dictionaries))]
        public static void CustomInterfacesNotSupported_Dictionaries(Type type)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize("{}", type));
            Assert.Contains(type.ToString(), ex.ToString());
        }

        public static IEnumerable<object[]> CustomInterfaces_Enumerables()
        {
            yield return new object[] { typeof(IDerivedICollectionOfT<string>) };
            yield return new object[] { typeof(IDerivedIList) };
            yield return new object[] { typeof(IDerivedISetOfT<string>) };
        }

        public static IEnumerable<object[]> CustomInterfaces_Dictionaries()
        {
            yield return new object[] { typeof(IDerivedIDictionaryOfTKeyTValue<string, string>) };
        }

        [Fact]
        public static void IReadOnlyDictionary_NotSupportedKey()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<IReadOnlyDictionary<Uri, int>>(@"{""http://foo"":1}"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new GenericIReadOnlyDictionaryWrapper<Uri, int>(new Dictionary<Uri, int> { { new Uri("http://foo"), 1 } })));
        }
    }
}
