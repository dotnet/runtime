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
        public static void ReadGenericIEnumerableOfIEnumerable()
        {
            IEnumerable<IEnumerable> result = JsonSerializer.Deserialize<IEnumerable<IEnumerable>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IEnumerable ie in result)
            {
                foreach (JsonElement i in ie)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }

            // No way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<GenericIEnumerableWrapper<WrapperForIEnumerable>>(@"[[1,2],[3,4]]"));
        }

        [Fact]
        public static void ReadIEnumerableOfArray()
        {
            IEnumerable result = JsonSerializer.Deserialize<IEnumerable>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            IEnumerable[] result = JsonSerializer.Deserialize<IEnumerable[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            IEnumerable result = JsonSerializer.Deserialize<IEnumerable>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = JsonSerializer.Deserialize<IEnumerable>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        [Fact]
        public static void ReadGenericIListOfIList()
        {
            IList<IList> result = JsonSerializer.Deserialize<IList<IList>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (IList list in result)
            {
                foreach (JsonElement i in list)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }

            GenericIListWrapper<WrapperForIList> result2 = JsonSerializer.Deserialize<GenericIListWrapper<WrapperForIList>>(@"[[1,2],[3,4]]");
            expected = 1;

            foreach (WrapperForIList list in result2)
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
            IList result = JsonSerializer.Deserialize<IList>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            IList[] result = JsonSerializer.Deserialize<IList[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
        public static void ReadStructIList()
        {
            string json = @"[""a"",20]";
            var wrapper = JsonSerializer.Deserialize<StructWrapperForIList>(json);
            Assert.Equal(2, wrapper.Count);
            Assert.Equal("a", ((JsonElement)wrapper[0]).GetString());
            Assert.Equal(20, ((JsonElement)wrapper[1]).GetInt32());
        }

        [Fact]
        public static void ReadNullableStructIList()
        {
            string json = @"[""a"",20]";
            var wrapper = JsonSerializer.Deserialize<StructWrapperForIList?>(json);
            Assert.True(wrapper.HasValue);
            Assert.Equal(2, wrapper.Value.Count);
            Assert.Equal("a", ((JsonElement)wrapper.Value[0]).GetString());
            Assert.Equal(20, ((JsonElement)wrapper.Value[1]).GetInt32());
        }

        [Fact]
        public static void ReadNullableStructIListWithNullJson()
        {
            var wrapper = JsonSerializer.Deserialize<StructWrapperForIList?>("null");
            Assert.False(wrapper.HasValue);
        }

        [Fact]
        public static void ReadClassWithStructIListWrapper_NullJson_Throws()
        {
            string json = @"{ ""List"" : null }";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithStructIListWrapper>(json));
        }

        [Fact]
        public static void ReadStructIDictionary()
        {
            string json = @"{""Key"":""Value""}";
            var wrapper = JsonSerializer.Deserialize<StructWrapperForIDictionary>(json);
            Assert.Equal("Value", wrapper["Key"].ToString());
        }

        [Fact]
        public static void ReadNullableStructIDictionary()
        {
            string json = @"{""Key"":""Value""}";
            var wrapper = JsonSerializer.Deserialize<StructWrapperForIDictionary?>(json);
            Assert.True(wrapper.HasValue);
            Assert.Equal("Value", wrapper.Value["Key"].ToString());
        }

        [Fact]
        public static void ReadNullableStructIDictionaryWithNullJson()
        {
            var wrapper = JsonSerializer.Deserialize<StructWrapperForIDictionary?>("null");
            Assert.False(wrapper.HasValue);
        }

        [Fact]
        public static void ReadClassWithStructIDictionaryWrapper_NullJson_Throws()
        {
            string json = @"{ ""Dictionary"" : null }";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithStructIDictionaryWrapper>(json));
        }

        [Fact]
        public static void ReadPrimitiveIList()
        {
            IList result = JsonSerializer.Deserialize<IList>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = JsonSerializer.Deserialize<IList>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);

            WrapperForIList result2 = JsonSerializer.Deserialize<WrapperForIList>(@"[1,2]");
            expected = 1;

            foreach (JsonElement i in result2)
            {
                Assert.Equal(expected++, i.GetInt32());
            }
        }

        [Fact]
        public static void ReadGenericICollectionOfICollection()
        {
            ICollection<ICollection> result = JsonSerializer.Deserialize<ICollection<ICollection>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (ICollection ie in result)
            {
                foreach (JsonElement i in ie)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }

            // No way to populate this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<GenericICollectionWrapper<WrapperForICollection>>(@"[[1,2],[3,4]]"));
        }

        [Fact]
        public static void ReadICollectionOfArray()
        {
            ICollection result = JsonSerializer.Deserialize<ICollection>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            ICollection[] result = JsonSerializer.Deserialize<ICollection[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            ICollection result = JsonSerializer.Deserialize<ICollection>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = JsonSerializer.Deserialize<ICollection>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        [Fact]
        public static void ReadGenericStackOfStack()
        {
            Stack<Stack> result = JsonSerializer.Deserialize<Stack<Stack>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            Stack result = JsonSerializer.Deserialize<Stack>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            Stack[] result = JsonSerializer.Deserialize<Stack[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            Stack result = JsonSerializer.Deserialize<Stack>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 2;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected--, i.GetInt32());
            }

            result = JsonSerializer.Deserialize<Stack>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);

            StackWrapper wrapper =  JsonSerializer.Deserialize<StackWrapper>(@"[1,2]");
            expected = 2;

            foreach (JsonElement i in wrapper)
            {
                Assert.Equal(expected--, i.GetInt32());
            }
        }

        [Fact]
        public static void ReadGenericQueueOfQueue()
        {
            Queue<Queue> result = JsonSerializer.Deserialize<Queue<Queue>>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            Queue result = JsonSerializer.Deserialize<Queue>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            Queue[] result = JsonSerializer.Deserialize<Queue[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            Queue result = JsonSerializer.Deserialize<Queue>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = JsonSerializer.Deserialize<Queue>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
            
            QueueWrapper wrapper = JsonSerializer.Deserialize<QueueWrapper>(@"[1,2]");
            expected = 1;

            foreach (JsonElement i in wrapper)
            {
                Assert.Equal(expected++, i.GetInt32());
            }
        }

        [Fact]
        public static void ReadArrayListOfArray()
        {
            ArrayList result = JsonSerializer.Deserialize<ArrayList>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            int expected = 1;

            foreach (JsonElement arr in result)
            {
                foreach (JsonElement i in arr.EnumerateArray())
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }

            ArrayListWrapper result2 = JsonSerializer.Deserialize<ArrayListWrapper>(@"[[1,2],[3,4]]");
            expected = 1;

            foreach (JsonElement arr in result2)
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
            ArrayList[] result = JsonSerializer.Deserialize<ArrayList[]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
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
            ArrayList result = JsonSerializer.Deserialize<ArrayList>(Encoding.UTF8.GetBytes(@"[1,2]"));
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = JsonSerializer.Deserialize<ArrayList>(Encoding.UTF8.GetBytes(@"[]"));

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        [Fact]
        public static void ReadSimpleTestClass_NonGenericCollectionWrappers()
        {
            SimpleTestClassWithNonGenericCollectionWrappers obj = JsonSerializer.Deserialize<SimpleTestClassWithNonGenericCollectionWrappers>(SimpleTestClassWithNonGenericCollectionWrappers.s_json);
            obj.Verify();
        }

        [Fact]
        public static void ReadSimpleTestClass_StructCollectionWrappers()
        {
            SimpleTestClassWithStructCollectionWrappers obj = JsonSerializer.Deserialize<SimpleTestClassWithStructCollectionWrappers>(SimpleTestClassWithStructCollectionWrappers.s_json);
            obj.Verify();
        }

        [Fact]
        public static void ReadSimpleTestStruct_NullableStructCollectionWrappers()
        {
            {
                SimpleTestStructWithNullableStructCollectionWrappers obj = JsonSerializer.Deserialize<SimpleTestStructWithNullableStructCollectionWrappers>(SimpleTestStructWithNullableStructCollectionWrappers.s_json);
                obj.Verify();
            }

            {
                string json =
                        @"{" +
                        @"""List"" : null," +
                        @"""Dictionary"" : null" +
                        @"}";

                SimpleTestStructWithNullableStructCollectionWrappers obj = JsonSerializer.Deserialize<SimpleTestStructWithNullableStructCollectionWrappers>(json);
                Assert.False(obj.List.HasValue);
                Assert.False(obj.Dictionary.HasValue);
            }
        }

        [Theory]
        [MemberData(nameof(ReadSimpleTestClass_NonGenericWrappers_NoAddMethod))]
        public static void ReadSimpleTestClass_NonGenericWrappers_NoAddMethod_Throws(Type type, string json, Type exceptionMessageType)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(json, type));
            Assert.Contains(exceptionMessageType.ToString(), ex.Message);
        }

        public static IEnumerable<object[]> ReadSimpleTestClass_NonGenericWrappers_NoAddMethod
        {
            get
            {
                yield return new object[]
                {
                    typeof(SimpleTestClassWithIEnumerableWrapper),
                    SimpleTestClassWithIEnumerableWrapper.s_json,
                    typeof(WrapperForIEnumerable)
                };
                yield return new object[]
                {
                    typeof(SimpleTestClassWithICollectionWrapper),
                    SimpleTestClassWithICollectionWrapper.s_json,
                    typeof(WrapperForICollection)
                };
            }
        }

        [Theory]
        [InlineData(typeof(WrapperForIEnumerablePrivateConstructor), @"[""1""]")]
        [InlineData(typeof(WrapperForIEnumerableInternalConstructor), @"[""1""]")]
        [InlineData(typeof(WrapperForICollectionPrivateConstructor), @"[""1""]")]
        [InlineData(typeof(WrapperForICollectionInternalConstructor), @"[""1""]")]
        [InlineData(typeof(WrapperForIListPrivateConstructor), @"[""1""]")]
        [InlineData(typeof(WrapperForIListInternalConstructor), @"[""1""]")]
        [InlineData(typeof(WrapperForIDictionaryPrivateConstructor), @"{""Key"":""Value""}")]
        [InlineData(typeof(WrapperForIDictionaryInternalConstructor), @"{""Key"":""Value""}")]
        public static void Read_NonGeneric_NoPublicConstructor_Throws(Type type, string json)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(json, type));
            Assert.Contains(type.ToString(), ex.Message);
        }
    }
}
