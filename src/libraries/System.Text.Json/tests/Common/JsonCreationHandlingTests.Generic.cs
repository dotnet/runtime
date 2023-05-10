// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    internal struct StructList<T> : IList<T>, IList
    {
        private List<T> _list = new List<T>();
        // we track count separately to make sure tests are not passing by accident because we use reference to list inside of struct
        private int _count;

        public T this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public int Count => _count;
        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public object SyncRoot => this;

        public bool IsSynchronized => false;

        object IList.this[int index]
        {
            get => _list[index];
            set => _list[index] = (T)value;
        }

        public StructList() { }

        public void Add(T item)
        {
            _count++;
            _list.Add(item);
        }

        public void Clear()
        {
            _count = 0;
            _list.Clear();
        }

        public bool Contains(T item) => _list.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        public int IndexOf(T item) => _list.IndexOf(item);
        public void Insert(int index, T item)
        {
            _count++;
            _list.Insert(index, item);
        }
        public bool Remove(T item)
        {
            if (_list.Remove(item))
            {
                _count--;
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            _count--;
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Add(object value)
        {
            _count++;
            return ((IList)_list).Add(value);
        }

        public bool Contains(object value) => _list.Contains((T)value);
        public int IndexOf(object value) => _list.IndexOf((T)value);
        public void Insert(int index, object value)
        {
            _count++;
            _list.Insert(index, (T)value);
        }

        public void Remove(object value)
        {
            if (_list.Remove((T)value))
            {
                _count--;
            }
        }

        public void CopyTo(Array array, int index) => ((IList)_list).CopyTo(array, index);

        public void Validate()
        {
            // This can fail only if we modified a copy of this struct
            Assert.Equal(_count, _list.Count);
        }
    }

    internal struct StructCollection<T> : ICollection<T>
    {
        private List<T> _list = new List<T>();

        // we track count separately to make sure tests are not passing by accident because we use reference to list inside of struct
        private int _count;

        public int Count => _count;
        public bool IsReadOnly => false;

        public StructCollection() { }

        public void Add(T item)
        {
            _count++;
            _list.Add(item);
        }

        public void Clear()
        {
            _count = 0;
            _list.Clear();
        }

        public bool Contains(T item) => _list.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        public bool Remove(T item)
        {
            if (_list.Remove(item))
            {
                _count--;
                return true;
            }

            return false;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Validate()
        {
            // This can fail only if we modified a copy of this struct
            Assert.Equal(_count, _list.Count);
        }
    }

    internal struct StructSet<T> : ISet<T>
    {
        private HashSet<T> _set = new HashSet<T>();

        // we track count separately to make sure tests are not passing by accident because we use reference to list inside of struct
        private int _count;

        public int Count => _count;
        public bool IsReadOnly => false;

        public StructSet() { }

        public bool Add(T item)
        {
            if (_set.Add(item))
            {
                _count++;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _count = 0;
            _set.Clear();
        }

        public bool Contains(T item) => _set.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);
        public void ExceptWith(IEnumerable<T> other)
        {
            int prevCount = _set.Count;
            _set.ExceptWith(other);
            _count -= prevCount - _set.Count;
        }
        public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();

        public void IntersectWith(IEnumerable<T> other)
        {
            int prevCount = _set.Count;
            _set.IntersectWith(other);
            _count -= prevCount - _set.Count;
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);
        public bool Remove(T item)
        {
            if (_set.Remove(item))
            {
                _count--;
                return true;
            }

            return false;
        }
        public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            int prevCount = _set.Count;
            _set.SymmetricExceptWith(other);
            _count -= prevCount - _set.Count;
        }

        public void UnionWith(IEnumerable<T> other)
        {
            int prevCount = _set.Count;
            _set.UnionWith(other);
            _count -= prevCount - _set.Count;
        }

        void ICollection<T>.Add(T item)
        {
            if (_set.Add(item))
            {
                _count++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Validate()
        {
            // This can fail only if we modified a copy of this struct
            Assert.Equal(_count, _set.Count);
        }
    }

    internal struct StructDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
    {
        private Dictionary<TKey, TValue> _dict = new();

        // we track count separately to make sure tests are not passing by accident because we use reference to list inside of struct
        private int _count;

        public StructDictionary() { }

        public TValue this[TKey key]
        {
            get => _dict[key];
            set
            {
                int prevCount = _dict.Count;
                _dict[key] = value;
                _count += _dict.Count - prevCount;
            }
        }

        public ICollection<TKey> Keys => _dict.Keys;

        public ICollection<TValue> Values => _dict.Values;

        public int Count => _count;

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        ICollection IDictionary.Keys => ((IDictionary)_dict).Keys;

        ICollection IDictionary.Values => ((IDictionary)_dict).Values;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public object? this[object key] { get => this[(TKey)key]; set => this[(TKey)key] = (TValue)value; }

        public void Add(TKey key, TValue value)
        {
            int prevCount = _dict.Count;
            _dict.Add(key, value);
            _count += _dict.Count - prevCount;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            int prevCount = _dict.Count;
            ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Add(item);
            _count += _dict.Count - prevCount;
        }

        public void Clear()
        {
            _dict.Clear();
            _count = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) => _dict.Contains(item);
        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();

        public bool Remove(TKey key)
        {
            int prevCount = _dict.Count;
            bool ret = _dict.Remove(key);
            _count -= prevCount - _dict.Count;
            return ret;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            int prevCount = _dict.Count;
            bool ret = ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Remove(item);
            _count -= prevCount - _dict.Count;
            return ret;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(object key, object? value) => Add((TKey)key, (TValue)value);
        public bool Contains(object key) => ContainsKey((TKey)key);
        IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)_dict).GetEnumerator();
        public void Remove(object key) => Remove((TKey)key);
        public void CopyTo(Array array, int index) => ((IDictionary)_dict).CopyTo(array, index);

        public void Validate()
        {
            // This can fail only if we modified a copy of this struct
            Assert.Equal(_count, _dict.Count);
        }
    }

    public abstract partial class JsonCreationHandlingTests : SerializerTests
    {
        public JsonCreationHandlingTests(JsonSerializerWrapper serializerUnderTest) : base(serializerUnderTest)
        {
        }

        [Theory]
        [InlineData(typeof(ClassWithWritableProperty<int>))]
        [InlineData(typeof(ClassWithWritableProperty<int?>))]
        [InlineData(typeof(ClassWithWritableProperty<int[]>))]
        [InlineData(typeof(ClassWithWritableProperty<List<int>>))] // custom converter
        [InlineData(typeof(ClassWithWritableProperty<IEnumerable<int>>))]
        [InlineData(typeof(ClassWithWritableProperty<IEnumerable>))]
        [InlineData(typeof(ClassWithWritableProperty<ImmutableArray<int>>))]
        [InlineData(typeof(ClassWithWritableProperty<ImmutableHashSet<int>>))]
        [InlineData(typeof(ClassWithWritableProperty<ImmutableList<int>>))]
        [InlineData(typeof(ClassWithWritableProperty<ImmutableQueue<int>>))]
        [InlineData(typeof(ClassWithWritableProperty<ImmutableStack<int>>))]
        public async Task CreationHandlingSetWithAttribute_PopulateWithInvalidTypeThrows(Type type)
        {
            JsonSerializerOptions options = Serializer.CreateOptions(customConverters: new() { new ThrowingCustomConverter<List<int>>() });
            string json = "{}";
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper(json, type, options));

            Assert.Throws<InvalidOperationException>(() => options.GetTypeInfo(type));
        }

        [Theory]
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<int>))]
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<int?>))]
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<int[]>))]
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<List<int>>))] // custom converter
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<IEnumerable<int>>))]
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<IEnumerable>))]
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<ImmutableArray<int>>))]
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<ImmutableHashSet<int>>))]
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<ImmutableList<int>>))]
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<ImmutableQueue<int>>))]
        [InlineData(typeof(ClassWithWritablePropertyWithoutPopulate<ImmutableStack<int>>))]
        public async Task CreationHandlingSetWithAttribute_PopulateSetWithModifierWithInvalidTypeThrows(Type type)
        {
            Action<JsonTypeInfo> modifier = (ti) =>
            {
                if (ti.Type == type)
                {
                    Assert.Equal(1, ti.Properties.Count);
                    JsonPropertyInfo prop = ti.Properties[0];
                    Assert.Null(prop.ObjectCreationHandling);
                    prop.ObjectCreationHandling = JsonObjectCreationHandling.Populate;
                }
            };

            JsonSerializerOptions options = Serializer.CreateOptions(
                customConverters: new() { new ThrowingCustomConverter<List<int>>() },
                modifier: modifier);

            string json = "{}";
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper(json, type, options));

            Assert.Throws<InvalidOperationException>(() => options.GetTypeInfo(type));
        }

        [Theory]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(IList<int>))]
        [InlineData(typeof(IList))]
        [InlineData(typeof(Queue<int>))]
        [InlineData(typeof(Queue))]
        [InlineData(typeof(ConcurrentQueue<int>))]
        [InlineData(typeof(Stack<int>))]
        [InlineData(typeof(Stack))]
        [InlineData(typeof(ConcurrentStack<int>))]
        [InlineData(typeof(ICollection<int>))]
        [InlineData(typeof(ISet<int>))]
        [InlineData(typeof(Dictionary<string, int>))]
        [InlineData(typeof(IDictionary<string, int>))]
        [InlineData(typeof(IDictionary))]
        [InlineData(typeof(ConcurrentDictionary<string, int>))]
        [InlineData(typeof(SortedDictionary<string, int>))]
        public Task CreationHandlingSetWithAttribute_PopulatedPropertyDeserializeNull(Type type)
        {
            return (Task)typeof(JsonCreationHandlingTests)
                .GetMethod(nameof(CreationHandling_PopulatedPropertyDeserializeNullGeneric), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(type).Invoke(this, null);
        }

        private async Task CreationHandling_PopulatedPropertyDeserializeNullGeneric<T>()
        {
            JsonSerializerOptions options = Serializer.CreateOptions();
            string json = """{"Property":null}""";
            var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<T>>(json, options);
            Assert.Null(obj.Property);
        }

        [Theory]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(IList<int>))]
        [InlineData(typeof(IList))]
        [InlineData(typeof(Queue<int>))]
        [InlineData(typeof(Queue))]
        [InlineData(typeof(ConcurrentQueue<int>))]
        [InlineData(typeof(Stack<int>))]
        [InlineData(typeof(Stack))]
        [InlineData(typeof(ConcurrentStack<int>))]
        [InlineData(typeof(ICollection<int>))]
        [InlineData(typeof(ISet<int>))]
        [InlineData(typeof(Dictionary<string, int>))]
        [InlineData(typeof(IDictionary<string, int>))]
        [InlineData(typeof(IDictionary))]
        [InlineData(typeof(ConcurrentDictionary<string, int>))]
        [InlineData(typeof(SortedDictionary<string, int>))]
        public Task CreationHandlingSetWithAttribute_PopulatedPropertyDeserializeNullOnReadOnlyProperty(Type type)
        {
            return (Task)typeof(JsonCreationHandlingTests)
                .GetMethod(nameof(CreationHandling_PopulatedPropertyDeserializeNullOnReadOnlyPropertyGeneric), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(type).Invoke(this, null);
        }

        private async Task CreationHandling_PopulatedPropertyDeserializeNullOnReadOnlyPropertyGeneric<T>()
        {
            JsonSerializerOptions options = Serializer.CreateOptions();
            string json = """{"Property":null}""";
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty<T>>(json, options));
        }

        [Theory]
        [InlineData(typeof(List<int>), true)]
        [InlineData(typeof(IList<int>), true)]
        [InlineData(typeof(IList), true)]
        [InlineData(typeof(Queue<int>), true)]
        [InlineData(typeof(Queue), true)]
        [InlineData(typeof(ConcurrentQueue<int>), true)]
        [InlineData(typeof(Stack<int>), true)]
        [InlineData(typeof(Stack), true)]
        [InlineData(typeof(ConcurrentStack<int>), true)]
        [InlineData(typeof(ICollection<int>), true)]
        [InlineData(typeof(ISet<int>), true)]
        [InlineData(typeof(Dictionary<string, int>), false)]
        [InlineData(typeof(IDictionary<string, int>), false)]
        [InlineData(typeof(IDictionary), false)]
        [InlineData(typeof(ConcurrentDictionary<string, int>), false)]
        [InlineData(typeof(SortedDictionary<string, int>), false)]
        [InlineData(typeof(StructList<int>?), true)]
        [InlineData(typeof(StructCollection<int>?), true)]
        [InlineData(typeof(StructSet<int>?), true)]
        [InlineData(typeof(StructDictionary<string, int>?), false)]
        public Task CreationHandlingSetWithAttribute_PopulatedPropertyDeserializeInitiallyNull(Type type, bool isArray)
        {
            return (Task)typeof(JsonCreationHandlingTests)
                .GetMethod(nameof(CreationHandling_PopulatedPropertyDeserializeInitiallyNullGeneric), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(type).Invoke(this, new object[] { isArray });
        }

        private async Task CreationHandling_PopulatedPropertyDeserializeInitiallyNullGeneric<T>(bool isArray)
        {
            JsonSerializerOptions options = Serializer.CreateOptions();
            string json = isArray ? """{"Property":[1,2,3]}""" : """{"Property":{"a":1,"b":2,"c":3}}""";

            if (typeof(T).IsValueType)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty<T>>("{}", options));
            }
            else
            {
                var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty<T>>("{}", options);
                Assert.Null(obj.Property);

                obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty<T>>(json, options);
                Assert.Null(obj.Property);
            }

            {
                var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<T>>("{}", options);
                Assert.Null(obj.Property);

                obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<T>>(json, options);
                Assert.NotNull(obj.Property);
            }
        }

        private static void CheckGenericDictionaryContent(IDictionary<string, int> dict, int expectedNumberOfElements = 6)
        {
            Assert.Equal(expectedNumberOfElements, dict.Count);

            for (int i = 0; i < expectedNumberOfElements; i++)
            {
                string expectedKey = ((char)('a' + i)).ToString();
                int expectedValue = i + 1;
                Assert.True(dict.ContainsKey(expectedKey), $"Dictionary does not contain '{expectedKey}' key.");
                Assert.Equal(expectedValue, dict[expectedKey]);
            }
        }

        private static void CheckDictionaryContent(IDictionary dict, int expectedNumberOfElements = 6)
        {
            Assert.Equal(expectedNumberOfElements, dict.Count);

            for (int i = 0; i < expectedNumberOfElements; i++)
            {
                string expectedKey = ((char)('a' + i)).ToString();
                int expectedValue = i + 1;
                Assert.True(dict.Contains(expectedKey), $"Dictionary does not contain '{expectedKey}' key.");
                Assert.Equal(expectedValue, ((JsonElement)dict[expectedKey]).GetInt32());
            }
        }

        private static Action<JsonTypeInfo> GetFirstPropertyToPopulateForTypeModifier(Type type) =>
            ti =>
            {
                if (ti.Type == type)
                {
                    Assert.True(ti.Properties.Count > 0);
                    JsonPropertyInfo prop = ti.Properties[0];
                    prop.ObjectCreationHandling = JsonObjectCreationHandling.Populate;
                }
            };

        private static void CheckTypeHasSinglePropertyWithPopulateHandling(JsonSerializerOptions options, Type type)
        {
            JsonTypeInfo typeInfo = options.GetTypeInfo(type);
            Assert.Equal(1, typeInfo.Properties.Count);
            JsonPropertyInfo propertyInfo = typeInfo.Properties[0];
            Assert.Equal(JsonObjectCreationHandling.Populate, propertyInfo.ObjectCreationHandling);
        }

        internal class ClassWithReadOnlyProperty<T>
        {
            [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
            public T Property { get; }
        }

        internal class ClassWithReadOnlyInitializedProperty<T> where T : new()
        {
            [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
            public T Property { get; } = new();
        }

        internal class ClassWithInitializedProperty<T> where T : new()
        {
            [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
            public T Property { get; set; } = new();
        }

        internal class ClassWithReadOnlyInitializedField<T> where T : new()
        {
            [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
            public readonly T Field = new();
        }

        internal class ClassWithInitializedField<T> where T : new()
        {
            [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
            public T Field = new();
        }

        internal class ClassWithWritableProperty<T>
        {
            [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
            public T Property { get; set; }
        }

        internal class ClassWithWritablePropertyWithoutPopulate<T>
        {
            public T Property { get; set; }
        }

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        internal class ClassWithDefaultPopulateAndProperty<T>
        {
            public T Property { get; set; }
        }

        internal class ThrowingCustomConverter<T> : JsonConverter<T>
        {
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Assert.True(false, "This converter should never be used");
                return default;
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                Assert.True(false, "This converter should never be used");
            }
        }
    }
}
