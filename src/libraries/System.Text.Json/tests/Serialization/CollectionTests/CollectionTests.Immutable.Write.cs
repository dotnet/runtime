// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
    {
        [Fact]
        public static void WriteImmutableArrayOfImmutableArray()
        {
            ImmutableArray<ImmutableArray<int>> input = ImmutableArray.CreateRange(new List<ImmutableArray<int>>{
                ImmutableArray.CreateRange(new List<int>() { 1, 2 }),
                ImmutableArray.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteImmutableArrayOfArray()
        {
            ImmutableArray<int[]> input = ImmutableArray.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfImmutableArray()
        {
            ImmutableArray<int>[] input = new ImmutableArray<int>[2];
            input[0] = ImmutableArray.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableArray.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteSimpleImmutableArray()
        {
            ImmutableArray<int> input = ImmutableArray.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteIImmutableListTOfIImmutableListT()
        {
            IImmutableList<IImmutableList<int>> input = ImmutableList.CreateRange(new List<IImmutableList<int>>{
                ImmutableList.CreateRange(new List<int>() { 1, 2 }),
                ImmutableList.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteSimpleClassWithImmutableArray()
        {
            SimpleTestClassWithImmutableArray obj = new SimpleTestClassWithImmutableArray();
            obj.Initialize();

            Assert.Equal(SimpleTestClassWithImmutableArray.s_json, JsonSerializer.Serialize(obj));
        }

        [Fact]
        public static void WriteSimpleClassWithObjectImmutableArray()
        {
            SimpleTestClassWithObjectImmutableArray obj = new SimpleTestClassWithObjectImmutableArray();
            obj.Initialize();

            Assert.Equal(SimpleTestClassWithObjectImmutableArray.s_json, JsonSerializer.Serialize(obj));
        }

        [Fact]
        public static void WriteArrayOfIImmutableListT()
        {
            IImmutableList<int>[] input = new IImmutableList<int>[2];
            input[0] = ImmutableList.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableList.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIImmutableListT()
        {
            IImmutableList<int> input = ImmutableList.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);

            StringIImmutableListWrapper input2 = new StringIImmutableListWrapper(new List<string> { "1", "2" });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[""1"",""2""]", json);
        }

        [Fact]
        public static void WriteIImmutableStackTOfIImmutableStackT()
        {
            IImmutableStack<IImmutableStack<int>> input = ImmutableStack.CreateRange(new List<IImmutableStack<int>>{
                ImmutableStack.CreateRange(new List<int>() { 1, 2 }),
                ImmutableStack.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[3,4],[1,2]]", json);
        }

        [Fact]
        public static void WriteArrayOfIImmutableStackT()
        {
            IImmutableStack<int>[] input = new IImmutableStack<int>[2];
            input[0] = ImmutableStack.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableStack.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[2,1],[4,3]]", json);
        }

        [Fact]
        public static void WritePrimitiveIImmutableStackT()
        {
            IImmutableStack<int> input = ImmutableStack.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[2,1]", json);

            StringIImmutableStackWrapper input2 = new StringIImmutableStackWrapper(new List<string> { "1", "2" });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[""2"",""1""]", json);
        }

        [Fact]
        public static void WriteIImmutableQueueTOfIImmutableQueueT()
        {
            IImmutableQueue<IImmutableQueue<int>> input = ImmutableQueue.CreateRange(new List<IImmutableQueue<int>>{
                ImmutableQueue.CreateRange(new List<int>() { 1, 2 }),
                ImmutableQueue.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIImmutableQueueT()
        {
            IImmutableQueue<int>[] input = new IImmutableQueue<int>[2];
            input[0] = ImmutableQueue.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableQueue.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIImmutableQueueT()
        {
            IImmutableQueue<int> input = ImmutableQueue.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);

            StringIImmutableQueueWrapper input2 = new StringIImmutableQueueWrapper(new List<string> { "1", "2" });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[""1"",""2""]", json);
        }

        [Fact]
        public static void WriteIImmutableSetTOfIImmutableSetT()
        {
            IImmutableSet<IImmutableSet<int>> input = ImmutableHashSet.CreateRange(new List<IImmutableSet<int>>{
                ImmutableHashSet.CreateRange(new List<int>() { 1, 2 }),
                ImmutableHashSet.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.Serialize(input);
            Assert.Contains("[1,2]", json);
            Assert.Contains("[3,4]", json);
        }

        [Fact]
        public static void WriteIImmutableSetTOfArray()
        {
            IImmutableSet<int[]> input = ImmutableHashSet.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.Serialize(input);
            Assert.Contains("[1,2]", json);
            Assert.Contains("[3,4]", json);
        }

        [Fact]
        public static void WriteArrayOfIImmutableSetT()
        {
            IImmutableSet<int>[] input = new IImmutableSet<int>[2];
            input[0] = ImmutableHashSet.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableHashSet.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIImmutableSetT()
        {
            IImmutableSet<int> input = ImmutableHashSet.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);

            StringIImmutableSetWrapper input2 = new StringIImmutableSetWrapper(new List<string> { "1", "2" });

            json = JsonSerializer.Serialize(input2);
            Assert.True(json == @"[""1"",""2""]" || json == @"[""2"",""1""]");
        }

        [Fact]
        public static void WriteImmutableHashSetTOfImmutableHashSetT()
        {
            ImmutableHashSet<ImmutableHashSet<int>> input = ImmutableHashSet.CreateRange(new List<ImmutableHashSet<int>>{
                ImmutableHashSet.CreateRange(new List<int>() { 1, 2 }),
                ImmutableHashSet.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.Serialize(input);
            Assert.Contains("[1,2]", json);
            Assert.Contains("[3,4]", json);
        }

        [Fact]
        public static void WriteImmutableHashSetTOfArray()
        {
            ImmutableHashSet<int[]> input = ImmutableHashSet.CreateRange(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.Serialize(input);
            Assert.Contains("[1,2]", json);
            Assert.Contains("[3,4]", json);
        }

        [Fact]
        public static void WriteArrayOfImmutableHashSetT()
        {
            ImmutableHashSet<int>[] input = new ImmutableHashSet<int>[2];
            input[0] = ImmutableHashSet.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableHashSet.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveImmutableHashSetT()
        {
            ImmutableHashSet<int> input = ImmutableHashSet.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteImmutableListTOfImmutableListT()
        {
            ImmutableList<ImmutableList<int>> input = ImmutableList.CreateRange(new List<ImmutableList<int>>{
                ImmutableList.CreateRange(new List<int>() { 1, 2 }),
                ImmutableList.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfImmutableListT()
        {
            ImmutableList<int>[] input = new ImmutableList<int>[2];
            input[0] = ImmutableList.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableList.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveImmutableListT()
        {
            ImmutableList<int> input = ImmutableList.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteImmutableStackTOfImmutableStackT()
        {
            ImmutableStack<ImmutableStack<int>> input = ImmutableStack.CreateRange(new List<ImmutableStack<int>>{
                ImmutableStack.CreateRange(new List<int>() { 1, 2 }),
                ImmutableStack.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[3,4],[1,2]]", json);
        }

        [Fact]
        public static void WriteArrayOfImmutableStackT()
        {
            ImmutableStack<int>[] input = new ImmutableStack<int>[2];
            input[0] = ImmutableStack.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableStack.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[2,1],[4,3]]", json);
        }

        [Fact]
        public static void WritePrimitiveImmutableStackT()
        {
            ImmutableStack<int> input = ImmutableStack.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[2,1]", json);
        }

        [Fact]
        public static void WriteImmutableQueueTOfImmutableQueueT()
        {
            ImmutableQueue<ImmutableQueue<int>> input = ImmutableQueue.CreateRange(new List<ImmutableQueue<int>>{
                ImmutableQueue.CreateRange(new List<int>() { 1, 2 }),
                ImmutableQueue.CreateRange(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfImmutableQueueT()
        {
            ImmutableQueue<int>[] input = new ImmutableQueue<int>[2];
            input[0] = ImmutableQueue.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableQueue.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveImmutableQueueT()
        {
            ImmutableQueue<int> input = ImmutableQueue.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteArrayOfImmutableSortedSetT()
        {
            ImmutableSortedSet<int>[] input = new ImmutableSortedSet<int>[2];
            input[0] = ImmutableSortedSet.CreateRange(new List<int>() { 1, 2 });
            input[1] = ImmutableSortedSet.CreateRange(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveImmutableSortedSetT()
        {
            ImmutableSortedSet<int> input = ImmutableSortedSet.CreateRange(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteImmutableCollectionWrappers()
        {
            SimpleTestClassWithIImmutableDictionaryWrapper obj1 = new SimpleTestClassWithIImmutableDictionaryWrapper();
            SimpleTestClassWithImmutableListWrapper obj2 = new SimpleTestClassWithImmutableListWrapper();
            SimpleTestClassWithImmutableStackWrapper obj3 = new SimpleTestClassWithImmutableStackWrapper();
            SimpleTestClassWithImmutableQueueWrapper obj4 = new SimpleTestClassWithImmutableQueueWrapper();
            SimpleTestClassWithImmutableSetWrapper obj5 = new SimpleTestClassWithImmutableSetWrapper();

            obj1.Initialize();
            obj2.Initialize();
            obj3.Initialize();
            obj4.Initialize();
            obj5.Initialize();

            Assert.Equal(SimpleTestClassWithIImmutableDictionaryWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj1));
            Assert.Equal(SimpleTestClassWithIImmutableDictionaryWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj1));

            Assert.Equal(SimpleTestClassWithImmutableListWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj2));
            Assert.Equal(SimpleTestClassWithImmutableListWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj2));

            Assert.Equal(SimpleTestClassWithImmutableStackWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj3));
            Assert.Equal(SimpleTestClassWithImmutableStackWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj3));

            Assert.Equal(SimpleTestClassWithImmutableQueueWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj4));
            Assert.Equal(SimpleTestClassWithImmutableQueueWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj4));

            Assert.Equal(SimpleTestClassWithImmutableSetWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj5));
            Assert.Equal(SimpleTestClassWithImmutableSetWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj5));
        }
    }
}
