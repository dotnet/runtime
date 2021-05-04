// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class SimpleTestClassWithGenericCollectionWrappers : ITestClass
    {
        public GenericICollectionWrapper<string> MyStringICollectionWrapper { get; set; }
        public StringIListWrapper MyStringIListWrapper { get; set; }
        public StringISetWrapper MyStringISetWrapper { get; set; }
        public GenericIDictionaryWrapper<string, string> MyStringToStringIDictionaryWrapper { get; set; }
        public StringListWrapper MyStringListWrapper { get; set; }
        public StringStackWrapper MyStringStackWrapper { get; set; }
        public StringQueueWrapper MyStringQueueWrapper { get; set; }
        public StringHashSetWrapper MyStringHashSetWrapper { get; set; }
        public StringLinkedListWrapper MyStringLinkedListWrapper { get; set; }
        public StringSortedSetWrapper MyStringSortedSetWrapper { get; set; }
        public StringToStringDictionaryWrapper MyStringToStringDictionaryWrapper { get; set; }
        public StringToStringSortedDictionaryWrapper MyStringToStringSortedDictionaryWrapper { get; set; }
        public StringToGenericDictionaryWrapper<StringToGenericDictionaryWrapper<string>> MyStringToGenericDictionaryWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringICollectionWrapper"" : [""Hello""]," +
            @"""MyStringIListWrapper"" : [""Hello""]," +
            @"""MyStringISetWrapper"" : [""Hello""]," +
            @"""MyStringToStringIDictionaryWrapper"" : {""key"" : ""value""}," +
            @"""MyStringListWrapper"" : [""Hello""]," +
            @"""MyStringStackWrapper"" : [""Hello""]," +
            @"""MyStringQueueWrapper"" : [""Hello""]," +
            @"""MyStringHashSetWrapper"" : [""Hello""]," +
            @"""MyStringLinkedListWrapper"" : [""Hello""]," +
            @"""MyStringSortedSetWrapper"" : [""Hello""]," +
            @"""MyStringToStringDictionaryWrapper"" : {""key"" : ""value""}," +
            @"""MyStringToStringSortedDictionaryWrapper"" : {""key"" : ""value""}," +
            @"""MyStringToGenericDictionaryWrapper"" : {""key"" : {""key"" : ""value""}}" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize()
        {
            MyStringICollectionWrapper = new GenericICollectionWrapper<string>() { "Hello" };
            MyStringIListWrapper = new StringIListWrapper() { "Hello" };
            MyStringISetWrapper = new StringISetWrapper() { "Hello" };
            MyStringToStringIDictionaryWrapper = new GenericIDictionaryWrapper<string, string>() { { "key", "value" } };
            MyStringListWrapper = new StringListWrapper() { "Hello" };
            MyStringStackWrapper = new StringStackWrapper(new List<string> { "Hello" });
            MyStringQueueWrapper = new StringQueueWrapper(new List<string> { "Hello" });
            MyStringHashSetWrapper = new StringHashSetWrapper() { "Hello" };
            MyStringLinkedListWrapper = new StringLinkedListWrapper(new List<string> { "Hello" });
            MyStringSortedSetWrapper = new StringSortedSetWrapper() { "Hello" };
            MyStringToStringDictionaryWrapper = new StringToStringDictionaryWrapper() { { "key", "value" } };
            MyStringToStringSortedDictionaryWrapper = new StringToStringSortedDictionaryWrapper() { { "key", "value" } };
            MyStringToGenericDictionaryWrapper = new StringToGenericDictionaryWrapper<StringToGenericDictionaryWrapper<string>>() { { "key", new StringToGenericDictionaryWrapper<string>() { { "key", "value" } } } };
        }

        public void Verify()
        {
            Assert.Equal("Hello", MyStringICollectionWrapper.First());
            Assert.Equal("Hello", MyStringIListWrapper[0]);
            Assert.Equal("Hello", MyStringISetWrapper.First());
            Assert.Equal("value", MyStringToStringIDictionaryWrapper["key"]);
            Assert.Equal("Hello", MyStringListWrapper[0]);
            Assert.Equal("Hello", MyStringStackWrapper.First());
            Assert.Equal("Hello", MyStringQueueWrapper.First());
            Assert.Equal("Hello", MyStringHashSetWrapper.First());
            Assert.Equal("Hello", MyStringLinkedListWrapper.First());
            Assert.Equal("Hello", MyStringSortedSetWrapper.First());
            Assert.Equal("value", MyStringToStringDictionaryWrapper["key"]);
            Assert.Equal("value", MyStringToStringSortedDictionaryWrapper["key"]);
            Assert.Equal("value", MyStringToGenericDictionaryWrapper["key"]["key"]);
        }
    }

    public class SimpleTestClassWithStringIEnumerableWrapper
    {
        public StringIEnumerableWrapper MyStringIEnumerableWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringIEnumerableWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringIEnumerableWrapper = new StringIEnumerableWrapper(new List<string>{ "Hello" });
        }
    }

    public class SimpleTestClassWithStringIReadOnlyCollectionWrapper
    {
        public WrapperForIReadOnlyCollectionOfT<string> MyStringIReadOnlyCollectionWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringIReadOnlyCollectionWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringIReadOnlyCollectionWrapper = new WrapperForIReadOnlyCollectionOfT<string>(new List<string> { "Hello" });
        }
    }

    public class SimpleTestClassWithStringIReadOnlyListWrapper
    {
        public StringIReadOnlyListWrapper MyStringIReadOnlyListWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringIReadOnlyListWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringIReadOnlyListWrapper = new StringIReadOnlyListWrapper(new List<string> { "Hello" });
        }
    }

    public class SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper
    {
        public GenericIReadOnlyDictionaryWrapper<string, string> MyStringToStringIReadOnlyDictionaryWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringToStringIReadOnlyDictionaryWrapper"" : {""key"" : ""value""}" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringToStringIReadOnlyDictionaryWrapper = new GenericIReadOnlyDictionaryWrapper<string, string>(
                new Dictionary<string, string>() { { "key", "value" } });
        }
    }

    public class StringIEnumerableWrapper : IEnumerable<string>
    {
        private readonly List<string> _list = new List<string>();

        public StringIEnumerableWrapper() { }

        public StringIEnumerableWrapper(List<string> items)
        {
            _list = items;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    public class GenericIEnumerableWrapper<T> : IEnumerable<T>
    {
        private readonly List<T> _list = new List<T>();

        public GenericIEnumerableWrapper() { }

        public GenericIEnumerableWrapper(List<T> items)
        {
            _list = items;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)_list).GetEnumerator();
        }
    }

    public class GenericIEnumerableWrapperPrivateConstructor<T> : GenericIEnumerableWrapper<T>
    {
        private GenericIEnumerableWrapperPrivateConstructor() { }
    }

    public class GenericIEnumerableWrapperInternalConstructor<T> : GenericIEnumerableWrapper<T>
    {
        internal GenericIEnumerableWrapperInternalConstructor() { }
    }

    public class ReadOnlyStringICollectionWrapper : GenericICollectionWrapper<string>
    {
        public override bool IsReadOnly => true;
    }

    public class StringIListWrapper : IList<string>
    {
        private readonly List<string> _list = new List<string>();

        public string this[int index] { get => _list[index]; set => _list[index] = value; }

        public int Count => _list.Count;

        public virtual bool IsReadOnly => ((IList<string>)_list).IsReadOnly;

        public virtual void Add(string item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(string item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IList<string>)_list).GetEnumerator();
        }

        public int IndexOf(string item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(string item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<string>)_list).GetEnumerator();
        }
    }

    public class ReadOnlyStringIListWrapper : StringIListWrapper
    {
        public override bool IsReadOnly => true;
    }

    public class GenericIListWrapper<T> : IList<T>
    {
        private readonly List<T> _list = new List<T>();

        public T this[int index] { get => _list[index]; set => _list[index] = value; }

        public int Count => _list.Count;

        public bool IsReadOnly => ((IList<T>)_list).IsReadOnly;

        public void Add(T item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IList<T>)_list).GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<T>)_list).GetEnumerator();
        }
    }

    public class GenericIListWrapperPrivateConstructor<T> : GenericIListWrapper<T>
    {
        private GenericIListWrapperPrivateConstructor() { }
    }

    public class GenericIListWrapperInternalConstructor<T> : GenericIListWrapper<T>
    {
        internal GenericIListWrapperInternalConstructor() { }
    }

    public class GenericICollectionWrapper<T> : ICollection<T>
    {
        private readonly List<T> _list;

        public GenericICollectionWrapper()
        {
            _list = new List<T>();
        }

        public GenericICollectionWrapper(IEnumerable<T> items)
        {
            _list = new List<T>(items);
        }

        public int Count => _list.Count;

        public virtual bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

        public void Add(T item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((ICollection<T>)_list).GetEnumerator();
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<T>)_list).GetEnumerator();
        }
    }

    public class GenericICollectionWrapperPrivateConstructor<T> : GenericICollectionWrapper<T>
    {
        private GenericICollectionWrapperPrivateConstructor() { }
    }

    public class GenericICollectionWrapperInternalConstructor<T> : GenericICollectionWrapper<T>
    {
        internal GenericICollectionWrapperInternalConstructor() { }
    }

    public class WrapperForIReadOnlyCollectionOfT<T> : IReadOnlyCollection<T>
    {
        private readonly List<T> _list;

        public WrapperForIReadOnlyCollectionOfT()
        {
            _list = new List<T>();
        }

        public WrapperForIReadOnlyCollectionOfT(IEnumerable<T> items)
        {
            _list = new List<T>(items);
        }

        public int Count => _list.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return ((IReadOnlyCollection<T>)_list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyCollection<T>)_list).GetEnumerator();
        }
    }

    public class GenericIReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>
    {
        private readonly List<T> _list = new List<T>();

        public GenericIReadOnlyCollectionWrapper() { }

        public GenericIReadOnlyCollectionWrapper(List<T> list)
        {
            _list = list;
        }

        public int Count => _list.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return ((IReadOnlyCollection<T>)_list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyCollection<T>)_list).GetEnumerator();
        }
    }

    public class StringIReadOnlyListWrapper : IReadOnlyList<string>
    {
        private readonly List<string> _list = new List<string>();

        public StringIReadOnlyListWrapper() { }

        public StringIReadOnlyListWrapper(List<string> list)
        {
            _list = list;
        }

        public string this[int index] => _list[index];

        public int Count => _list.Count;

        public IEnumerator<string> GetEnumerator()
        {
            return ((IReadOnlyList<string>)_list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyList<string>)_list).GetEnumerator();
        }
    }

    public class GenericIReadOnlyListWrapper<T> : IReadOnlyList<T>
    {
        private readonly List<T> _list = new List<T>();

        public GenericIReadOnlyListWrapper() { }

        public GenericIReadOnlyListWrapper(List<T> list)
        {
            _list = list;
        }

        public T this[int index] => _list[index];

        public int Count => _list.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return ((IReadOnlyList<T>)_list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyList<T>)_list).GetEnumerator();
        }
    }

    public class ReadOnlyStringISetWrapper: StringISetWrapper
    {
        public override bool IsReadOnly => true;
    }

    public class StringISetWrapper : ISet<string>
    {
        private readonly HashSet<string> _hashset = new HashSet<string>();

        public int Count => _hashset.Count;

        public virtual bool IsReadOnly => ((ISet<string>)_hashset).IsReadOnly;

        public bool Add(string item)
        {
            return _hashset.Add(item);
        }

        public void Clear()
        {
            _hashset.Clear();
        }

        public bool Contains(string item)
        {
            return _hashset.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _hashset.CopyTo(array, arrayIndex);
        }

        public void ExceptWith(IEnumerable<string> other)
        {
            _hashset.ExceptWith(other);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((ISet<string>)_hashset).GetEnumerator();
        }

        public void IntersectWith(IEnumerable<string> other)
        {
            _hashset.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<string> other)
        {
            return _hashset.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<string> other)
        {
            return _hashset.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<string> other)
        {
            return _hashset.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<string> other)
        {
            return _hashset.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<string> other)
        {
            return _hashset.Overlaps(other);
        }

        public bool Remove(string item)
        {
            return _hashset.Remove(item);
        }

        public bool SetEquals(IEnumerable<string> other)
        {
            return _hashset.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<string> other)
        {
            _hashset.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<string> other)
        {
            _hashset.UnionWith(other);
        }

        void ICollection<string>.Add(string item)
        {
            _hashset.Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ISet<string>)_hashset).GetEnumerator();
        }
    }

    public class GenericISetWrapper<T> : ISet<T>
    {
        private readonly HashSet<T> _hashset = new HashSet<T>();

        public int Count => _hashset.Count;

        public bool IsReadOnly => ((ISet<T>)_hashset).IsReadOnly;

        public bool Add(T item)
        {
            return _hashset.Add(item);
        }

        public void Clear()
        {
            _hashset.Clear();
        }

        public bool Contains(T item)
        {
            return _hashset.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _hashset.CopyTo(array, arrayIndex);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            _hashset.ExceptWith(other);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((ISet<T>)_hashset).GetEnumerator();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            _hashset.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _hashset.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _hashset.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _hashset.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _hashset.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _hashset.Overlaps(other);
        }

        public bool Remove(T item)
        {
            return _hashset.Remove(item);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _hashset.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            _hashset.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            _hashset.UnionWith(other);
        }

        void ICollection<T>.Add(T item)
        {
            _hashset.Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ISet<T>)_hashset).GetEnumerator();
        }
    }

    public class GenericISetWrapperPrivateConstructor<T> : GenericISetWrapper<T>
    {
        private GenericISetWrapperPrivateConstructor() { }
    }

    public class GenericISetWrapperInternalConstructor<T> : GenericISetWrapper<T>
    {
        internal GenericISetWrapperInternalConstructor() { }
    }

    public class GenericIDictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dict;

        public GenericIDictionaryWrapper()
        {
            _dict = new Dictionary<TKey, TValue>();
        }

        public GenericIDictionaryWrapper(IDictionary<TKey, TValue> items)
        {
            _dict = new Dictionary<TKey, TValue>(items);
        }

        public TValue this[TKey key] { get => ((IDictionary<TKey, TValue>)_dict)[key]; set => ((IDictionary<TKey, TValue>)_dict)[key] = value; }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)_dict).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)_dict).Values;

        public int Count => ((IDictionary<TKey, TValue>)_dict).Count;

        public virtual bool IsReadOnly => ((IDictionary<TKey, TValue>)_dict).IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)_dict).Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)_dict).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<TKey, TValue>)_dict).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_dict).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_dict).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)_dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)_dict).GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_dict).Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_dict).Remove(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>)_dict).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)_dict).GetEnumerator();
        }
    }

    public class GenericIDictionaryWrapperPrivateConstructor<TKey, TValue> : GenericIDictionaryWrapper<TKey, TValue>
    {
        private GenericIDictionaryWrapperPrivateConstructor() { }
    }

    public class GenericIDictionaryWrapperInternalConstructor<TKey, TValue> : GenericIDictionaryWrapper<TKey, TValue>
    {
        internal GenericIDictionaryWrapperInternalConstructor() { }
    }

    public class GenericIDictonaryWrapperThreeGenericParameters<TKey, TValue, TUnused> : GenericIDictionaryWrapper<TKey, TValue> { }

    public class ReadOnlyStringToStringIDictionaryWrapper : GenericIDictionaryWrapper<string, string>
    {
        public override bool IsReadOnly => true;
    }

    public class StringToObjectIDictionaryWrapper : GenericIDictionaryWrapper<string, object> { }

    public class StringToGenericIDictionaryWrapper<TValue> : GenericIDictionaryWrapper<string, TValue> { }

    public class GenericIReadOnlyDictionaryWrapper<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dictionary;

        public GenericIReadOnlyDictionaryWrapper()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        public GenericIReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> items)
        {
            _dictionary = new Dictionary<TKey, TValue>(items);
        }

        public TValue this[TKey key] => ((IReadOnlyDictionary<TKey, TValue>)_dictionary)[key];

        public IEnumerable<TKey> Keys => ((IReadOnlyDictionary<TKey, TValue>)_dictionary).Keys;

        public IEnumerable<TValue> Values => ((IReadOnlyDictionary<TKey, TValue>)_dictionary).Values;

        public int Count => ((IReadOnlyDictionary<TKey, TValue>)_dictionary).Count;

        public bool ContainsKey(TKey key)
        {
            return ((IReadOnlyDictionary<TKey, TValue>)_dictionary).ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IReadOnlyDictionary<TKey, TValue>)_dictionary).GetEnumerator();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IReadOnlyDictionary<TKey, TValue>)_dictionary).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyDictionary<TKey, TValue>)_dictionary).GetEnumerator();
        }
    }

    public class StringToStringIReadOnlyDictionaryWrapperPrivateConstructor : GenericIReadOnlyDictionaryWrapper<string, string>
    {
        private StringToStringIReadOnlyDictionaryWrapperPrivateConstructor() { }
    }

    public class StringToStringIReadOnlyDictionaryWrapperInternalConstructor : GenericIReadOnlyDictionaryWrapper<string, string>
    {
        internal StringToStringIReadOnlyDictionaryWrapperInternalConstructor() { }
    }

    public class StringListWrapper : List<string> { }

    class MyMyList<T> : GenericListWrapper<T>
    {
    }

    class MyListString : GenericListWrapper<string>
    {
    }

    public class GenericListWrapper<T> : List<T> { }

    public class GenericListWrapperPrivateConstructor<T> : GenericListWrapper<T>
    {
        private GenericListWrapperPrivateConstructor() { }
    }

    public class GenericListWrapperInternalConstructor<T> : GenericListWrapper<T>
    {
        internal GenericListWrapperInternalConstructor() { }
    }

    public class StringStackWrapper : Stack<string>
    {
        public StringStackWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public StringStackWrapper(IList<string> items)
        {
            foreach (string item in items)
            {
                Push(item);
            }
        }
    }

    public class GenericStackWrapper<T> : Stack<T>
    {
        public GenericStackWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public GenericStackWrapper(IList<T> items)
        {
            foreach (T item in items)
            {
                Push(item);
            }
        }
    }

    public class GenericStackWrapperPrivateConstructor<T> : GenericStackWrapper<T>
    {
        private GenericStackWrapperPrivateConstructor() { }
    }

    public class GenericStackWrapperInternalConstructor<T> : GenericStackWrapper<T>
    {
        internal GenericStackWrapperInternalConstructor() { }
    }

    public class StringQueueWrapper : Queue<string>
    {
        public StringQueueWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public StringQueueWrapper(IList<string> items)
        {
            foreach (string item in items)
            {
                Enqueue(item);
            }
        }
    }

    public class GenericQueueWrapper<T> : Queue<T>
    {
        public GenericQueueWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public GenericQueueWrapper(IList<T> items)
        {
            foreach (T item in items)
            {
                Enqueue(item);
            }
        }
    }

    public class GenericQueueWrapperPrivateConstructor<T> : GenericQueueWrapper<T>
    {
        private GenericQueueWrapperPrivateConstructor() { }
    }

    public class GenericQueueWrapperInternalConstructor<T> : GenericQueueWrapper<T>
    {
        internal GenericQueueWrapperInternalConstructor() { }
    }

    public class StringHashSetWrapper : HashSet<string>
    {
        public StringHashSetWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public StringHashSetWrapper(IList<string> items)
        {
            foreach (string item in items)
            {
                Add(item);
            }
        }
    }

    public class GenericHashSetWrapper<T> : HashSet<T>
    {
        public GenericHashSetWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public GenericHashSetWrapper(IList<T> items)
        {
            foreach (T item in items)
            {
                Add(item);
            }
        }
    }

    public class StringLinkedListWrapper : LinkedList<string>
    {
        public StringLinkedListWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public StringLinkedListWrapper(IList<string> items)
        {
            foreach (string item in items)
            {
                AddLast(item);
            }
        }
    }

    public class GenericLinkedListWrapper<T> : LinkedList<T>
    {
        public GenericLinkedListWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public GenericLinkedListWrapper(IList<T> items)
        {
            foreach (T item in items)
            {
                AddLast(item);
            }
        }
    }

    public class StringSortedSetWrapper : SortedSet<string>
    {
        public StringSortedSetWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public StringSortedSetWrapper(IList<string> items)
        {
            foreach (string item in items)
            {
                Add(item);
            }
        }
    }

    public class GenericSortedSetWrapper<T> : SortedSet<T>
    {
        public GenericSortedSetWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public GenericSortedSetWrapper(IList<T> items)
        {
            foreach (T item in items)
            {
                Add(item);
            }
        }
    }

    public class StringToStringDictionaryWrapper : Dictionary<string, string>
    {
        public StringToStringDictionaryWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public StringToStringDictionaryWrapper(IList<KeyValuePair<string, string>> items)
        {
            foreach (KeyValuePair<string, string> item in items)
            {
                Add(item.Key, item.Value);
            }
        }
    }

    public class StringToGenericDictionaryWrapper<T> : Dictionary<string, T>
    {
        public StringToGenericDictionaryWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public StringToGenericDictionaryWrapper(IList<KeyValuePair<string, T>> items)
        {
            foreach (KeyValuePair<string, T> item in items)
            {
                Add(item.Key, item.Value);
            }
        }
    }

    public class StringToGenericDictionaryWrapperPrivateConstructor<T> : StringToGenericDictionaryWrapper<T>
    {
        private StringToGenericDictionaryWrapperPrivateConstructor() { }
    }

    public class StringToGenericDictionaryWrapperInternalConstructor<T> : StringToGenericDictionaryWrapper<T>
    {
        internal StringToGenericDictionaryWrapperInternalConstructor() { }
    }

    public class StringToStringSortedDictionaryWrapper : SortedDictionary<string, string>
    {
        public StringToStringSortedDictionaryWrapper() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public StringToStringSortedDictionaryWrapper(IList<KeyValuePair<string, string>> items)
        {
            foreach (KeyValuePair<string, string> item in items)
            {
                Add(item.Key, item.Value);
            }
        }
    }

    public class HashSetWithBackingCollection : ICollection<string>
    {
        private readonly ICollection<string> _inner;

        public HashSetWithBackingCollection()
        {
            _inner = new HashSet<string>();
        }

        public HashSetWithBackingCollection(IEnumerable<string> values)
        {
            _inner = new HashSet<string>(values);
        }

        public int Count => _inner.Count;

        public bool IsReadOnly => _inner.IsReadOnly;

        public void Add(string item)
        {
            _inner.Add(item);
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public bool Contains(string item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        public bool Remove(string item)
        {
            return _inner.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }
    }

    public interface IDerivedICollectionOfT<T> : ICollection<T> { }

    public interface IDerivedIDictionaryOfTKeyTValue<TKey, TValue> : IDictionary<TKey, TValue> { }

    public interface IDerivedISetOfT<T> : ISet<T> { }

    public struct GenericStructIListWrapper<T> : IList<T>
    {
        private List<T> _list;
        public T this[int index]
        {
            get
            {
                InitializeIfNull();
                return _list[index];
            }
            set
            {
                InitializeIfNull();
                _list[index] = value;
            }
        }

        public int Count => _list == null ? 0 : _list.Count;

        public bool IsReadOnly => false;

        private void InitializeIfNull()
        {
            if (_list == null)
            {
                _list = new List<T>();
            }
        }

        public void Add(T item)
        {
            InitializeIfNull();
            _list.Add(item);
        }

        public void Clear()
        {
            InitializeIfNull();
            _list.Clear();
        }

        public bool Contains(T item)
        {
            InitializeIfNull();
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            InitializeIfNull();
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            InitializeIfNull();
            return _list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            InitializeIfNull();
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            InitializeIfNull();
            _list.Insert(index, item);
        }

        public bool Remove(T item)
        {
            InitializeIfNull();
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            InitializeIfNull();
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            InitializeIfNull();
            return GetEnumerator();
        }
    }

    public struct GenericStructICollectionWrapper<T> : ICollection<T>
    {
        private List<T> _list;

        private void InitializeIfNull()
        {
            if (_list == null)
            {
                _list = new List<T>();
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            InitializeIfNull();
            _list.Add(item);
        }

        public void Clear()
        {
            InitializeIfNull();
            _list.Clear();
        }

        public bool Contains(T item)
        {
            InitializeIfNull();
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            InitializeIfNull();
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            InitializeIfNull();
            return ((ICollection<T>)_list).GetEnumerator();
        }

        public bool Remove(T item)
        {
            InitializeIfNull();
            return _list.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            InitializeIfNull();
            return ((ICollection<T>)_list).GetEnumerator();
        }
    }

    public struct GenericStructISetWrapper<T> : ISet<T>
    {
        private HashSet<T> _hashset;

        private void InitializeIfNull()
        {
            if (_hashset == null)
            {
                _hashset = new HashSet<T>();
            }
        }

        public int Count => _hashset == null ? 0 : _hashset.Count;

        public bool IsReadOnly => false;

        public bool Add(T item)
        {
            InitializeIfNull();
            return _hashset.Add(item);
        }

        public void Clear()
        {
            InitializeIfNull();
            _hashset.Clear();
        }

        public bool Contains(T item)
        {
            InitializeIfNull();
            return _hashset.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            InitializeIfNull();
            _hashset.CopyTo(array, arrayIndex);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            InitializeIfNull();
            _hashset.ExceptWith(other);
        }

        public IEnumerator<T> GetEnumerator()
        {
            InitializeIfNull();
            return ((ISet<T>)_hashset).GetEnumerator();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            InitializeIfNull();
            _hashset.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            InitializeIfNull();
            return _hashset.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            InitializeIfNull();
            return _hashset.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            InitializeIfNull();
            return _hashset.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            InitializeIfNull();
            return _hashset.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            InitializeIfNull();
            return _hashset.Overlaps(other);
        }

        public bool Remove(T item)
        {
            InitializeIfNull();
            return _hashset.Remove(item);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            InitializeIfNull();
            return _hashset.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            InitializeIfNull();
            _hashset.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            InitializeIfNull();
            _hashset.UnionWith(other);
        }

        void ICollection<T>.Add(T item)
        {
            InitializeIfNull();
            _hashset.Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            InitializeIfNull();
            return ((ISet<T>)_hashset).GetEnumerator();
        }
    }

    public struct GenericStructIDictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _dict;

        private void InitializeIfNull()
        {
            if (_dict == null)
            {
                _dict = new Dictionary<TKey, TValue>();
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                InitializeIfNull();
                return _dict[key];
            }
            set
            {
                InitializeIfNull();
                _dict[key] = value;
            }
        }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)_dict).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)_dict).Values;

        public int Count => _dict == null ? 0 : _dict.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            InitializeIfNull();
            ((IDictionary<TKey, TValue>)_dict).Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            InitializeIfNull();
            ((IDictionary<TKey, TValue>)_dict).Add(item);
        }

        public void Clear()
        {
            InitializeIfNull();
            ((IDictionary<TKey, TValue>)_dict).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            InitializeIfNull();
            return ((IDictionary<TKey, TValue>)_dict).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            InitializeIfNull();
            return ((IDictionary<TKey, TValue>)_dict).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            InitializeIfNull();
            ((IDictionary<TKey, TValue>)_dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            InitializeIfNull();
            return ((IDictionary<TKey, TValue>)_dict).GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            InitializeIfNull();
            return ((IDictionary<TKey, TValue>)_dict).Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            InitializeIfNull();
            return ((IDictionary<TKey, TValue>)_dict).Remove(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            InitializeIfNull();
            return ((IDictionary<TKey, TValue>)_dict).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            InitializeIfNull();
            return ((IDictionary<TKey, TValue>)_dict).GetEnumerator();
        }
    }

    public class ClassWithGenericStructIListWrapper
    {
        public GenericStructIListWrapper<int> List { get; set; }
    }

    public class ClassWithGenericStructICollectionWrapper
    {
        public GenericStructICollectionWrapper<int> Collection { get; set; }
    }

    public class ClassWithGenericStructIDictionaryWrapper
    {
        public GenericStructIDictionaryWrapper<string, string> Dictionary { get; set; }
    }

    public class ClassWithGenericStructISetWrapper
    {
        public GenericStructISetWrapper<int> Set { get; set; }
    }

    public class SimpleTestClassWithGenericStructCollectionWrappers : ITestClass
    {
        public GenericStructIListWrapper<int> List { get; set; }
        public GenericStructICollectionWrapper<int> Collection { get; set; }
        public GenericStructISetWrapper<int> Set { get; set; }
        public GenericStructIDictionaryWrapper<string, string> Dictionary { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""List"" : [10]," +
            @"""Collection"" : [30]," +
            @"""Set"" : [50]," +
            @"""Dictionary"" : {""key1"" : ""value1""}" +
            @"}";

        public void Initialize()
        {
            List = new GenericStructIListWrapper<int>() { 10 };
            Collection = new GenericStructICollectionWrapper<int>() { 30 };
            Set = new GenericStructISetWrapper<int>() { 50 };
            Dictionary = new GenericStructIDictionaryWrapper<string, string>() { { "key1", "value1" } };
        }

        public void Verify()
        {
            Assert.Equal(1, List.Count);
            Assert.Equal(10, List[0]);
            Assert.Equal(1, Collection.Count);
            Assert.Equal(30, Collection.ElementAt(0));
            Assert.Equal(1, Set.Count);
            Assert.Equal(50, Set.ElementAt(0));
            Assert.Equal(1, Dictionary.Keys.Count);
            Assert.Equal("value1", Dictionary["key1"]);
        }
    }

    public struct SimpleTestStructWithNullableGenericStructCollectionWrappers : ITestClass
    {
        public GenericStructIListWrapper<int>? List { get; set; }
        public GenericStructICollectionWrapper<int>? Collection { get; set; }
        public GenericStructISetWrapper<int>? Set { get; set; }
        public GenericStructIDictionaryWrapper<string, string>? Dictionary { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""List"" : [10]," +
            @"""Collection"" : [30]," +
            @"""Set"" : [50]," +
            @"""Dictionary"" : {""key1"" : ""value1""}" +
            @"}";

        public void Initialize()
        {
            List = new GenericStructIListWrapper<int>() { 10 };
            Collection = new GenericStructICollectionWrapper<int>() { 30 };
            Set = new GenericStructISetWrapper<int>() { 50 };
            Dictionary = new GenericStructIDictionaryWrapper<string, string>() { { "key1", "value1" } };
        }

        public void Verify()
        {
            Assert.Equal(1, List.Value.Count);
            Assert.Equal(10, List.Value[0]);
            Assert.Equal(1, Collection.Value.Count);
            Assert.Equal(30, Collection.Value.ElementAt(0));
            Assert.Equal(1, Set.Value.Count);
            Assert.Equal(50, Set.Value.ElementAt(0));
            Assert.Equal(1, Dictionary.Value.Keys.Count);
            Assert.Equal("value1", Dictionary.Value["key1"]);
        }
    }

    public class DisposableEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        private Action _onDispose;

        public DisposableEnumerator(IEnumerator<T> inner, Action onDispose)
        {
            _inner = inner;
            _onDispose = onDispose;
        }

        public T Current => _inner.Current;

        object IEnumerator.Current => ((IEnumerator)_inner).Current;

        public bool MoveNext() => _inner.MoveNext();

        public void Dispose()
        {
            _onDispose?.Invoke();
            _onDispose = null;
        }

        public void Reset() => _inner.Reset();
    }
}
