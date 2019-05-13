// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ValueTests
    {
        [Fact]
        public static void WriteIImmutableListTOfIImmutableListT()
        {
            IImmutableList<IImmutableList<int>> input = ImmutableList.CreateRange(new List<IImmutableList<int>>{
                ImmutableList.CreateRange(new List<int>() { 1, 2 }),
                ImmutableList.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteIImmutableListTOfArray()
        {
            IImmutableList<int[]> input = ImmutableList.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIImmutableListT()
        {
            IImmutableList<int>[] input = new IImmutableList<int>[2];
            input[0] = ImmutableList.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableList.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIImmutableListT()
        {
            IImmutableList<int> input = ImmutableList.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteIImmutableStackTOfIImmutableStackT()
        {
            IImmutableStack<IImmutableStack<int>> input = ImmutableStack.CreateRange(new List<IImmutableStack<int>>{
                ImmutableStack.CreateRange(new List<int>() { 1, 2 }),
                ImmutableStack.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[4,3],[2,1]]", json);
        }

        [Fact]
        public static void WriteIImmutableStackTOfArray()
        {
            IImmutableStack<int[]> input = ImmutableStack.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[3,4],[1,2]]", json);
        }

        [Fact]
        public static void WriteArrayOfIImmutableStackT()
        {
            IImmutableStack<int>[] input = new IImmutableStack<int>[2];
            input[0] = ImmutableStack.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableStack.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[2,1],[4,3]]", json);
        }

        [Fact]
        public static void WritePrimitiveIImmutableStackT()
        {
            IImmutableStack<int> input = ImmutableStack.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[2,1]", json);
        }

        [Fact]
        public static void WriteIImmutableQueueTOfIImmutableQueueT()
        {
            IImmutableQueue<IImmutableQueue<int>> input = ImmutableQueue.CreateRange(new List<IImmutableQueue<int>>{
                ImmutableQueue.CreateRange(new List<int>() { 1, 2 }),
                ImmutableQueue.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteIImmutableQueueTOfArray()
        {
            IImmutableQueue<int[]> input = ImmutableQueue.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIImmutableQueueT()
        {
            IImmutableQueue<int>[] input = new IImmutableQueue<int>[2];
            input[0] = ImmutableQueue.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableQueue.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIImmutableQueueT()
        {
            IImmutableQueue<int> input = ImmutableQueue.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteIImmutableSetTOfIImmutableSetT()
        {
            IImmutableSet<IImmutableSet<int>> input = ImmutableHashSet.CreateRange(new List<IImmutableSet<int>>{
                ImmutableHashSet.CreateRange(new List<int>() { 1, 2 }),
                ImmutableHashSet.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.True(json.Contains("[1,2]"));
            Assert.True(json.Contains("[3,4]"));
        }

        [Fact]
        public static void WriteIImmutableSetTOfArray()
        {
            IImmutableSet<int[]> input = ImmutableHashSet.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.True(json.Contains("[1,2]"));
            Assert.True(json.Contains("[3,4]"));
        }

        [Fact]
        public static void WriteArrayOfIImmutableSetT()
        {
            IImmutableSet<int>[] input = new IImmutableSet<int>[2];
            input[0] = ImmutableHashSet.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableHashSet.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIImmutableSetT()
        {
            IImmutableSet<int> input = ImmutableHashSet.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteImmutableHashSetTOfImmutableHashSetT()
        {
            ImmutableHashSet<ImmutableHashSet<int>> input = ImmutableHashSet.CreateRange(new List<ImmutableHashSet<int>>{
                ImmutableHashSet.CreateRange(new List<int>() { 1, 2 }),
                ImmutableHashSet.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.True(json.Contains("[1,2]"));
            Assert.True(json.Contains("[3,4]"));
        }

        [Fact]
        public static void WriteImmutableHashSetTOfArray()
        {
            ImmutableHashSet<int[]> input = ImmutableHashSet.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.True(json.Contains("[1,2]"));
            Assert.True(json.Contains("[3,4]"));
        }

        [Fact]
        public static void WriteArrayOfImmutableHashSetT()
        {
            ImmutableHashSet<int>[] input = new ImmutableHashSet<int>[2];
            input[0] = ImmutableHashSet.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableHashSet.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveImmutableHashSetT()
        {
            ImmutableHashSet<int> input = ImmutableHashSet.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteImmutableListTOfImmutableListT()
        {
            ImmutableList<ImmutableList<int>> input = ImmutableList.CreateRange(new List<ImmutableList<int>>{
                ImmutableList.CreateRange(new List<int>() { 1, 2 }),
                ImmutableList.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteImmutableListTOfArray()
        {
            ImmutableList<int[]> input = ImmutableList.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfImmutableListT()
        {
            ImmutableList<int>[] input = new ImmutableList<int>[2];
            input[0] = ImmutableList.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableList.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveImmutableListT()
        {
            ImmutableList<int> input = ImmutableList.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteImmutableStackTOfImmutableStackT()
        {
            ImmutableStack<ImmutableStack<int>> input = ImmutableStack.CreateRange(new List<ImmutableStack<int>>{
                ImmutableStack.CreateRange(new List<int>() { 1, 2 }),
                ImmutableStack.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[4,3],[2,1]]", json);
        }

        [Fact]
        public static void WriteImmutableStackTOfArray()
        {
            ImmutableStack<int[]> input = ImmutableStack.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[3,4],[1,2]]", json);
        }

        [Fact]
        public static void WriteArrayOfImmutableStackT()
        {
            ImmutableStack<int>[] input = new ImmutableStack<int>[2];
            input[0] = ImmutableStack.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableStack.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[2,1],[4,3]]", json);
        }

        [Fact]
        public static void WritePrimitiveImmutableStackT()
        {
            ImmutableStack<int> input = ImmutableStack.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[2,1]", json);
        }

        [Fact]
        public static void WriteImmutableQueueTOfImmutableQueueT()
        {
            ImmutableQueue<ImmutableQueue<int>> input = ImmutableQueue.CreateRange(new List<ImmutableQueue<int>>{
                ImmutableQueue.CreateRange(new List<int>() { 1, 2 }),
                ImmutableQueue.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteImmutableQueueTOfArray()
        {
            ImmutableQueue<int[]> input = ImmutableQueue.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfImmutableQueueT()
        {
            ImmutableQueue<int>[] input = new ImmutableQueue<int>[2];
            input[0] = ImmutableQueue.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableQueue.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveImmutableQueueT()
        {
            ImmutableQueue<int> input = ImmutableQueue.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteArrayOfImmutableSortedSetT()
        {
            ImmutableSortedSet<int>[] input = new ImmutableSortedSet<int>[2];
            input[0] = ImmutableSortedSet.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableSortedSet.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveImmutableSortedSetT()
        {
            ImmutableSortedSet<int> input = ImmutableSortedSet.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }
    }
}
