// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Xunit;
using System.Collections.Generic;
using System.Collections;

namespace System.Json.Tests
{
    public class JsonArrayTests
    {
        public static IEnumerable<object[]> JsonValues_TestData()
        {
            yield return new object[] { new JsonValue[0] };
            yield return new object[] { new JsonValue[] { null } };
        }

        [Theory]
        [MemberData(nameof(JsonValues_TestData))]
        public void Ctor(JsonValue[] items)
        {
            VerifyJsonArray(new JsonArray(items), items);
            VerifyJsonArray(new JsonArray((IEnumerable<JsonValue>)items), items);
        }

        private static void VerifyJsonArray(JsonArray array, JsonValue[] values)
        {
            Assert.Equal(values.Length, array.Count);
            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(values[i], array[i]);
            }
        }
        
        [Fact]
        public void Ctor_Array_Works()
        {
        	// Workaround xunit/xunit#987: InvalidOperationException thrown if this is in MemberData
            JsonValue[] items = new JsonValue[] { new JsonPrimitive(true) };
            JsonArray array = new JsonArray(items);
            Assert.Equal(1, array.Count);
            Assert.Same(items[0], array[0]);
        }

        [Fact]
        public void Ctor_IEnumerable_Works()
        {
        	// Workaround xunit/xunit#987: InvalidOperationException thrown if this is in MemberData
            JsonValue[] items = new JsonValue[] { new JsonPrimitive(true) };
            JsonArray array = new JsonArray((IEnumerable<JsonValue>)items);
            Assert.Equal(1, array.Count);
            Assert.Same(items[0], array[0]);
        }

        [Fact]
        public void Ctor_NullArray_Works()
        {
            JsonArray array = new JsonArray(null);
            Assert.Equal(0, array.Count);
        }

        [Fact]
        public void Ctor_NullIEnumerable_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("items", () => new JsonArray((IEnumerable<JsonValue>)null));
        }

        [Fact]
        public void Item_Get_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array[1]);
        }

        [Fact]
        public void Item_Set_Get()
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true));
            JsonPrimitive value = new JsonPrimitive(false);
            array[0] = value;
            Assert.Same(value, array[0]);
        }

        [Fact]
        public void Item_Set_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array[-1] = false);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array[1] = false);
        }

        [Fact]
        public void IsReadOnly_Get_ReturnsFalse()
        {
            Assert.False(new JsonArray().IsReadOnly);
        }

        [Fact]
        public void JsonType_ReturnsArray()
        {
            Assert.Equal(JsonType.Array, new JsonArray().JsonType);
        }

        [Fact]
        public void Add()
        {
            JsonArray array = new JsonArray();
            JsonValue value = new JsonPrimitive(true);
            array.Add(value);
            Assert.Equal(1, array.Count);
            Assert.Same(value, array[0]);
        }

        [Fact]
        public void Add_NullItem()
        {
            JsonArray array = new JsonArray();
            array.Add(null);
            Assert.Equal(1, array.Count);
        }

        [Fact]
        public void AddRange_Array()
        {
            JsonArray array = new JsonArray();
            JsonValue[] values = new JsonValue[] { null, new JsonPrimitive(true) };

            array.AddRange(values);
            Assert.Equal(2, array.Count);
            Assert.Same(values[0], array[0]);
            Assert.Same(values[1], array[1]);
        }

        [Fact]
        public void AddRange_IEnumerable()
        {
            JsonArray array = new JsonArray();
            JsonValue[] values = new JsonValue[] { null, new JsonPrimitive(true) };

            array.AddRange((IEnumerable<JsonValue>)values);
            Assert.Equal(2, array.Count);
            Assert.Same(values[0], array[0]);
            Assert.Same(values[1], array[1]);
        }

        [Fact]
        public void AddRange_NullArray_Works()
        {
            JsonArray array = new JsonArray();
            array.AddRange(null);
            Assert.Equal(0, array.Count);
        }

        [Fact]
        public void AddRange_NullIEnumerable_ThrowsArgumentNullException()
        {
            JsonArray array = new JsonArray();
            AssertExtensions.Throws<ArgumentNullException>("items", () => array.AddRange((IEnumerable<JsonValue>)null));
        }

        [Fact]
        public void Insert()
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true));
            JsonPrimitive value = new JsonPrimitive(false);
            array.Insert(1, value);

            Assert.Equal(2, array.Count);
            Assert.Same(value, array[1]);
        }

        [Fact]
        public void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.Insert(-1, new JsonPrimitive(false)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.Insert(2, new JsonPrimitive(false)));
        }

        [Fact]
        public void IndexOf()
        {
            JsonValue[] items = new JsonValue[] { new JsonPrimitive(true) };
            JsonArray array = new JsonArray((IEnumerable<JsonValue>)items);

            Assert.Equal(0, array.IndexOf(items[0]));
            Assert.Equal(-1, array.IndexOf(new JsonPrimitive(false)));
        }

        [Fact]
        public void Contains()
        {
            JsonValue[] items = new JsonValue[] { new JsonPrimitive(true) };
            JsonArray array = new JsonArray((IEnumerable<JsonValue>)items);

            Assert.True(array.Contains(items[0]));
            Assert.False(array.Contains(new JsonPrimitive(false)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void CopyTo(int arrayIndex)
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true));
            JsonValue[] copy = new JsonValue[array.Count + arrayIndex];
            array.CopyTo(copy, arrayIndex);

            for (int i = 0; i < arrayIndex; i++)
            {
                Assert.Null(copy[i]);
            }
            for (int i = arrayIndex; i < copy.Length; i++)
            {
                Assert.Same(array[i - arrayIndex], copy[i]);
            }
        }

        [Fact]
        public void Remove()
        {
            JsonValue[] items = new JsonValue[] { new JsonPrimitive(true) };
            JsonArray array = new JsonArray((IEnumerable<JsonValue>)items);

            array.Remove(items[0]);
            Assert.Equal(0, array.Count);

            array.Remove(items[0]);
            Assert.Equal(0, array.Count);
        }

        [Fact]
        public void RemoveAt()
        {
            JsonValue[] items = new JsonValue[] { new JsonPrimitive(true) };
            JsonArray array = new JsonArray((IEnumerable<JsonValue>)items);

            array.RemoveAt(0);
            Assert.Equal(0, array.Count);
        }

        [Fact]
        public void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.RemoveAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.RemoveAt(1));
        }

        [Fact]
        public void Clear()
        {
            JsonArray array = new JsonArray(new JsonValue[3]);
            array.Clear();
            Assert.Equal(0, array.Count);

            array.Clear();
            Assert.Equal(0, array.Count);
        }

        [Fact]
        public void Save_Stream()
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true), null);
            using (MemoryStream stream = new MemoryStream())
            {
                array.Save(stream);
                string result = Encoding.UTF8.GetString(stream.ToArray());
                Assert.Equal("[true, null]", result);
            }
        }

        [Fact]
        public void Save_TextWriter()
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true), null);
            using (StringWriter writer = new StringWriter())
            {
                array.Save(writer);
                Assert.Equal("[true, null]", writer.ToString());
            }
        }

        [Fact]
        public void Save_NullStream_ThrowsArgumentNullException()
        {
            JsonArray array = new JsonArray();
            AssertExtensions.Throws<ArgumentNullException>("stream", () => array.Save((Stream)null));
            AssertExtensions.Throws<ArgumentNullException>("textWriter", () => array.Save((TextWriter)null));
        }

        [Fact]
        public void GetEnumerator_GenericIEnumerable()
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true));

            IEnumerator<JsonValue> enumerator = ((IEnumerable<JsonValue>)array).GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Same(array[counter], enumerator.Current);
                    counter++;
                }
                Assert.Equal(array.Count, counter);

                enumerator.Reset();
            }
        }

        [Fact]
        public void GetEnumerator_NonGenericIEnumerable()
        {
            JsonArray array = new JsonArray(new JsonPrimitive(true));

            IEnumerator enumerator = ((IEnumerable)array).GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Same(array[counter], enumerator.Current);
                    counter++;
                }
                Assert.Equal(array.Count, counter);

                enumerator.Reset();
            }
        }
    }
}
