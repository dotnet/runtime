// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ValueTests
    {
        [Fact]
        public static void WriteIEnumerableOfIEnumerable()
        {
            IEnumerable input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        public static void WriteGenericIEnumerableOfIEnumerable()
        {
            IEnumerable<IEnumerable> input = new List<IEnumerable>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIEnumerable()
        {
            IEnumerable[] input = new IEnumerable[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIEnumerable()
        {
            IEnumerable input = new List<int> { 1, 2 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteIListOfIList()
        {
            IList input = new List<IList>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        public static void WriteIListGenericOfIList()
        {
            IList<IList> input = new List<IList>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIList()
        {
            IList[] input = new IList[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIList()
        {
            IList input = new List<int> { 1, 2 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteICollectionOfICollection()
        {
            ICollection input = new List<ICollection>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        public static void WriteGenericICollectionOfICollection()
        {
            ICollection<ICollection> input = new List<ICollection>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfICollection()
        {
            ICollection[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveICollection()
        {
            ICollection input = new List<int> { 1, 2 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteStackOfStack()
        {
            Stack input = new Stack();
            input.Push(new Stack(new List<int>() { 1, 2 }));
            input.Push(new Stack(new List<int>() { 3, 4 }));

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[4,3],[2,1]]", json);
        }

        public static void WriteGenericStackOfStack()
        {
            Stack<Stack> input = new Stack<Stack>();
            input.Push(new Stack(new List<int>() { 1, 2 }));
            input.Push(new Stack(new List<int>() { 3, 4 }));

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[4,3],[2,1]]", json);
        }

        [Fact]
        public static void WriteArrayOfStack()
        {
            Stack[] input = new Stack[2];
            input[0] = new Stack(new List<int>() { 1, 2 });
            input[1] = new Stack(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[2,1],[4,3]]", json);
        }

        [Fact]
        public static void WritePrimitiveStack()
        {
            Stack input = new Stack( new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[2,1]", json);
        }

        [Fact]
        public static void WriteQueueOfQueue()
        {
            Queue input = new Queue();
            input.Enqueue(new Queue(new List<int>() { 1, 2 }));
            input.Enqueue(new Queue(new List<int>() { 3, 4 }));

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        public static void WriteGenericQueueOfQueue()
        {
            Queue<Queue> input = new Queue<Queue>();
            input.Enqueue(new Queue(new List<int>() { 1, 2 }));
            input.Enqueue(new Queue(new List<int>() { 3, 4 }));

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfQueue()
        {
            Queue[] input = new Queue[2];
            input[0] = new Queue(new List<int>() { 1, 2 });
            input[1] = new Queue(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveQueue()
        {
            Queue input = new Queue(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteArrayListOfArrayList()
        {
            ArrayList input = new ArrayList
            {
                new ArrayList(new List<int>() { 1, 2 }),
                new ArrayList(new List<int>() { 3, 4 })
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfArrayList()
        {
            ArrayList[] input = new ArrayList[2];
            input[0] = new ArrayList(new List<int>() { 1, 2 });
            input[1] = new ArrayList(new List<int>() { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveArrayList()
        {
            ArrayList input = new ArrayList(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }
    }
}
