// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Json.Tests
{
    public class JsonObjectTests
    {
        public static IEnumerable<object[]> Ctor_Array_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new KeyValuePair<string, JsonValue>[0] };
            yield return new object[] { new KeyValuePair<string, JsonValue>[] { new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true)) } };
        }

        [Theory]
        [MemberData(nameof(Ctor_Array_TestData))]
        public void Ctor_Array(KeyValuePair<string, JsonValue>[] items)
        {
            var obj = new JsonObject(items);

            Assert.Equal(items?.Length ?? 0, obj.Count);
            for (int i = 0; i < (items?.Length ?? 0); i++)
            {
                Assert.Equal(items[i].Value.ToString(), obj[items[i].Key].ToString());

                JsonValue value;
                Assert.True(obj.TryGetValue(items[i].Key, out value));
                Assert.Equal(items[i].Value.ToString(), value.ToString());
            }
        }

        public static IEnumerable<object[]> Ctor_Enumerable_TestData()
        {
            yield return new object[] { Enumerable.Empty<KeyValuePair<string, JsonValue>>() };
            yield return new object[] { new KeyValuePair<string, JsonValue>[] { new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true)) } };
        }

        [Theory]
        [MemberData(nameof(Ctor_Enumerable_TestData))]
        public void Ctor_IEnumerable(IEnumerable<KeyValuePair<string, JsonValue>> items)
        {
            var obj = new JsonObject(items);

            KeyValuePair<string, JsonValue>[] expectedItems = items.ToArray();
            Assert.Equal(expectedItems.Length, obj.Count);
            for (int i = 0; i < expectedItems.Length; i++)
            {
                Assert.Equal(expectedItems[i].Value.ToString(), obj[expectedItems[i].Key].ToString());

                JsonValue value;
                Assert.True(obj.TryGetValue(expectedItems[i].Key, out value));
                Assert.Equal(expectedItems[i].Value.ToString(), value.ToString());
            }
        }

        [Fact]
        public void Ctor_NullIEnumerable_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("items", () => new JsonObject((IEnumerable<KeyValuePair<string, JsonValue>>)null));
        }
        
        [Fact]
        public void JsonType_Get_ReturnsObject()
        {
            Assert.Equal(JsonType.Object, new JsonObject().JsonType);
        }

        [Fact]
        public void IsReadOnly_Get_ReturnsFalse()
        {
            ICollection<KeyValuePair<string, JsonValue>> iCollection = new JsonObject();
            Assert.False(iCollection.IsReadOnly);
        }

        [Fact]
        public void Item_Set_GetReturnsExpected()
        {
            JsonObject obj = new JsonObject();

            string key = "key";
            JsonValue value = new JsonPrimitive(true);
            obj[key] = value;

            Assert.Equal(1, obj.Count);
            Assert.Same(value, obj[key]);
        }

        [Fact]
        public void Item_NoSuchKey_ThrowsKeyNotFoundException()
        {
            JsonObject obj = new JsonObject();
            Assert.Throws<KeyNotFoundException>(() => obj["no-such-key"]);
        }

        [Fact]
        public void TryGetValue_NoSuchKey_ReturnsNull()
        {
            JsonObject obj = new JsonObject();

            JsonValue value;
            Assert.False(obj.TryGetValue("no-such-key", out value));
            Assert.Null(value);
        }

        [Fact]
        public void Add()
        {
            JsonObject obj = new JsonObject();
            KeyValuePair<string, JsonValue> item = new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true));
            obj.Add(item);

            Assert.Equal(1, obj.Count);
            Assert.Equal(item.Key, obj.Keys.First());
            Assert.Equal(item.Value.ToString(), obj.Values.First().ToString());
        }

        [Fact]
        public void Add_NullKey_ThrowsArgumentNullException()
        {
            JsonObject obj = new JsonObject();
            AssertExtensions.Throws<ArgumentNullException>("key", () => obj.Add(null, new JsonPrimitive(true)));
        }

        [Fact]
        public void Add_NullKeyInKeyValuePair_ThrowsArgumentNullException()
        {
            JsonObject obj = new JsonObject();
            KeyValuePair<string, JsonValue> item = new KeyValuePair<string, JsonValue>(null, new JsonPrimitive(true));
            AssertExtensions.Throws<ArgumentNullException>("key", () => obj.Add(item));
        }

        [Fact]
        public void AddRange_Array()
        {
            KeyValuePair<string, JsonValue>[] items = new KeyValuePair<string, JsonValue>[] { new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true)) };
            JsonObject obj = new JsonObject();
            obj.AddRange(items);

            Assert.Equal(items.Length, obj.Count);
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(items[i].Value.ToString(), obj[items[i].Key].ToString());
            }
        }

        [Fact]
        public void AddRange_IEnumerable()
        {
            KeyValuePair<string, JsonValue>[] items = new KeyValuePair<string, JsonValue>[] { new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true)) };
            JsonObject obj = new JsonObject();
            obj.AddRange((IEnumerable<KeyValuePair<string, JsonValue>>)items);

            Assert.Equal(items.Length, obj.Count);
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(items[i].Value.ToString(), obj[items[i].Key].ToString());
            }
        }

        [Fact]
        public void AddRange_NullItems_ThrowsArgumentNullException()
        {
            JsonObject obj = new JsonObject();
            AssertExtensions.Throws<ArgumentNullException>("items", () => obj.AddRange(null));
            AssertExtensions.Throws<ArgumentNullException>("items", () => obj.AddRange((IEnumerable<KeyValuePair<string, JsonValue>>)null));
        }

        [Fact]
        public void Clear()
        {
            JsonObject obj = new JsonObject(new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true)));
            obj.Clear();
            Assert.Equal(0, obj.Count);

            obj.Clear();
            Assert.Equal(0, obj.Count);
        }

        [Fact]
        public void ContainsKey()
        {
            KeyValuePair<string, JsonValue> item = new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true));
            JsonObject obj = new JsonObject(item);

            Assert.True(obj.ContainsKey(item.Key));
            Assert.False(obj.ContainsKey("abc"));

            ICollection<KeyValuePair<string, JsonValue>> iCollection = obj;
            Assert.True(iCollection.Contains(item));
            Assert.False(iCollection.Contains(new KeyValuePair<string, JsonValue>()));
        }

        [Fact]
        public void ContainsKey_NullKey_ThrowsArgumentNullException()
        {
            JsonObject obj = new JsonObject();
            AssertExtensions.Throws<ArgumentNullException>("key", () => obj.ContainsKey(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void CopyTo(int arrayIndex)
        {
            KeyValuePair<string, JsonValue>[] items = new KeyValuePair<string, JsonValue>[] { new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true)) };
            JsonObject array = new JsonObject(items);
            KeyValuePair<string, JsonValue>[] copy = new KeyValuePair<string, JsonValue>[array.Count + arrayIndex];
            array.CopyTo(copy, arrayIndex);

            for (int i = 0; i < arrayIndex; i++)
            {
                Assert.Equal(default(KeyValuePair<string, JsonValue>), copy[i]);
            }
            for (int i = arrayIndex; i < copy.Length; i++)
            {
                Assert.Equal(items[i - arrayIndex], copy[i]);
            }
        }

        [Fact]
        public void Remove()
        {
            KeyValuePair<string, JsonValue> item = new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true));
            JsonObject obj = new JsonObject(item);

            obj.Remove(item.Key);
            Assert.Equal(0, obj.Count);
            Assert.False(obj.ContainsKey(item.Key));

            obj.Remove(item.Key);
            Assert.Equal(0, obj.Count);
        }

        [Fact]
        public void Remove_NullKey_ThrowsArgumentNullException()
        {
            JsonObject obj = new JsonObject();
            AssertExtensions.Throws<ArgumentNullException>("key", () => obj.Remove(null));
        }

        [Fact]
        public void ICollection_Remove()
        {
            KeyValuePair<string, JsonValue> item = new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true));
            JsonObject obj = new JsonObject(item);
            ICollection<KeyValuePair<string, JsonValue>> iCollection = obj;

            iCollection.Remove(item);
            Assert.Equal(0, obj.Count);
            Assert.False(obj.ContainsKey(item.Key));

            iCollection.Remove(item);
            Assert.Equal(0, obj.Count);
        }

        [Fact]
        public void Save_Stream()
        {
            JsonObject obj = new JsonObject(new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true)), new KeyValuePair<string, JsonValue>("key2", null));
            using (MemoryStream stream = new MemoryStream())
            {
                obj.Save(stream);
                string result = Encoding.UTF8.GetString(stream.ToArray());
                Assert.Equal("{\"key\": true, \"key2\": null}", result);
            }
        }

        [Fact]
        public void Save_TextWriter()
        {
            JsonObject obj = new JsonObject(new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true)), new KeyValuePair<string, JsonValue>("key2", null));
            using (StringWriter writer = new StringWriter())
            {
                obj.Save(writer);
                Assert.Equal("{\"key\": true, \"key2\": null}", writer.ToString());
            }
        }

        [Fact]
        public void Save_NullStream_ThrowsArgumentNullException()
        {
            JsonObject obj = new JsonObject();
            AssertExtensions.Throws<ArgumentNullException>("stream", () => obj.Save((Stream)null));
            AssertExtensions.Throws<ArgumentNullException>("textWriter", () => obj.Save((TextWriter)null));
        }

        [Fact]
        public void GetEnumerator_GenericIEnumerable()
        {
            KeyValuePair<string, JsonValue>[] items = new KeyValuePair<string, JsonValue>[] { new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true)) };
            JsonObject obj = new JsonObject(items);

            IEnumerator<KeyValuePair<string, JsonValue>> enumerator = ((IEnumerable<KeyValuePair<string, JsonValue>>)obj).GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(items[counter].Key, enumerator.Current.Key);
                    Assert.Equal(items[counter].Value.ToString(), enumerator.Current.Value.ToString());
                    counter++;
                }
                Assert.Equal(obj.Count, counter);

                enumerator.Reset();
            }
        }

        [Fact]
        public void GetEnumerator_NonGenericIEnumerable()
        {
            KeyValuePair<string, JsonValue>[] items = new KeyValuePair<string, JsonValue>[] { new KeyValuePair<string, JsonValue>("key", new JsonPrimitive(true)) };
            JsonObject obj = new JsonObject(items);

            IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    KeyValuePair<string, JsonValue> current = (KeyValuePair<string, JsonValue>)enumerator.Current;
                    Assert.Equal(items[counter].Key, current.Key);
                    Assert.Equal(items[counter].Value.ToString(), current.Value.ToString());
                    counter++;
                }
                Assert.Equal(obj.Count, counter);

                enumerator.Reset();
            }
        }
    }
}
