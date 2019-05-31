// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ValueTests
    {
        [Fact]
        public static void ReadIImmutableListTOfIImmutableListT()
        {
            IImmutableList<IImmutableList<int>> result = JsonSerializer.Parse<IImmutableList<IImmutableList<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadIImmutableListTOfArray()
        {
            IImmutableList<int[]> result = JsonSerializer.Parse<IImmutableList<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadArrayOfIIImmutableListT()
        {
            IImmutableList<int>[] result = JsonSerializer.Parse<IImmutableList<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadPrimitiveIImmutableListT()
        {
            IImmutableList<int> result = JsonSerializer.Parse<IImmutableList<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Parse<IImmutableList<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadIImmutableStackTOfIImmutableStackT()
        {
            IImmutableStack<IImmutableStack<int>> result = JsonSerializer.Parse<IImmutableStack<IImmutableStack<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadIImmutableStackTOfArray()
        {
            IImmutableStack<int[]> result = JsonSerializer.Parse<IImmutableStack<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadArrayOfIIImmutableStackT()
        {
            IImmutableStack<int>[] result = JsonSerializer.Parse<IImmutableStack<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadPrimitiveIImmutableStackT()
        {
            IImmutableStack<int> result = JsonSerializer.Parse<IImmutableStack<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 2;

            foreach (int i in result)
            {
                Assert.Equal(expected--, i);
            }

            result = JsonSerializer.Parse<IImmutableStack<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadIImmutableQueueTOfIImmutableQueueT()
        {
            IImmutableQueue<IImmutableQueue<int>> result = JsonSerializer.Parse<IImmutableQueue<IImmutableQueue<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadIImmutableQueueTOfArray()
        {
            IImmutableQueue<int[]> result = JsonSerializer.Parse<IImmutableQueue<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadArrayOfIIImmutableQueueT()
        {
            IImmutableQueue<int>[] result = JsonSerializer.Parse<IImmutableQueue<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadPrimitiveIImmutableQueueT()
        {
            IImmutableQueue<int> result = JsonSerializer.Parse<IImmutableQueue<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Parse<IImmutableQueue<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadIImmutableSetTOfIImmutableSetT()
        {
            IImmutableSet<IImmutableSet<int>> result = JsonSerializer.Parse<IImmutableSet<IImmutableSet<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadIImmutableSetTOfArray()
        {
            IImmutableSet<int[]> result = JsonSerializer.Parse<IImmutableSet<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadArrayOfIIImmutableSetT()
        {
            IImmutableSet<int>[] result = JsonSerializer.Parse<IImmutableSet<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadPrimitiveIImmutableSetT()
        {
            IImmutableSet<int> result = JsonSerializer.Parse<IImmutableSet<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            List<int> expected = new List<int> { 1, 2 };

            foreach (int i in result)
            {
                expected.Remove(i);
            }

            Assert.Equal(0, expected.Count);

            result = JsonSerializer.Parse<IImmutableSet<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadImmutableHashSetTOfImmutableHashSetT()
        {
            ImmutableHashSet<ImmutableHashSet<int>> result = JsonSerializer.Parse<ImmutableHashSet<ImmutableHashSet<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadImmutableHashSetTOfArray()
        {
            ImmutableHashSet<int[]> result = JsonSerializer.Parse<ImmutableHashSet<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadArrayOfIImmutableHashSetT()
        {
            ImmutableHashSet<int>[] result = JsonSerializer.Parse<ImmutableHashSet<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadPrimitiveImmutableHashSetT()
        {
            ImmutableHashSet<int> result = JsonSerializer.Parse<ImmutableHashSet<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            List<int> expected = new List<int> { 1, 2 };

            foreach (int i in result)
            {
                expected.Remove(i);
            }

            Assert.Equal(0, expected.Count);

            result = JsonSerializer.Parse<ImmutableHashSet<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadImmutableListTOfImmutableListT()
        {
            ImmutableList<ImmutableList<int>> result = JsonSerializer.Parse<ImmutableList<ImmutableList<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadImmutableListTOfArray()
        {
            ImmutableList<int[]> result = JsonSerializer.Parse<ImmutableList<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadArrayOfIImmutableListT()
        {
            ImmutableList<int>[] result = JsonSerializer.Parse<ImmutableList<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadPrimitiveImmutableListT()
        {
            ImmutableList<int> result = JsonSerializer.Parse<ImmutableList<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Parse<ImmutableList<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadImmutableStackTOfImmutableStackT()
        {
            ImmutableStack<ImmutableStack<int>> result = JsonSerializer.Parse<ImmutableStack<ImmutableStack<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadImmutableStackTOfArray()
        {
            ImmutableStack<int[]> result = JsonSerializer.Parse<ImmutableStack<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadArrayOfIImmutableStackT()
        {
            ImmutableStack<int>[] result = JsonSerializer.Parse<ImmutableStack<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadPrimitiveImmutableStackT()
        {
            ImmutableStack<int> result = JsonSerializer.Parse<ImmutableStack<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 2;

            foreach (int i in result)
            {
                Assert.Equal(expected--, i);
            }

            result = JsonSerializer.Parse<ImmutableStack<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadImmutableQueueTOfImmutableQueueT()
        {
            ImmutableQueue<ImmutableQueue<int>> result = JsonSerializer.Parse<ImmutableQueue<ImmutableQueue<int>>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadImmutableQueueTOfArray()
        {
            ImmutableQueue<int[]> result = JsonSerializer.Parse<ImmutableQueue<int[]>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadArrayOfIImmutableQueueT()
        {
            ImmutableQueue<int>[] result = JsonSerializer.Parse<ImmutableQueue<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadPrimitiveImmutableQueueT()
        {
            ImmutableQueue<int> result = JsonSerializer.Parse<ImmutableQueue<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Parse<ImmutableQueue<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadArrayOfIImmutableSortedSetT()
        {
            ImmutableSortedSet<int>[] result = JsonSerializer.Parse<ImmutableSortedSet<int>[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadPrimitiveImmutableSortedSetT()
        {
            ImmutableSortedSet<int> result = JsonSerializer.Parse<ImmutableSortedSet<int>>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (int i in result)
            {
                Assert.Equal(expected++, i);
            }

            result = JsonSerializer.Parse<ImmutableSortedSet<int>>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public static void ReadPrimitiveImmutableArrayThrows()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Parse<ImmutableArray<int>>(Encoding.UTF8.GetBytes(@"[1,2]")));
        }
    }
}
