// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public async Task ReadListOfList()
        {
            List<List<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<List<List<int>>>(@"[[1,2],[3,4]]");

            Assert.Equal(1, result[0][0]);
            Assert.Equal(2, result[0][1]);
            Assert.Equal(3, result[1][0]);
            Assert.Equal(4, result[1][1]);

            GenericListWrapper<StringListWrapper> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericListWrapper<StringListWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");

            Assert.Equal("1", result2[0][0]);
            Assert.Equal("2", result2[0][1]);
            Assert.Equal("3", result2[1][0]);
            Assert.Equal("4", result2[1][1]);
        }

        [Fact]
        public async Task ReadListOfArray()
        {
            List<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<List<int[]>>(@"[[1,2],[3,4]]");

            Assert.Equal(1, result[0][0]);
            Assert.Equal(2, result[0][1]);
            Assert.Equal(3, result[1][0]);
            Assert.Equal(4, result[1][1]);

            GenericListWrapper<string[]> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericListWrapper<string[]>>(@"[[""1"",""2""],[""3"",""4""]]");

            Assert.Equal("1", result2[0][0]);
            Assert.Equal("2", result2[0][1]);
            Assert.Equal("3", result2[1][0]);
            Assert.Equal("4", result2[1][1]);
        }

        [Fact]
        public async Task ReadArrayOfList()
        {
            List<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<List<int>[]>(@"[[1,2],[3,4]]");

            Assert.Equal(1, result[0][0]);
            Assert.Equal(2, result[0][1]);
            Assert.Equal(3, result[1][0]);
            Assert.Equal(4, result[1][1]);

            StringListWrapper[] result2 = await JsonSerializerWrapperForString.DeserializeWrapper<StringListWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]");
            Assert.Equal("1", result2[0][0]);
            Assert.Equal("2", result2[0][1]);
            Assert.Equal("3", result2[1][0]);
            Assert.Equal("4", result2[1][1]);
        }

        [Fact]
        public async Task ReadSimpleList()
        {
            List<int> i = await JsonSerializerWrapperForString.DeserializeWrapper<List<int>>(@"[1,2]");
            Assert.Equal(1, i[0]);
            Assert.Equal(2, i[1]);

            i = await JsonSerializerWrapperForString.DeserializeWrapper<List<int>>(@"[]");
            Assert.Equal(0, i.Count);

            StringListWrapper i2 = await JsonSerializerWrapperForString.DeserializeWrapper<StringListWrapper>(@"[""1"",""2""]");
            Assert.Equal("1", i2[0]);
            Assert.Equal("2", i2[1]);

            i2 = await JsonSerializerWrapperForString.DeserializeWrapper<StringListWrapper>(@"[]");
            Assert.Equal(0, i2.Count);
        }

        [Fact]
        public async Task ReadGenericIEnumerableOfGenericIEnumerable()
        {
            IEnumerable<IEnumerable<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<IEnumerable<IEnumerable<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IEnumerable<int> ie in result)
            {
                foreach (int i in ie)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<GenericIEnumerableWrapper<StringIEnumerableWrapper>>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public async Task ReadIEnumerableTOfArray()
        {
            IEnumerable<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<IEnumerable<int[]>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<GenericIEnumerableWrapper<int[]>>(@"[[1,2],[3, 4]]"));
        }

        [Fact]
        public async Task ReadArrayOfIEnumerableT()
        {
            IEnumerable<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<IEnumerable<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IEnumerable<int> arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIEnumerableWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public async Task ReadSimpleGenericIEnumerable()
        {
            IEnumerable<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<IEnumerable<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<IEnumerable<int>>(@"[]");
            Assert.Equal(0, result.Count());

            // There is no way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIEnumerableWrapper>(@"[""1"",""2""]"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIEnumerableWrapper>(@"[]"));
        }

        [Fact]
        public async Task ReadIListTOfIListT()
        {
            IList<IList<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<IList<IList<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IList<int> ie in result)
            {
                foreach (int i in ie)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericIListWrapper<StringIListWrapper> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericIListWrapper<StringIListWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadGenericIListOfArray()
        {
            IList<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<IList<int[]>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericIListWrapper<string[]> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericIListWrapper<string[]>>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadArrayOfIListT()
        {
            IList<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<IList<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IList<int> arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            StringIListWrapper[] result2 = await JsonSerializerWrapperForString.DeserializeWrapper<StringIListWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadSimpleGenericIList()
        {
            IList<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<IList<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<IList<int>>(@"[]");
            Assert.Equal(0, result.Count());

            StringIListWrapper result2 = await JsonSerializerWrapperForString.DeserializeWrapper<StringIListWrapper>(@"[""1"",""2""]");
            expected = 1;

            foreach (string str in result2)
            {
                Assert.Equal($"{expected++}", str);
            }

            result2 = await JsonSerializerWrapperForString.DeserializeWrapper<StringIListWrapper>(@"[]");
            Assert.Equal(0, result2.Count());
        }

        [Fact]
        public async Task ReadGenericStructIList()
        {
            string json = "[10,20,30]";
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructIListWrapper<int>>(json);
            Assert.Equal(3, wrapper.Count);
            Assert.Equal(10, wrapper[0]);
            Assert.Equal(20, wrapper[1]);
            Assert.Equal(30, wrapper[2]);
        }

        [Fact]
        public async Task ReadNullableGenericStructIList()
        {
            string json = "[10,20,30]";
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructIListWrapper<int>?>(json);
            Assert.True(wrapper.HasValue);
            Assert.Equal(3, wrapper.Value.Count);
            Assert.Equal(10, wrapper.Value[0]);
            Assert.Equal(20, wrapper.Value[1]);
            Assert.Equal(30, wrapper.Value[2]);
        }

        [Fact]
        public async Task ReadNullableGenericStructIListWithNullJson()
        {
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructIListWrapper<int>?>("null");
            Assert.False(wrapper.HasValue);
        }

        [Fact]
        public async Task ReadGenericStructICollection()
        {
            string json = "[10,20,30]";
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructICollectionWrapper<int>>(json);
            Assert.Equal(3, wrapper.Count);
            Assert.Equal(10, wrapper.ElementAt(0));
            Assert.Equal(20, wrapper.ElementAt(1));
            Assert.Equal(30, wrapper.ElementAt(2));
        }

        [Fact]
        public async Task ReadNullableGenericStructICollection()
        {
            string json = "[10,20,30]";
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructICollectionWrapper<int>?>(json);
            Assert.True(wrapper.HasValue);
            Assert.Equal(3, wrapper.Value.Count);
            Assert.Equal(10, wrapper.Value.ElementAt(0));
            Assert.Equal(20, wrapper.Value.ElementAt(1));
            Assert.Equal(30, wrapper.Value.ElementAt(2));
        }

        [Fact]
        public async Task ReadNullableGenericStructICollectionWithNullJson()
        {
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructICollectionWrapper<int>?>("null");
            Assert.False(wrapper.HasValue);
        }

        [Fact]
        public async Task ReadGenericICollectionOfGenericICollection()
        {
            ICollection<ICollection<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<ICollection<ICollection<int>>>(@"[[1,2],[3,4]]");
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
        public async Task ReadGenericICollectionOfArray()
        {
            ICollection<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<ICollection<int[]>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericICollectionWrapper<string[]> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericICollectionWrapper<string[]>>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadArrayOfGenericICollection()
        {
            ICollection<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<ICollection<int>[]>(@"[[1,2],[3,4]]");
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
        public async Task ReadSimpleGenericICollection()
        {
            ICollection<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<ICollection<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<ICollection<int>>(@"[]");
            Assert.Equal(0, result.Count());

            GenericICollectionWrapper<string> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericICollectionWrapper<string>>(@"[""1"",""2""]");
            expected = 1;

            foreach (string str in result2)
            {
                Assert.Equal($"{expected++}", str);
            }

            result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericICollectionWrapper<string>>(@"[]");
            Assert.Equal(0, result2.Count());
        }

        [Fact]
        public async Task ReadGenericIReadOnlyCollectionOfGenericIReadOnlyCollection()
        {
            IReadOnlyCollection<IReadOnlyCollection<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyCollection<IReadOnlyCollection<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IReadOnlyCollection<int> ie in result)
            {
                foreach (int i in ie)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // There's no way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => await JsonSerializerWrapperForString.DeserializeWrapper<GenericIReadOnlyCollectionWrapper<WrapperForIReadOnlyCollectionOfT<string>>>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public async Task ReadGenericIReadOnlyCollectionOfArray()
        {
            IReadOnlyCollection<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyCollection<int[]>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<GenericIReadOnlyCollectionWrapper<int[]>>(@"[[1,2],[3,4]]"));
        }

        [Fact]
        public async Task ReadArrayOfIReadOnlyCollectionT()
        {
            IReadOnlyCollection<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyCollection<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IReadOnlyCollection<int> arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<WrapperForIReadOnlyCollectionOfT<string>[]>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public async Task ReadGenericSimpleIReadOnlyCollection()
        {
            IReadOnlyCollection<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyCollection<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyCollection<int>>(@"[]");
            Assert.Equal(0, result.Count());

            // No way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<WrapperForIReadOnlyCollectionOfT<string>>(@"[""1"",""2""]"));
        }

        [Fact]
        public async Task ReadGenericIReadOnlyListOfGenericIReadOnlyList()
        {
            IReadOnlyList<IReadOnlyList<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyList<IReadOnlyList<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IReadOnlyList<int> ie in result)
            {
                foreach (int i in ie)
                {
                    Assert.Equal(expected++, i);
                }
            }

            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<GenericIReadOnlyListWrapper<StringIReadOnlyListWrapper>>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public async Task ReadGenericIReadOnlyListOfArray()
        {
            IReadOnlyList<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyList<int[]>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<GenericIReadOnlyListWrapper<string[]>>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public async Task ReadArrayOfGenericIReadOnlyList()
        {
            IReadOnlyList<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyList<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IReadOnlyList<int> arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }
            }

            // No way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIReadOnlyListWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]"));
        }

        [Fact]
        public async Task ReadSimpleGenericIReadOnlyList()
        {
            IReadOnlyList<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyList<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyList<int>>(@"[]");
            Assert.Equal(0, result.Count());

            // No way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIReadOnlyListWrapper>(@"[""1"",""2""]"));
        }

        [Fact]
        public async Task ReadGenericISetOfGenericISet()
        {
            ISet<ISet<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<ISet<ISet<int>>>(@"[[1,2],[3,4]]");

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

            GenericISetWrapper<StringISetWrapper> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericISetWrapper<StringISetWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");

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
        public async Task ReadGenericStructISet()
        {
            string json = "[10, 20, 30]";
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructISetWrapper<int>>(json);
            Assert.Equal(3, wrapper.Count);
            Assert.Equal(10, wrapper.ElementAt(0));
            Assert.Equal(20, wrapper.ElementAt(1));
            Assert.Equal(30, wrapper.ElementAt(2));
        }

        [Fact]
        public async Task ReadNullableGenericStructISet()
        {
            string json = "[10, 20, 30]";
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructISetWrapper<int>?>(json);
            Assert.True(wrapper.HasValue);
            Assert.Equal(3, wrapper.Value.Count);
            Assert.Equal(10, wrapper.Value.ElementAt(0));
            Assert.Equal(20, wrapper.Value.ElementAt(1));
            Assert.Equal(30, wrapper.Value.ElementAt(2));
        }

        [Fact]
        public async Task ReadNullableGenericStructISetWithNullJson()
        {
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructISetWrapper<int>?>("null");
            Assert.False(wrapper.HasValue);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50721", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public async Task ReadISetTOfHashSetT()
        {
            ISet<HashSet<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<ISet<HashSet<int>>>(@"[[1,2],[3,4]]");

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
        public async Task ReadHashSetTOfISetT()
        {
            HashSet<ISet<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<HashSet<ISet<int>>>(@"[[1,2],[3,4]]");

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
        public async Task ReadISetTOfArray()
        {
            ISet<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<ISet<int[]>>(@"[[1,2],[3,4]]");

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
        public async Task ReadArrayOfISetT()
        {
            ISet<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<ISet<int>[]>(@"[[1,2],[3,4]]");

            Assert.Equal(new HashSet<int> { 1, 2 }, result.First());
            Assert.Equal(new HashSet<int> { 3, 4 }, result.Last());
        }

        [Fact]
        public async Task ReadSimpleISetT()
        {
            ISet<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<ISet<int>>(@"[1,2]");

            Assert.Equal(new HashSet<int> { 1, 2 }, result);

            result = await JsonSerializerWrapperForString.DeserializeWrapper<ISet<int>>(@"[]");
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task StackTOfStackT()
        {
            Stack<Stack<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<Stack<Stack<int>>>(@"[[1,2],[3,4]]");
            int expected = 4;

            foreach (Stack<int> st in result)
            {
                foreach (int i in st)
                {
                    Assert.Equal(expected--, i);
                }
            }

            GenericStackWrapper<StringStackWrapper> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStackWrapper<StringStackWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadGenericStackOfArray()
        {
            Stack<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<Stack<int[]>>(@"[[1,2],[3,4]]");
            int expected = 3;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }

                expected = 1;
            }

            GenericStackWrapper<string[]> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStackWrapper<string[]>>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadArrayOfGenericStack()
        {
            Stack<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<Stack<int>[]>(@"[[1,2],[3,4]]");
            int expected = 2;

            foreach (Stack<int> st in result)
            {
                foreach (int i in st)
                {
                    Assert.Equal(expected--, i);
                }

                expected = 4;
            }

            StringStackWrapper[] result2 = await JsonSerializerWrapperForString.DeserializeWrapper<StringStackWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadSimpleGenericStack()
        {
            Stack<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<Stack<int>>(@"[1,2]");
            int expected = 2;

            foreach (int i in result)
            {
                Assert.Equal(expected--, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<Stack<int>>(@"[]");
            Assert.Equal(0, result.Count());

            StringStackWrapper result2 = await JsonSerializerWrapperForString.DeserializeWrapper<StringStackWrapper>(@"[""1"",""2""]");
            expected = 2;

            foreach (string str in result2)
            {
                Assert.Equal($"{expected--}", str);
            }

            result2 = await JsonSerializerWrapperForString.DeserializeWrapper<StringStackWrapper>(@"[]");
            Assert.Equal(0, result2.Count());
        }

        [Fact]
        public async Task ReadQueueTOfQueueT()
        {
            Queue<Queue<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<Queue<Queue<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (Queue<int> q in result)
            {
                foreach (int i in q)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericQueueWrapper<StringQueueWrapper> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericQueueWrapper<StringQueueWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadQueueTOfArray()
        {
            Queue<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<Queue<int[]>>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfIQueueT()
        {
            Queue<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<Queue<int>[]>(@"[[1,2],[3,4]]");
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
        public async Task ReadSimpleQueueT()
        {
            Queue<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<Queue<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }
            result = await JsonSerializerWrapperForString.DeserializeWrapper<Queue<int>>(@"[]");
            Assert.Equal(0, result.Count());

        }

        [Fact]
        public async Task ReadHashSetTOfHashSetT()
        {
            HashSet<HashSet<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<HashSet<HashSet<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (HashSet<int> hs in result)
            {
                foreach (int i in hs)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericHashSetWrapper<StringHashSetWrapper> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericHashSetWrapper<StringHashSetWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadHashSetTOfArray()
        {
            HashSet<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<HashSet<int[]>>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfIHashSetT()
        {
            HashSet<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<HashSet<int>[]>(@"[[1,2],[3,4]]");
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
        public async Task ReadSimpleHashSetT()
        {
            HashSet<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<HashSet<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<HashSet<int>>(@"[]");
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task ReadGenericLinkedListOfGenericLinkedList()
        {
            LinkedList<LinkedList<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<LinkedList<LinkedList<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (LinkedList<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }

            GenericLinkedListWrapper<StringLinkedListWrapper> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericLinkedListWrapper<StringLinkedListWrapper>>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadLinkedListTOfArray()
        {
            LinkedList<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<LinkedList<int[]>>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfILinkedListT()
        {
            LinkedList<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<LinkedList<int>[]>(@"[[1,2],[3,4]]");
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
        public async Task ReadSimpleLinkedListT()
        {
            LinkedList<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<LinkedList<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<LinkedList<int>>(@"[]");
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task ReadArrayOfSortedSetT()
        {
            SortedSet<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<SortedSet<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (SortedSet<int> s in result)
            {
                foreach (int i in s)
                {
                    Assert.Equal(expected++, i);
                }
            }

            StringSortedSetWrapper[] result2 = await JsonSerializerWrapperForString.DeserializeWrapper<StringSortedSetWrapper[]>(@"[[""1"",""2""],[""3"",""4""]]");
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
        public async Task ReadSimpleSortedSetT()
        {
            SortedSet<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<SortedSet<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<SortedSet<int>>(@"[]");
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task ReadClass_WithGenericStructCollectionWrapper_NullJson_Throws()
        {
            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithGenericStructIListWrapper>(@"{ ""List"": null }"));
            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithGenericStructICollectionWrapper>(@"{ ""Collection"": null }"));
            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithGenericStructIDictionaryWrapper>(@"{ ""Dictionary"": null }"));
            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithGenericStructISetWrapper>(@"{ ""Set"": null }"));
        }

        [Fact]
        public async Task ReadSimpleTestClass_GenericStructCollectionWrappers()
        {
            SimpleTestClassWithGenericStructCollectionWrappers obj = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClassWithGenericStructCollectionWrappers>(SimpleTestClassWithGenericStructCollectionWrappers.s_json);
            obj.Verify();
        }

        [Fact]
        public async Task ReadSimpleTestStruct_NullableGenericStructCollectionWrappers()
        {
            {
                SimpleTestStructWithNullableGenericStructCollectionWrappers obj = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestStructWithNullableGenericStructCollectionWrappers>(SimpleTestStructWithNullableGenericStructCollectionWrappers.s_json);
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
                SimpleTestStructWithNullableGenericStructCollectionWrappers obj = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestStructWithNullableGenericStructCollectionWrappers>(json);
                Assert.False(obj.List.HasValue);
                Assert.False(obj.Collection.HasValue);
                Assert.False(obj.Set.HasValue);
                Assert.False(obj.Dictionary.HasValue);
            }
        }

        [Fact]
        public async Task ReadSimpleTestClass_GenericCollectionWrappers()
        {
            SimpleTestClassWithGenericCollectionWrappers obj = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClassWithGenericCollectionWrappers>(SimpleTestClassWithGenericCollectionWrappers.s_json);
            obj.Verify();
        }

        [Theory]
        [MemberData(nameof(ReadSimpleTestClass_GenericWrappers_NoAddMethod))]
        public async Task ReadSimpleTestClass_GenericWrappers_NoAddMethod_Throws(Type type, string json, Type exceptionMessageType)
        {
            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper(json, type));
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
        public async Task ReadReadOnlyCollections_Throws(Type type, string json)
        {
            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper(json, type));
            Assert.Contains(type.ToString(), ex.Message);
        }

        [Fact]
        public async Task Read_HigherOrderCollectionInheritance_Works()
        {
            const string json = "[\"test\"]";
            Assert.Equal("test", (await JsonSerializerWrapperForString.DeserializeWrapper<string[]>(json))[0]);
            Assert.Equal("test", (await JsonSerializerWrapperForString.DeserializeWrapper<List<string>>(json)).First());
            Assert.Equal("test", (await JsonSerializerWrapperForString.DeserializeWrapper<StringListWrapper>(json)).First());
            Assert.Equal("test", (await JsonSerializerWrapperForString.DeserializeWrapper<GenericListWrapper<string>>(json)).First());
            Assert.Equal("test", (await JsonSerializerWrapperForString.DeserializeWrapper<MyMyList<string>>(json)).First());
            Assert.Equal("test", (await JsonSerializerWrapperForString.DeserializeWrapper<MyListString>(json)).First());
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
        public async Task Read_Generic_NoPublicConstructor_Throws(Type type, string json)
        {
            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper(json, type));
            Assert.Contains(type.ToString(), ex.Message);
        }

        [Fact]
        public async Task DoesNotCall_CollectionPropertyGetter_EveryTimeElementIsAdded()
        {
            var networkList = new List<string> { "Network1", "Network2" };

            string serialized = await JsonSerializerWrapperForString.SerializeWrapper(new NetworkWrapper { NetworkList = networkList });
            Assert.Equal(@"{""NetworkList"":[""Network1"",""Network2""]}", serialized);

            NetworkWrapper obj = await JsonSerializerWrapperForString.DeserializeWrapper<NetworkWrapper>(serialized);

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
        public async Task CollectionWith_BackingField_CanRoundtrip()
        {
            string json = "{\"AllowedGrantTypes\":[\"client_credentials\"]}";

            Client obj = await JsonSerializerWrapperForString.DeserializeWrapper<Client>(json);
            Assert.Equal("client_credentials", obj.AllowedGrantTypes.First());

            string serialized = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Equal(json, serialized);
        }

        public class Client
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
        public async Task CustomInterfacesNotSupported_Enumerables(Type type)
        {
            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper("[]", type));
            Assert.Contains(type.ToString(), ex.ToString());
        }

        [Theory]
        [MemberData(nameof(CustomInterfaces_Dictionaries))]
        public async Task CustomInterfacesNotSupported_Dictionaries(Type type)
        {
            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper("{}", type));
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
        public async Task IReadOnlyDictionary_NotSupportedKey()
        {
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyDictionary<Uri, int>>(@"{""http://foo"":1}"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(new GenericIReadOnlyDictionaryWrapper<Uri, int>(new Dictionary<Uri, int> { { new Uri("http://foo"), 1 } })));
        }
    }
}
