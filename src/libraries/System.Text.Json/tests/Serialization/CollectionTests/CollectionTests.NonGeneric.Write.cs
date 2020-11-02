// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
    {
        [Fact]
        public static void WriteIEnumerableOfIEnumerable()
        {
            IEnumerable input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            WrapperForIEnumerable input2 = new WrapperForIEnumerable(new List<object>
            {
                new List<object>() { 1, 2 },
                new List<object>() { 3, 4 },
            });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteGenericIEnumerableOfIEnumerable()
        {
            IEnumerable<IEnumerable> input = new List<IEnumerable>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIEnumerable()
        {
            IEnumerable[] input = new IEnumerable[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIEnumerable()
        {
            IEnumerable input = new List<int> { 1, 2 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteStructWrapperForIList()
        {
            {
                StructWrapperForIList obj = new StructWrapperForIList() { 1, "Hello" };
                Assert.Equal(@"[1,""Hello""]", JsonSerializer.Serialize(obj));
            }

            {
                StructWrapperForIList obj = default;
                Assert.Equal("[]", JsonSerializer.Serialize(obj));
            }
        }

        [Fact]
        public static void WriteIListOfIList()
        {
            IList input = new List<IList>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            WrapperForIList input2 = new WrapperForIList
            {
                new List<object>() { 1, 2 },
                new List<object>() { 3, 4 },
            };

            json = JsonSerializer.Serialize(input2);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteIListGenericOfIList()
        {
            IList<IList> input = new List<IList>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIList()
        {
            IList[] input = new IList[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIList()
        {
            IList input = new List<int> { 1, 2 };

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteGenericICollectionOfICollection()
        {
            ICollection<ICollection> input = new List<ICollection>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericICollectionWrapper<WrapperForICollection> input2 = new GenericICollectionWrapper<WrapperForICollection>
            {
                new WrapperForICollection(new List<object> { 1, 2 }),
                new WrapperForICollection(new List<object> { 3, 4 }),
            };

            json = JsonSerializer.Serialize(input2);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfICollection()
        {
            ICollection[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveICollection()
        {
            ICollection input = new List<int> { 1, 2 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteStackOfStack()
        {
            Stack input = new Stack();
            input.Push(new Stack(new List<int>() { 1, 2 }));
            input.Push(new Stack(new List<int>() { 3, 4 }));

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[4,3],[2,1]]", json);
        }

        [Fact]
        public static void WriteGenericStackOfStack()
        {
            Stack<Stack> input = new Stack<Stack>();
            input.Push(new Stack(new List<int>() { 1, 2 }));
            input.Push(new Stack(new List<int>() { 3, 4 }));

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[4,3],[2,1]]", json);

            GenericStackWrapper<StackWrapper> input2 = new GenericStackWrapper<StackWrapper>();
            input2.Push(new StackWrapper(new List<object> { 1, 2 }));
            input2.Push(new StackWrapper(new List<object> { 3, 4 }));

            json = JsonSerializer.Serialize(input2);
            Assert.Equal("[[4,3],[2,1]]", json);
        }

        [Fact]
        public static void WriteArrayOfStack()
        {
            Stack[] input = new Stack[2];
            input[0] = new Stack(new List<int>() { 1, 2 });
            input[1] = new Stack(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[2,1],[4,3]]", json);
        }

        [Fact]
        public static void WritePrimitiveStack()
        {
            Stack input = new Stack( new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[2,1]", json);
        }

        [Fact]
        public static void WriteQueueOfQueue()
        {
            Queue input = new Queue();
            input.Enqueue(new Queue(new List<int>() { 1, 2 }));
            input.Enqueue(new Queue(new List<int>() { 3, 4 }));

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteGenericQueueOfQueue()
        {
            Queue<Queue> input = new Queue<Queue>();
            input.Enqueue(new Queue(new List<int>() { 1, 2 }));
            input.Enqueue(new Queue(new List<int>() { 3, 4 }));

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericQueueWrapper<QueueWrapper> input2 = new GenericQueueWrapper<QueueWrapper>();
            input2.Enqueue(new QueueWrapper(new List<object>() { 1, 2 }));
            input2.Enqueue(new QueueWrapper(new List<object>() { 3, 4 }));

            json = JsonSerializer.Serialize(input2);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfQueue()
        {
            Queue[] input = new Queue[2];
            input[0] = new Queue(new List<int>() { 1, 2 });
            input[1] = new Queue(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveQueue()
        {
            Queue input = new Queue(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            ArrayListWrapper input2 = new ArrayListWrapper(new List<object>
            {
                new ArrayListWrapper(new List<object>() { 1, 2 }),
                new ArrayListWrapper(new List<object>() { 3, 4 })
            });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfArrayList()
        {
            ArrayList[] input = new ArrayList[2];
            input[0] = new ArrayList(new List<int>() { 1, 2 });
            input[1] = new ArrayList(new List<int>() { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveArrayList()
        {
            ArrayList input = new ArrayList(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteSimpleTestStructWithNullableStructCollectionWrappers()
        {
            {
                SimpleTestStructWithNullableStructCollectionWrappers obj = new SimpleTestStructWithNullableStructCollectionWrappers();
                obj.Initialize();
                Assert.Equal(SimpleTestStructWithNullableStructCollectionWrappers.s_json.StripWhitespace(), JsonSerializer.Serialize(obj));
            }

            {
                SimpleTestStructWithNullableStructCollectionWrappers obj = new SimpleTestStructWithNullableStructCollectionWrappers();
                string json =
                    @"{" +
                    @"""List"" : null," +
                    @"""Dictionary"" : null" +
                    @"}";
                Assert.Equal(json.StripWhitespace(), JsonSerializer.Serialize(obj));
            }
        }

        [Fact]
        public static void WriteSimpleTestClassWithStructCollectionWrappers()
        {
            {
                SimpleTestClassWithStructCollectionWrappers obj = new SimpleTestClassWithStructCollectionWrappers();
                obj.Initialize();
                Assert.Equal(SimpleTestClassWithStructCollectionWrappers.s_json.StripWhitespace(), JsonSerializer.Serialize(obj));
            }

            {
                SimpleTestClassWithStructCollectionWrappers obj = new SimpleTestClassWithStructCollectionWrappers()
                {
                    List = default,
                    Dictionary = default
                };
                string json =
                    @"{" +
                    @"""List"" : []," +
                    @"""Dictionary"" : {}" +
                    @"}";
                Assert.Equal(json.StripWhitespace(), JsonSerializer.Serialize(obj));
            }
        }

        [Fact]
        public static void WriteNonGenericCollectionWrappers()
        {
            SimpleTestClassWithNonGenericCollectionWrappers obj1 = new SimpleTestClassWithNonGenericCollectionWrappers();
            SimpleTestClassWithIEnumerableWrapper obj2 = new SimpleTestClassWithIEnumerableWrapper();
            SimpleTestClassWithICollectionWrapper obj3 = new SimpleTestClassWithICollectionWrapper();
            SimpleTestClassWithStackWrapper obj4 = new SimpleTestClassWithStackWrapper();
            SimpleTestClassWithQueueWrapper obj5 = new SimpleTestClassWithQueueWrapper();

            obj1.Initialize();
            obj2.Initialize();
            obj3.Initialize();
            obj4.Initialize();
            obj5.Initialize();

            Assert.Equal(SimpleTestClassWithNonGenericCollectionWrappers.s_json.StripWhitespace(), JsonSerializer.Serialize(obj1));
            Assert.Equal(SimpleTestClassWithNonGenericCollectionWrappers.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj1));

            Assert.Equal(SimpleTestClassWithIEnumerableWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj2));
            Assert.Equal(SimpleTestClassWithIEnumerableWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj2));

            Assert.Equal(SimpleTestClassWithICollectionWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj3));
            Assert.Equal(SimpleTestClassWithICollectionWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj3));

            Assert.Equal(SimpleTestClassWithStackWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj4));
            Assert.Equal(SimpleTestClassWithStackWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj4));

            Assert.Equal(SimpleTestClassWithQueueWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj5));
            Assert.Equal(SimpleTestClassWithQueueWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj5));
        }
    }
}
