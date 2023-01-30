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

namespace System.Text.Json.Serialization.Tests;

public sealed partial class JsonCreationHandlingTests_String : JsonCreationHandlingTests
{
    public JsonCreationHandlingTests_String() : base(JsonSerializerWrapper.StringSerializer) { }
}
public sealed partial class JsonCreationHandlingTests_SourceGen_String : JsonCreationHandlingTests
{
    public JsonCreationHandlingTests_SourceGen_String() : base(JsonSerializerWrapper.StringSerializer, useSourceGen: true) { }
}
public sealed partial class JsonCreationHandlingTests_AsyncStream : JsonCreationHandlingTests
{
    public JsonCreationHandlingTests_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
}
public sealed partial class JsonCreationHandlingTests_AsyncStreamWithSmallBuffer : JsonCreationHandlingTests
{
    public JsonCreationHandlingTests_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
}
public sealed partial class JsonCreationHandlingTests_SourceGen_AsyncStreamWithSmallBuffer : JsonCreationHandlingTests
{
    public JsonCreationHandlingTests_SourceGen_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer, useSourceGen: true) { }
}
public sealed partial class JsonCreationHandlingTests_SyncStream : JsonCreationHandlingTests
{
    public JsonCreationHandlingTests_SyncStream() : base(JsonSerializerWrapper.SyncStreamSerializer) { }
}

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
    private bool _useSourceGen;

    public JsonCreationHandlingTests(JsonSerializerWrapper serializerUnderTest, bool useSourceGen = false) : base(serializerUnderTest)
    {
        _useSourceGen = useSourceGen;
    }

    protected JsonSerializerOptions CreateOptions(Action<JsonSerializerOptions> configure = null, bool includeFields = false, List<JsonConverter> customConverters = null, Action<JsonTypeInfo> modifier = null)
    {
        IJsonTypeInfoResolver resolver = _useSourceGen ? CreationHandlingTestContext.Default : new DefaultJsonTypeInfoResolver();
        resolver = modifier != null ? resolver.WithModifier(modifier) : resolver;

        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = resolver,
            IncludeFields = includeFields,
        };

        if (customConverters != null)
        {
            foreach (var converter in customConverters)
            {
                options.Converters.Add(converter);
            }
        }

        configure?.Invoke(options);

        options.MakeReadOnly();

        return options;
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
        JsonSerializerOptions options = CreateOptions(customConverters: new() { new ThrowingCustomConverter<List<int>>() });
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

        JsonSerializerOptions options = CreateOptions(
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
        JsonSerializerOptions options = CreateOptions();
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
        JsonSerializerOptions options = CreateOptions();
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
        JsonSerializerOptions options = CreateOptions();
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

    private static void CheckFirstPropertyIsPopulated(JsonSerializerOptions options, Type type)
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

    [JsonSerializable(typeof(ClassWithReadOnlyPropertyListOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyListOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyListOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyListOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElement))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElement))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElementWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElementWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructListOfInt))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructListOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructListOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructListOfIntWithNumberHandling))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructListOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructListOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructListOfInt))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructListOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructListOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyQueueOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyQueueOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyQueueOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyQueueOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyQueue))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyQueueWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyQueueWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentQueueOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentQueueOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentQueueOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyStackOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyStackOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyStackOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyStackOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyStack))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyStackWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyStackWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentStackOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentStackOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentStackOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructCollectionOfInt))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructCollectionOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructCollectionOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritableFieldStructCollectionOfIntWithNumberHandling))]
    [JsonSerializable(typeof(StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructCollectionOfInt))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructCollectionOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructCollectionOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructSetOfInt))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructSetOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructSetOfIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructSetOfIntWithNumberHandling))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructSetOfInt))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructSetOfIntWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructSetOfIntWithoutPopulateAttribute))]

    [JsonSerializable(typeof(ClassWithReadOnlyPropertyDictionaryOfStringToInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyDictionaryOfStringToIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyDictionaryOfStringToIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElement))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElement))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElementWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElementWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructDictionaryOfStringToInt))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructDictionaryOfStringToIntWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructDictionaryOfStringToIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandling))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructDictionaryOfStringToInt))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructDictionaryOfStringToIntWithAttributeOnType))]
    [JsonSerializable(typeof(StructWithWritableFieldNullableStructDictionaryOfStringToIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToInt))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithoutPopulateAttribute))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandling))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType))]
    [JsonSerializable(typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))]

    [JsonSerializable(typeof(ClassWithReadOnlyProperty_SimpleClass))]
    [JsonSerializable(typeof(ClassWithWritableProperty_SimpleClass))]
    [JsonSerializable(typeof(StructWithReadOnlyProperty_SimpleStruct))]
    [JsonSerializable(typeof(StructWithWritableProperty_SimpleStruct))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty_SimpleClassWithSmallParametrizedCtor))]
    [JsonSerializable(typeof(ClassWithWritableProperty_SimpleClassWithSmallParametrizedCtor))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty_SimpleClassWithLargeParametrizedCtor))]
    [JsonSerializable(typeof(ClassWithWritableProperty_SimpleClassWithLargeParametrizedCtor))]
    [JsonSerializable(typeof(ClassWithProperty_BaseClassWithPolymorphism))]
    [JsonSerializable(typeof(ClassWithProperty_BaseClassWithPolymorphismOnSerializationOnly))]
    [JsonSerializable(typeof(BaseClassRecursive))]
    [JsonSerializable(typeof(ClassWithClassProperty))]

    // TODO: Consider support for following syntax [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<int>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<int?>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<int[]>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<List<int>>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<IEnumerable<int>>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<IEnumerable>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<ImmutableArray<int>>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<ImmutableHashSet<int>>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<ImmutableList<int>>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<ImmutableQueue<int>>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<ImmutableStack<int>>))]

    // TODO: Consider support for following syntax [JsonSerializable(typeof(ClassWithWritableProperty<>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<int>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<int?>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<int[]>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<IEnumerable<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<IEnumerable>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<ImmutableArray<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<ImmutableHashSet<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<ImmutableList<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<ImmutableQueue<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<ImmutableStack<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<List<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<IList<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<IList>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<Queue<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<Queue>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<ConcurrentQueue<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<Stack<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<Stack>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<ConcurrentStack<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<ICollection<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<ISet<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<Dictionary<string, int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<IDictionary<string, int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<IDictionary>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<ConcurrentDictionary<string, int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<SortedDictionary<string, int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<StructList<int>?>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<StructCollection<int>?>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<StructSet<int>?>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<StructDictionary<string, int>?>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<Stack<int>>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<Stack>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<SimpleClass>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<SimpleClassWithSmallParametrizedCtor>))]
    [JsonSerializable(typeof(ClassWithWritableProperty<SimpleClassWithLargeParametrizedCtor>))]

    // TODO: Consider support for following syntax [JsonSerializable(typeof(ClassWithReadOnlyProperty<>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<List<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<IList<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<IList>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<Queue<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<Queue>), TypeInfoPropertyName = "NonGenericClassWithReadOnlyPropertyOfQueue")]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<ConcurrentQueue<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<Stack<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<Stack>), TypeInfoPropertyName = "NonGenericClassWithReadOnlyPropertyOfStack")]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<ConcurrentStack<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<ICollection<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<ISet<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<Dictionary<string, int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<IDictionary<string, int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<IDictionary>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<ConcurrentDictionary<string, int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<SortedDictionary<string, int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<StructList<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<StructList<int>?>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<StructCollection<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<StructCollection<int>?>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<StructSet<int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<StructSet<int>?>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<StructDictionary<string, int>>))]
    [JsonSerializable(typeof(ClassWithReadOnlyProperty<StructDictionary<string, int>?>))]

    [JsonSerializable(typeof(ClassWithReadOnlyInitializedField<SimpleClass>))]
    [JsonSerializable(typeof(ClassWithInitializedField<SimpleClass>))]
    [JsonSerializable(typeof(ClassWithReadOnlyInitializedProperty<SimpleClass>))]
    [JsonSerializable(typeof(ClassWithInitializedProperty<SimpleClass>))]
    [JsonSerializable(typeof(ClassWithWritablePropertyWithoutPopulate<SimpleClass>))]
    [JsonSerializable(typeof(ClassWithDefaultPopulateAndProperty<SimpleClass>))]
    [JsonSerializable(typeof(ClassWithInitializedProperty<ClassWithRequiredProperty>))]
    [JsonSerializable(typeof(ClassWithRecursiveRequiredProperty))]
    [JsonSerializable(typeof(ClassImplementingInterfaceWithPopulateOnTypeWithInterfaceProperty))]
    [JsonSerializable(typeof(ClassImplementingInterfaceWithInterfaceProperty))]
    [JsonSerializable(typeof(IInterfaceWithInterfacePropertyWithPopulateOnType))]
    [JsonSerializable(typeof(IInterfaceWithInterfaceProperty))]
    [JsonSerializable(typeof(ClassWithReplaceOnTypeAndWritableProperty_SimpleClass))]
    [JsonSerializable(typeof(SimpleClassWitNonPopulatableProperty))]
    internal partial class CreationHandlingTestContext : JsonSerializerContext
    {
    }
}
