// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class SimpleTestClassWithGenericCollectionWrappers : ITestClass
    {
        public StringICollectionWrapper MyStringICollectionWrapper { get; set; }
        public StringIListWrapper MyStringIListWrapper { get; set; }
        public StringISetWrapper MyStringISetWrapper { get; set; }
        public StringToStringIDictionaryWrapper MyStringToStringIDictionaryWrapper { get; set; }
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
            MyStringICollectionWrapper = new StringICollectionWrapper() { "Hello" };
            MyStringIListWrapper = new StringIListWrapper() { "Hello" };
            MyStringISetWrapper = new StringISetWrapper() { "Hello" };
            MyStringToStringIDictionaryWrapper = new StringToStringIDictionaryWrapper() { { "key", "value" } };
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
        public StringIReadOnlyCollectionWrapper MyStringIReadOnlyCollectionWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringIReadOnlyCollectionWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringIReadOnlyCollectionWrapper = new StringIReadOnlyCollectionWrapper(new List<string> { "Hello" });
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
        public StringToStringIReadOnlyDictionaryWrapper MyStringToStringIReadOnlyDictionaryWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringToStringIReadOnlyDictionaryWrapper"" : {""key"" : ""value""}" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringToStringIReadOnlyDictionaryWrapper = new StringToStringIReadOnlyDictionaryWrapper(
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

    public class StringICollectionWrapper : ICollection<string>
    {
        private readonly List<string> _list = new List<string>();

        public int Count => _list.Count;

        public virtual bool IsReadOnly => ((ICollection<string>)_list).IsReadOnly;

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
            return ((ICollection<string>)_list).GetEnumerator();
        }

        public bool Remove(string item)
        {
            return _list.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<string>)_list).GetEnumerator();
        }
    }

    public class ReadOnlyStringICollectionWrapper : StringICollectionWrapper
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

    public class GenericICollectionWrapper<T> : ICollection<T>
    {
        private readonly List<T> _list = new List<T>();

        public int Count => _list.Count;

        public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

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

    public class StringIReadOnlyCollectionWrapper : IReadOnlyCollection<string>
    {
        private readonly List<string> _list = new List<string>();

        public StringIReadOnlyCollectionWrapper() { }

        public StringIReadOnlyCollectionWrapper(List<string> list)
        {
            _list = list;
        }

        public int Count => _list.Count;

        public IEnumerator<string> GetEnumerator()
        {
            return ((IReadOnlyCollection<string>)_list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyCollection<string>)_list).GetEnumerator();
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

    public class StringISetWrapper : ISet<string>
    {
        private readonly HashSet<string> _hashset = new HashSet<string>();

        public int Count => _hashset.Count;

        public bool IsReadOnly => ((ISet<string>)_hashset).IsReadOnly;

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

    public class StringToStringIDictionaryWrapper : IDictionary<string, string>
    {
        private Dictionary<string, string> _dictionary = new Dictionary<string, string>();

        public StringToStringIDictionaryWrapper() { }

        public StringToStringIDictionaryWrapper(Dictionary<string, string> dictionary)
        {
            _dictionary = dictionary;
        }

        public string this[string key] { get => ((IDictionary<string, string>)_dictionary)[key]; set => ((IDictionary<string, string>)_dictionary)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, string>)_dictionary).Keys;

        public ICollection<string> Values => ((IDictionary<string, string>)_dictionary).Values;

        public int Count => ((IDictionary<string, string>)_dictionary).Count;

        public virtual bool IsReadOnly => ((IDictionary<string, string>)_dictionary).IsReadOnly;

        public virtual void Add(string key, string value)
        {
            ((IDictionary<string, string>)_dictionary).Add(key, value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            ((IDictionary<string, string>)_dictionary).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<string, string>)_dictionary).Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return ((IDictionary<string, string>)_dictionary).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, string>)_dictionary).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            ((IDictionary<string, string>)_dictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return ((IDictionary<string, string>)_dictionary).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, string>)_dictionary).Remove(key);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return ((IDictionary<string, string>)_dictionary).Remove(item);
        }

        public bool TryGetValue(string key, out string value)
        {
            return ((IDictionary<string, string>)_dictionary).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<string, string>)_dictionary).GetEnumerator();
        }
    }

    public class ReadOnlyStringToStringIDictionaryWrapper : StringToStringIDictionaryWrapper
    {
        public override bool IsReadOnly => true;
    }

    public class StringToObjectIDictionaryWrapper : IDictionary<string, object>
    {
        private Dictionary<string, object> _dictionary = new Dictionary<string, object>();

        public StringToObjectIDictionaryWrapper() { }

        public StringToObjectIDictionaryWrapper(Dictionary<string, object> dictionary)
        {
            _dictionary = dictionary;
        }

        public object this[string key] { get => ((IDictionary<string, object>)_dictionary)[key]; set => ((IDictionary<string, object>)_dictionary)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, object>)_dictionary).Keys;

        public ICollection<object> Values => ((IDictionary<string, object>)_dictionary).Values;

        public int Count => ((IDictionary<string, object>)_dictionary).Count;

        public bool IsReadOnly => ((IDictionary<string, object>)_dictionary).IsReadOnly;

        public void Add(string key, object value)
        {
            ((IDictionary<string, object>)_dictionary).Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            ((IDictionary<string, object>)_dictionary).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<string, object>)_dictionary).Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return ((IDictionary<string, object>)_dictionary).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, object>)_dictionary).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((IDictionary<string, object>)_dictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ((IDictionary<string, object>)_dictionary).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, object>)_dictionary).Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return ((IDictionary<string, object>)_dictionary).Remove(item);
        }

        public bool TryGetValue(string key, out object value)
        {
            return ((IDictionary<string, object>)_dictionary).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<string, object>)_dictionary).GetEnumerator();
        }
    }

    public class StringToGenericIDictionaryWrapper<TValue> : IDictionary<string, TValue>
    {
        private Dictionary<string, TValue> _dictionary = new Dictionary<string, TValue>();

        public TValue this[string key] { get => ((IDictionary<string, TValue>)_dictionary)[key]; set => ((IDictionary<string, TValue>)_dictionary)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, TValue>)_dictionary).Keys;

        public ICollection<TValue> Values => ((IDictionary<string, TValue>)_dictionary).Values;

        public int Count => ((IDictionary<string, TValue>)_dictionary).Count;

        public bool IsReadOnly => ((IDictionary<string, TValue>)_dictionary).IsReadOnly;

        public void Add(string key, TValue value)
        {
            ((IDictionary<string, TValue>)_dictionary).Add(key, value);
        }

        public void Add(KeyValuePair<string, TValue> item)
        {
            ((IDictionary<string, TValue>)_dictionary).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<string, TValue>)_dictionary).Clear();
        }

        public bool Contains(KeyValuePair<string, TValue> item)
        {
            return ((IDictionary<string, TValue>)_dictionary).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, TValue>)_dictionary).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<string, TValue>)_dictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            return ((IDictionary<string, TValue>)_dictionary).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, TValue>)_dictionary).Remove(key);
        }

        public bool Remove(KeyValuePair<string, TValue> item)
        {
            return ((IDictionary<string, TValue>)_dictionary).Remove(item);
        }

        public bool TryGetValue(string key, out TValue value)
        {
            return ((IDictionary<string, TValue>)_dictionary).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<string, TValue>)_dictionary).GetEnumerator();
        }
    }

    public class StringToStringIReadOnlyDictionaryWrapper : IReadOnlyDictionary<string, string>
    {
        private Dictionary<string, string> _dictionary = new Dictionary<string, string>();

        public StringToStringIReadOnlyDictionaryWrapper() { }

        public StringToStringIReadOnlyDictionaryWrapper(Dictionary<string, string> items)
        {
            _dictionary = items;
        }

        public string this[string key] => ((IReadOnlyDictionary<string, string>)_dictionary)[key];

        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, string>)_dictionary).Keys;

        public IEnumerable<string> Values => ((IReadOnlyDictionary<string, string>)_dictionary).Values;

        public int Count => ((IReadOnlyDictionary<string, string>)_dictionary).Count;

        public bool ContainsKey(string key)
        {
            return ((IReadOnlyDictionary<string, string>)_dictionary).ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return ((IReadOnlyDictionary<string, string>)_dictionary).GetEnumerator();
        }

        public bool TryGetValue(string key, out string value)
        {
            return ((IReadOnlyDictionary<string, string>)_dictionary).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyDictionary<string, string>)_dictionary).GetEnumerator();
        }
    }

    public class StringListWrapper : List<string> { }

    class MyMyList<T> : GenericListWrapper<T>
    {
    }

    class MyListString : GenericListWrapper<string>
    {
    }

    public class GenericListWrapper<T> : List<T> { }

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

    public class StringToGenericSortedDictionary<T> : SortedDictionary<string, T>
    {
        public StringToGenericSortedDictionary() { }

        // For populating test data only. We cannot assume actual input will have this method.
        public StringToGenericSortedDictionary(IList<KeyValuePair<string, T>> items)
        {
            foreach (KeyValuePair<string, T> item in items)
            {
                Add(item.Key, item.Value);
            }
        }
    }
}
