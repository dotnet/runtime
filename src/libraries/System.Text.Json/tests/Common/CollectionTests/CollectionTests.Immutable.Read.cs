// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public async Task ReadImmutableArrayOfImmutableArray()
        {
            ImmutableArray<ImmutableArray<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableArray<ImmutableArray<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (ImmutableArray<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadImmutableArrayOfArray()
        {
            ImmutableArray<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableArray<int[]>>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfImmutableArray()
        {
            ImmutableArray<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableArray<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (ImmutableArray<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadSimpleImmutableArray()
        {
            ImmutableArray<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableArray<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableArray<int>>(@"[]");
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task ReadSimpleClassWithImmutableArray()
        {
            SimpleTestClassWithImmutableArray obj = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClassWithImmutableArray>(SimpleTestClassWithImmutableArray.s_json);
            obj.Verify();
        }

        [Fact]
        public async Task ReadIImmutableListTOfIImmutableListT()
        {
            IImmutableList<IImmutableList<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableList<IImmutableList<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IImmutableList<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadIImmutableListTOfArray()
        {
            IImmutableList<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableList<int[]>>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfIIImmutableListT()
        {
            IImmutableList<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableList<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IImmutableList<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadPrimitiveIImmutableListT()
        {
            IImmutableList<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableList<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableList<int>>(@"[]");
            Assert.Equal(0, result.Count());

            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIImmutableListWrapper>(@"[""1"",""2""]"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIImmutableListWrapper>(@"[]"));
        }

        [Fact]
        public async Task ReadIImmutableStackTOfIImmutableStackT()
        {
            IImmutableStack<IImmutableStack<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableStack<IImmutableStack<int>>>(@"[[1,2],[3,4]]");
            int expected = 4;

            foreach (IImmutableStack<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected--, i);
                }
            }
        }

        [Fact]
        public async Task ReadIImmutableStackTOfArray()
        {
            IImmutableStack<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableStack<int[]>>(@"[[1,2],[3,4]]");
            int expected = 3;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }

                expected = 1;
            }
        }

        [Fact]
        public async Task ReadArrayOfIIImmutableStackT()
        {
            IImmutableStack<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableStack<int>[]>(@"[[1,2],[3,4]]");
            int expected = 2;

            foreach (IImmutableStack<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected--, i);
                }

                expected = 4;
            }
        }

        [Fact]
        public async Task ReadPrimitiveIImmutableStackT()
        {
            IImmutableStack<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableStack<int>>(@"[1,2]");
            int expected = 2;

            foreach (int i in result)
            {
                Assert.Equal(expected--, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableStack<int>>(@"[]");
            Assert.Equal(0, result.Count());

            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIImmutableStackWrapper>(@"[""1"",""2""]"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIImmutableStackWrapper>(@"[]"));
        }

        [Fact]
        public async Task ReadIImmutableQueueTOfIImmutableQueueT()
        {
            IImmutableQueue<IImmutableQueue<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableQueue<IImmutableQueue<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IImmutableQueue<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadIImmutableQueueTOfArray()
        {
            IImmutableQueue<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableQueue<int[]>>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfIImmutableQueueT()
        {
            IImmutableQueue<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableQueue<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IImmutableQueue<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadPrimitiveIImmutableQueueT()
        {
            IImmutableQueue<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableQueue<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableQueue<int>>(@"[]");
            Assert.Equal(0, result.Count());

            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIImmutableQueueWrapper>(@"[""1"",""2""]"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIImmutableQueueWrapper>(@"[]"));
        }

        [Fact]
        public async Task ReadIImmutableSetTOfIImmutableSetT()
        {
            IImmutableSet<IImmutableSet<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableSet<IImmutableSet<int>>>(@"[[1,2],[3,4]]");
            List<int> expected = new List<int> { 1, 2, 3, 4 };

            foreach (IImmutableSet<int> l in result)
            {
                foreach (int i in l)
                {
                    expected.Remove(i);
                }
            }

            Assert.Equal(0, expected.Count);
        }

        [Fact]
        public async Task ReadIImmutableSetTOfArray()
        {
            IImmutableSet<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableSet<int[]>>(@"[[1,2],[3,4]]");
            List<int> expected = new List<int> { 1, 2, 3, 4 };

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    expected.Remove(i);
                }
            }

            Assert.Equal(0, expected.Count);
        }

        [Fact]
        public async Task ReadArrayOfIImmutableSetT()
        {
            IImmutableSet<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableSet<int>[]>(@"[[1,2],[3,4]]");
            List<int> expected = new List<int> { 1, 2, 3, 4 };

            foreach (IImmutableSet<int> l in result)
            {
                foreach (int i in l)
                {
                    expected.Remove(i);
                }
            }

            Assert.Equal(0, expected.Count);
        }

        [Fact]
        public async Task ReadPrimitiveIImmutableSetT()
        {
            IImmutableSet<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableSet<int>>(@"[1,2]");
            List<int> expected = new List<int> { 1, 2 };

            foreach (int i in result)
            {
                expected.Remove(i);
            }

            Assert.Equal(0, expected.Count);

            result = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableSet<int>>(@"[]");
            Assert.Equal(0, result.Count());

            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIImmutableSetWrapper>(@"[""1"",""2""]"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringIImmutableSetWrapper>(@"[]"));
        }

        [Fact]
        public async Task ReadImmutableHashSetTOfImmutableHashSetT()
        {
            ImmutableHashSet<ImmutableHashSet<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableHashSet<ImmutableHashSet<int>>>(@"[[1,2],[3,4]]");
            List<int> expected = new List<int> { 1, 2, 3, 4 };

            foreach (ImmutableHashSet<int> l in result)
            {
                foreach (int i in l)
                {
                    expected.Remove(i);
                }
            }

            Assert.Equal(0, expected.Count);
        }

        [Fact]
        public async Task ReadImmutableHashSetTOfArray()
        {
            ImmutableHashSet<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableHashSet<int[]>>(@"[[1,2],[3,4]]");
            List<int> expected = new List<int> { 1, 2, 3, 4 };

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    expected.Remove(i);
                }
            }

            Assert.Equal(0, expected.Count);
        }

        [Fact]
        public async Task ReadArrayOfIImmutableHashSetT()
        {
            ImmutableHashSet<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableHashSet<int>[]>(@"[[1,2],[3,4]]");
            List<int> expected = new List<int> { 1, 2, 3, 4 };

            foreach (ImmutableHashSet<int> l in result)
            {
                foreach (int i in l)
                {
                    expected.Remove(i);
                }
            }

            Assert.Equal(0, expected.Count);
        }

        [Fact]
        public async Task ReadPrimitiveImmutableHashSetT()
        {
            ImmutableHashSet<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableHashSet<int>>(@"[1,2]");
            List<int> expected = new List<int> { 1, 2 };

            foreach (int i in result)
            {
                expected.Remove(i);
            }

            Assert.Equal(0, expected.Count);

            result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableHashSet<int>>(@"[]");
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task ReadImmutableListTOfImmutableListT()
        {
            ImmutableList<ImmutableList<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableList<ImmutableList<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (ImmutableList<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadImmutableListTOfArray()
        {
            ImmutableList<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableList<int[]>>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfIImmutableListT()
        {
            ImmutableList<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableList<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (ImmutableList<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadPrimitiveImmutableListT()
        {
            ImmutableList<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableList<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableList<int>>(@"[]");
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task ReadImmutableStackTOfImmutableStackT()
        {
            ImmutableStack<ImmutableStack<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableStack<ImmutableStack<int>>>(@"[[1,2],[3,4]]");
            int expected = 4;

            foreach (ImmutableStack<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected--, i);
                }
            }
        }

        [Fact]
        public async Task ReadImmutableStackTOfArray()
        {
            ImmutableStack<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableStack<int[]>>(@"[[1,2],[3,4]]");
            int expected = 3;

            foreach (int[] arr in result)
            {
                foreach (int i in arr)
                {
                    Assert.Equal(expected++, i);
                }

                expected = 1;
            }
        }

        [Fact]
        public async Task ReadArrayOfIImmutableStackT()
        {
            ImmutableStack<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableStack<int>[]>(@"[[1,2],[3,4]]");
            int expected = 2;

            foreach (ImmutableStack<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected--, i);
                }

                expected = 4;
            }
        }

        [Fact]
        public async Task ReadPrimitiveImmutableStackT()
        {
            ImmutableStack<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableStack<int>>(@"[1,2]");
            int expected = 2;

            foreach (int i in result)
            {
                Assert.Equal(expected--, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableStack<int>>(@"[]");
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task ReadImmutableQueueTOfImmutableQueueT()
        {
            ImmutableQueue<ImmutableQueue<int>> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableQueue<ImmutableQueue<int>>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (ImmutableQueue<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadImmutableQueueTOfArray()
        {
            ImmutableQueue<int[]> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableQueue<int[]>>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfImmutableQueueT()
        {
            ImmutableQueue<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableQueue<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (ImmutableQueue<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadPrimitiveImmutableQueueT()
        {
            ImmutableQueue<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableQueue<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableQueue<int>>(@"[]");
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task ReadArrayOfIImmutableSortedSetT()
        {
            ImmutableSortedSet<int>[] result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableSortedSet<int>[]>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (ImmutableSortedSet<int> l in result)
            {
                foreach (int i in l)
                {
                    Assert.Equal(expected++, i);
                }
            }
        }

        [Fact]
        public async Task ReadPrimitiveImmutableSortedSetT()
        {
            ImmutableSortedSet<int> result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableSortedSet<int>>(@"[1,2]");
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableSortedSet<int>>(@"[]");
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task ReadSimpleTestClass_ImmutableCollectionWrappers_Throws()
        {
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClassWithIImmutableDictionaryWrapper>(SimpleTestClassWithIImmutableDictionaryWrapper.s_json));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClassWithImmutableListWrapper>(SimpleTestClassWithImmutableListWrapper.s_json));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClassWithImmutableStackWrapper>(SimpleTestClassWithImmutableStackWrapper.s_json));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClassWithImmutableQueueWrapper>(SimpleTestClassWithImmutableQueueWrapper.s_json));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClassWithImmutableSetWrapper>(SimpleTestClassWithImmutableSetWrapper.s_json));
        }
    }
}
