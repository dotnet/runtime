// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ValueTests
    {
        public static void ReadGenericIEnumerableOfIEnumerable()
        {
            IEnumerable<IEnumerable> result = JsonSerializer.Parse<IEnumerable<IEnumerable>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IEnumerable ie in result)
            {
                foreach (JsonElement i in ie)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadIEnumerableOfArray()
        {
            IEnumerable result = JsonSerializer.Parse<IEnumerable>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (JsonElement arr in result)
            {
                foreach (JsonElement i in arr.EnumerateArray())
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadArrayOfIEnumerable()
        {
            IEnumerable[] result = JsonSerializer.Parse<IEnumerable[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IEnumerable arr in result)
            {
                foreach (JsonElement i in arr)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadPrimitiveIEnumerable()
        {
            IEnumerable result = JsonSerializer.Parse<IEnumerable>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = JsonSerializer.Parse<IEnumerable>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        public static void ReadGenericIListOfIList()
        {
            IList<IList> result = JsonSerializer.Parse<IList<IList>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IList list in result)
            {
                foreach (JsonElement i in list)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadIListOfArray()
        {
            IList result = JsonSerializer.Parse<IList>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (JsonElement arr in result)
            {
                foreach (JsonElement i in arr.EnumerateArray())
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadArrayOfIList()
        {
            IList[] result = JsonSerializer.Parse<IList[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IList arr in result)
            {
                foreach (JsonElement i in arr)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadPrimitiveIList()
        {
            IList result = JsonSerializer.Parse<IList>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = JsonSerializer.Parse<IList>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        public static void ReadGenericICollectionOfICollection()
        {
            ICollection<ICollection> result = JsonSerializer.Parse<ICollection<ICollection>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (ICollection ie in result)
            {
                foreach (JsonElement i in ie)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadICollectionOfArray()
        {
            ICollection result = JsonSerializer.Parse<ICollection>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (JsonElement arr in result)
            {
                foreach (JsonElement i in arr.EnumerateArray())
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadArrayOfICollection()
        {
            ICollection[] result = JsonSerializer.Parse<ICollection[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (ICollection arr in result)
            {
                foreach (JsonElement i in arr)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadPrimitiveICollection()
        {
            ICollection result = JsonSerializer.Parse<ICollection>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = JsonSerializer.Parse<ICollection>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        public static void ReadGenericStackOfStack()
        {
            Stack<Stack> result = JsonSerializer.Parse<Stack<Stack>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 4;

            foreach (Stack stack in result)
            {
                foreach (JsonElement i in stack)
                {
                    Assert.Equal(expected--, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadStackOfArray()
        {
            Stack result = JsonSerializer.Parse<Stack>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 3;

            foreach (JsonElement arr in result)
            {
                foreach (JsonElement i in arr.EnumerateArray())
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
                expected = 1;
            }
        }

        [Fact]
        public static void ReadArrayOfStack()
        {
            Stack[] result = JsonSerializer.Parse<Stack[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 2;

            foreach (Stack arr in result)
            {
                foreach (JsonElement i in arr)
                {
                    Assert.Equal(expected--, i.GetInt32());
                }
                expected = 4;
            }
        }

        [Fact]
        public static void ReadPrimitiveStack()
        {
            Stack result = JsonSerializer.Parse<Stack>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 2;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected--, i.GetInt32());
            }

            result = JsonSerializer.Parse<Stack>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        public static void ReadGenericQueueOfQueue()
        {
            Queue<Queue> result = JsonSerializer.Parse<Queue<Queue>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (Queue ie in result)
            {
                foreach (JsonElement i in ie)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadQueueOfArray()
        {
            Queue result = JsonSerializer.Parse<Queue>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (JsonElement arr in result)
            {
                foreach (JsonElement i in arr.EnumerateArray())
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadArrayOfQueue()
        {
            Queue[] result = JsonSerializer.Parse<Queue[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (Queue arr in result)
            {
                foreach (JsonElement i in arr)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadPrimitiveQueue()
        {
            Queue result = JsonSerializer.Parse<Queue>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = JsonSerializer.Parse<Queue>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        [Fact]
        public static void ReadArrayListOfArray()
        {
            ArrayList result = JsonSerializer.Parse<ArrayList>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (JsonElement arr in result)
            {
                foreach (JsonElement i in arr.EnumerateArray())
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadArrayOfArrayList()
        {
            ArrayList[] result = JsonSerializer.Parse<ArrayList[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (ArrayList arr in result)
            {
                foreach (JsonElement i in arr)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }
        }

        [Fact]
        public static void ReadPrimitiveArrayList()
        {
            ArrayList result = JsonSerializer.Parse<ArrayList>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = JsonSerializer.Parse<ArrayList>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }
    }
}
