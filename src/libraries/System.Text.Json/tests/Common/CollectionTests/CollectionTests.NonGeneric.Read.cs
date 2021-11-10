// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public async Task ReadGenericIEnumerableOfIEnumerable()
        {
            IEnumerable<IEnumerable> result = await JsonSerializerWrapperForString.DeserializeWrapper<IEnumerable<IEnumerable>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IEnumerable ie in result)
            {
                foreach (JsonElement i in ie)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }

            // No way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<GenericIEnumerableWrapper<WrapperForIEnumerable>>(@"[[1,2],[3,4]]"));
        }

        [Fact]
        public async Task ReadIEnumerableOfArray()
        {
            IEnumerable result = await JsonSerializerWrapperForString.DeserializeWrapper<IEnumerable>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfIEnumerable()
        {
            IEnumerable[] result = await JsonSerializerWrapperForString.DeserializeWrapper<IEnumerable[]>(@"[[1,2],[3,4]]");
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
        public async Task ReadPrimitiveIEnumerable()
        {
            IEnumerable result = await JsonSerializerWrapperForString.DeserializeWrapper<IEnumerable>(@"[1,2]");
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<IEnumerable>(@"[]");

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task ReadGenericIListOfIList()
        {
            IList<IList> result = await JsonSerializerWrapperForString.DeserializeWrapper<IList<IList>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (IList list in result)
            {
                foreach (JsonElement i in list)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }

            GenericIListWrapper<WrapperForIList> result2 = await JsonSerializerWrapperForString.DeserializeWrapper<GenericIListWrapper<WrapperForIList>>(@"[[1,2],[3,4]]");
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
        public async Task ReadIListOfArray()
        {
            IList result = await JsonSerializerWrapperForString.DeserializeWrapper<IList>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfIList()
        {
            IList[] result = await JsonSerializerWrapperForString.DeserializeWrapper<IList[]>(@"[[1,2],[3,4]]");
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
        public async Task ReadStructIList()
        {
            string json = @"[""a"",20]";
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<StructWrapperForIList>(json);
            Assert.Equal(2, wrapper.Count);
            Assert.Equal("a", ((JsonElement)wrapper[0]).GetString());
            Assert.Equal(20, ((JsonElement)wrapper[1]).GetInt32());
        }

        [Fact]
        public async Task ReadNullableStructIList()
        {
            string json = @"[""a"",20]";
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<StructWrapperForIList?>(json);
            Assert.True(wrapper.HasValue);
            Assert.Equal(2, wrapper.Value.Count);
            Assert.Equal("a", ((JsonElement)wrapper.Value[0]).GetString());
            Assert.Equal(20, ((JsonElement)wrapper.Value[1]).GetInt32());
        }

        [Fact]
        public async Task ReadNullableStructIListWithNullJson()
        {
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<StructWrapperForIList?>("null");
            Assert.False(wrapper.HasValue);
        }

        [Fact]
        public async Task ReadClassWithStructIListWrapper_NullJson_Throws()
        {
            string json = @"{ ""List"" : null }";
            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithStructIListWrapper>(json));
        }

        [Fact]
        public async Task ReadStructIDictionary()
        {
            string json = @"{""Key"":""Value""}";
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<StructWrapperForIDictionary>(json);
            Assert.Equal("Value", wrapper["Key"].ToString());
        }

        [Fact]
        public async Task ReadNullableStructIDictionary()
        {
            string json = @"{""Key"":""Value""}";
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<StructWrapperForIDictionary?>(json);
            Assert.True(wrapper.HasValue);
            Assert.Equal("Value", wrapper.Value["Key"].ToString());
        }

        [Fact]
        public async Task ReadNullableStructIDictionaryWithNullJson()
        {
            var wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<StructWrapperForIDictionary?>("null");
            Assert.False(wrapper.HasValue);
        }

        [Fact]
        public async Task ReadClassWithStructIDictionaryWrapper_NullJson_Throws()
        {
            string json = @"{ ""Dictionary"" : null }";
            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithStructIDictionaryWrapper>(json));
        }

        [Fact]
        public async Task ReadPrimitiveIList()
        {
            IList result = await JsonSerializerWrapperForString.DeserializeWrapper<IList>(@"[1,2]");
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<IList>(@"[]");

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);

            WrapperForIList result2 = await JsonSerializerWrapperForString.DeserializeWrapper<WrapperForIList>(@"[1,2]");
            expected = 1;

            foreach (JsonElement i in result2)
            {
                Assert.Equal(expected++, i.GetInt32());
            }
        }

        [Fact]
        public async Task ReadGenericICollectionOfICollection()
        {
            ICollection<ICollection> result = await JsonSerializerWrapperForString.DeserializeWrapper<ICollection<ICollection>>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (ICollection ie in result)
            {
                foreach (JsonElement i in ie)
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }

            // No way to populate this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<GenericICollectionWrapper<WrapperForICollection>>(@"[[1,2],[3,4]]"));
        }

        [Fact]
        public async Task ReadICollectionOfArray()
        {
            ICollection result = await JsonSerializerWrapperForString.DeserializeWrapper<ICollection>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfICollection()
        {
            ICollection[] result = await JsonSerializerWrapperForString.DeserializeWrapper<ICollection[]>(@"[[1,2],[3,4]]");
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
        public async Task ReadPrimitiveICollection()
        {
            ICollection result = await JsonSerializerWrapperForString.DeserializeWrapper<ICollection>(@"[1,2]");
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<ICollection>(@"[]");

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task ReadGenericStackOfStack()
        {
            Stack<Stack> result = await JsonSerializerWrapperForString.DeserializeWrapper<Stack<Stack>>(@"[[1,2],[3,4]]");
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
        public async Task ReadStackOfArray()
        {
            Stack result = await JsonSerializerWrapperForString.DeserializeWrapper<Stack>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfStack()
        {
            Stack[] result = await JsonSerializerWrapperForString.DeserializeWrapper<Stack[]>(@"[[1,2],[3,4]]");
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
        public async Task ReadPrimitiveStack()
        {
            Stack result = await JsonSerializerWrapperForString.DeserializeWrapper<Stack>(@"[1,2]");
            int expected = 2;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected--, i.GetInt32());
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<Stack>(@"[]");

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);

            StackWrapper wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<StackWrapper>(@"[1,2]");
            expected = 2;

            foreach (JsonElement i in wrapper)
            {
                Assert.Equal(expected--, i.GetInt32());
            }
        }

        [Fact]
        public async Task ReadGenericQueueOfQueue()
        {
            Queue<Queue> result = await JsonSerializerWrapperForString.DeserializeWrapper<Queue<Queue>>(@"[[1,2],[3,4]]");
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
        public async Task ReadQueueOfArray()
        {
            Queue result = await JsonSerializerWrapperForString.DeserializeWrapper<Queue>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfQueue()
        {
            Queue[] result = await JsonSerializerWrapperForString.DeserializeWrapper<Queue[]>(@"[[1,2],[3,4]]");
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
        public async Task ReadPrimitiveQueue()
        {
            Queue result = await JsonSerializerWrapperForString.DeserializeWrapper<Queue>(@"[1,2]");
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<Queue>(@"[]");

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
            
            QueueWrapper wrapper = await JsonSerializerWrapperForString.DeserializeWrapper<QueueWrapper>(@"[1,2]");
            expected = 1;

            foreach (JsonElement i in wrapper)
            {
                Assert.Equal(expected++, i.GetInt32());
            }
        }

        [Fact]
        public async Task ReadArrayListOfArray()
        {
            ArrayList result = await JsonSerializerWrapperForString.DeserializeWrapper<ArrayList>(@"[[1,2],[3,4]]");
            int expected = 1;

            foreach (JsonElement arr in result)
            {
                foreach (JsonElement i in arr.EnumerateArray())
                {
                    Assert.Equal(expected++, i.GetInt32());
                }
            }

            ArrayListWrapper result2 = await JsonSerializerWrapperForString.DeserializeWrapper<ArrayListWrapper>(@"[[1,2],[3,4]]");
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
        public async Task ReadArrayOfArrayList()
        {
            ArrayList[] result = await JsonSerializerWrapperForString.DeserializeWrapper<ArrayList[]>(@"[[1,2],[3,4]]");
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
        public async Task ReadPrimitiveArrayList()
        {
            ArrayList result = await JsonSerializerWrapperForString.DeserializeWrapper<ArrayList>(@"[1,2]");
            int expected = 1;

            foreach (JsonElement i in result)
            {
                Assert.Equal(expected++, i.GetInt32());
            }

            result = await JsonSerializerWrapperForString.DeserializeWrapper<ArrayList>(@"[]");

            int count = 0;
            IEnumerator e = result.GetEnumerator();
            while (e.MoveNext())
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task ReadSimpleTestClass_NonGenericCollectionWrappers()
        {
            SimpleTestClassWithNonGenericCollectionWrappers obj = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClassWithNonGenericCollectionWrappers>(SimpleTestClassWithNonGenericCollectionWrappers.s_json);
            obj.Verify();
        }

        [Fact]
        public async Task ReadSimpleTestClass_StructCollectionWrappers()
        {
            SimpleTestClassWithStructCollectionWrappers obj = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClassWithStructCollectionWrappers>(SimpleTestClassWithStructCollectionWrappers.s_json);
            obj.Verify();
        }

        [Fact]
        public async Task ReadSimpleTestStruct_NullableStructCollectionWrappers()
        {
            {
                SimpleTestStructWithNullableStructCollectionWrappers obj = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestStructWithNullableStructCollectionWrappers>(SimpleTestStructWithNullableStructCollectionWrappers.s_json);
                obj.Verify();
            }

            {
                string json =
                        @"{" +
                        @"""List"" : null," +
                        @"""Dictionary"" : null" +
                        @"}";

                SimpleTestStructWithNullableStructCollectionWrappers obj = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestStructWithNullableStructCollectionWrappers>(json);
                Assert.False(obj.List.HasValue);
                Assert.False(obj.Dictionary.HasValue);
            }
        }

        [Theory]
        [MemberData(nameof(ReadSimpleTestClass_NonGenericWrappers_NoAddMethod))]
        public async Task ReadSimpleTestClass_NonGenericWrappers_NoAddMethod_Throws(Type type, string json, Type exceptionMessageType)
        {
            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper(json, type));
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
        public async Task Read_NonGeneric_NoPublicConstructor_Throws(Type type, string json)
        {
            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper(json, type));
            Assert.Contains(type.ToString(), ex.Message);
        }
    }
}
