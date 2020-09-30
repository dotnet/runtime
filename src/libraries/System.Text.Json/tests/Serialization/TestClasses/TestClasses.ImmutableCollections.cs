// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class SimpleTestClassWithImmutableArray : ITestClass
    {
        public ImmutableArray<string> MyStringImmutableArray { get; set; }

        public static readonly string s_json = @"{""MyStringImmutableArray"":[""Hello""]}";

        public void Initialize()
        {
            MyStringImmutableArray = ImmutableArray.CreateRange(new List<string> { "Hello" });
        }

        public void Verify()
        {
            Assert.Equal("Hello", MyStringImmutableArray[0]);
        }
    }

    public class SimpleTestClassWithObjectImmutableArray : ITestClass
    {
        public object MyStringImmutableArray { get; set; }

        public static readonly string s_json = @"{""MyStringImmutableArray"":[""Hello""]}";

        public void Initialize()
        {
            MyStringImmutableArray = ImmutableArray.CreateRange(new List<string> { "Hello" });
        }

        public void Verify()
        {
            Assert.Equal("Hello", ((ImmutableArray<string>)MyStringImmutableArray)[0]);
        }
    }

    public class SimpleTestClassWithIImmutableDictionaryWrapper
    {
        public StringToStringIImmutableDictionaryWrapper MyStringToStringImmutableDictionaryWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringToStringImmutableDictionaryWrapper"" : {""key"" : ""value""}" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringToStringImmutableDictionaryWrapper = new StringToStringIImmutableDictionaryWrapper(new Dictionary<string, string> { { "key", "value" } });
        }
    }

    public class SimpleTestClassWithImmutableListWrapper
    {
        public StringIImmutableListWrapper MyStringIImmutableListWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringIImmutableListWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringIImmutableListWrapper = new StringIImmutableListWrapper(new List<string> { "Hello" });
        }
    }

    public class SimpleTestClassWithImmutableStackWrapper
    {
        public StringIImmutableStackWrapper MyStringIImmutableStackWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringIImmutableStackWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringIImmutableStackWrapper = new StringIImmutableStackWrapper(new List<string> { "Hello" });
        }
    }

    public class SimpleTestClassWithImmutableQueueWrapper
    {
        public StringIImmutableQueueWrapper MyStringIImmutableQueueWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringIImmutableQueueWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringIImmutableQueueWrapper = new StringIImmutableQueueWrapper(new List<string> { "Hello" });
        }
    }

    public class SimpleTestClassWithImmutableSetWrapper
    {
        public StringIImmutableSetWrapper MyStringIImmutableSetWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStringIImmutableSetWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStringIImmutableSetWrapper = new StringIImmutableSetWrapper(new List<string> { "Hello" });
        }
    }

    public class StringToStringIImmutableDictionaryWrapper : IImmutableDictionary<string, string>
    {
        private ImmutableDictionary<string, string> _dictionary;

        public StringToStringIImmutableDictionaryWrapper() { }

        public StringToStringIImmutableDictionaryWrapper(Dictionary<string, string> items)
        {
            _dictionary = ImmutableDictionary.CreateRange(items);
        }

        public string this[string key] => _dictionary[key];

        public IEnumerable<string> Keys => _dictionary.Keys;

        public IEnumerable<string> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public IImmutableDictionary<string, string> Add(string key, string value)
        {
            return ((IImmutableDictionary<string, string>)_dictionary).Add(key, value);
        }

        public IImmutableDictionary<string, string> AddRange(IEnumerable<KeyValuePair<string, string>> pairs)
        {
            return ((IImmutableDictionary<string, string>)_dictionary).AddRange(pairs);
        }

        public IImmutableDictionary<string, string> Clear()
        {
            return ((IImmutableDictionary<string, string>)_dictionary).Clear();
        }

        public bool Contains(KeyValuePair<string, string> pair)
        {
            return _dictionary.Contains(pair);
        }

        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return ((IImmutableDictionary<string, string>)_dictionary).GetEnumerator();
        }

        public IImmutableDictionary<string, string> Remove(string key)
        {
            return ((IImmutableDictionary<string, string>)_dictionary).Remove(key);
        }

        public IImmutableDictionary<string, string> RemoveRange(IEnumerable<string> keys)
        {
            return ((IImmutableDictionary<string, string>)_dictionary).RemoveRange(keys);
        }

        public IImmutableDictionary<string, string> SetItem(string key, string value)
        {
            return ((IImmutableDictionary<string, string>)_dictionary).SetItem(key, value);
        }

        public IImmutableDictionary<string, string> SetItems(IEnumerable<KeyValuePair<string, string>> items)
        {
            return ((IImmutableDictionary<string, string>)_dictionary).SetItems(items);
        }

        public bool TryGetKey(string equalKey, out string actualKey)
        {
            return _dictionary.TryGetKey(equalKey, out actualKey);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IImmutableDictionary<string, string>)_dictionary).GetEnumerator();
        }
    }

    public class GenericIImmutableDictionaryWrapper<TKey, TValue> : IImmutableDictionary<TKey, TValue>
    {
        private ImmutableDictionary<TKey, TValue> _dictionary;

        public GenericIImmutableDictionaryWrapper() { }

        public GenericIImmutableDictionaryWrapper(Dictionary<TKey, TValue> items)
        {
            _dictionary = ImmutableDictionary.CreateRange(items);
        }

        public TValue this[TKey key] => _dictionary[key];

        public IEnumerable<TKey> Keys => _dictionary.Keys;

        public IEnumerable<TValue> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public IImmutableDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            return ((IImmutableDictionary<TKey, TValue>)_dictionary).Add(key, value);
        }

        public IImmutableDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            return ((IImmutableDictionary<TKey, TValue>)_dictionary).AddRange(pairs);
        }

        public IImmutableDictionary<TKey, TValue> Clear()
        {
            return ((IImmutableDictionary<TKey, TValue>)_dictionary).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> pair)
        {
            return _dictionary.Contains(pair);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IImmutableDictionary<TKey, TValue>)_dictionary).GetEnumerator();
        }

        public IImmutableDictionary<TKey, TValue> Remove(TKey key)
        {
            return ((IImmutableDictionary<TKey, TValue>)_dictionary).Remove(key);
        }

        public IImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            return ((IImmutableDictionary<TKey, TValue>)_dictionary).RemoveRange(keys);
        }

        public IImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value)
        {
            return ((IImmutableDictionary<TKey, TValue>)_dictionary).SetItem(key, value);
        }

        public IImmutableDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            return ((IImmutableDictionary<TKey, TValue>)_dictionary).SetItems(items);
        }

        public bool TryGetKey(TKey equalKey, out TKey actualKey)
        {
            return _dictionary.TryGetKey(equalKey, out actualKey);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IImmutableDictionary<TKey, TValue>)_dictionary).GetEnumerator();
        }
    }

    public class StringIImmutableListWrapper : IImmutableList<string>
    {
        private ImmutableList<string> _list = ImmutableList.Create<string>();

        public StringIImmutableListWrapper() { }

        public StringIImmutableListWrapper(List<string> items)
        {
            _list = ImmutableList.CreateRange(items);
        }

        public string this[int index] => _list[index];

        public int Count => _list.Count;

        public IImmutableList<string> Add(string value)
        {
            return ((IImmutableList<string>)_list).Add(value);
        }

        public IImmutableList<string> AddRange(IEnumerable<string> items)
        {
            return ((IImmutableList<string>)_list).AddRange(items);
        }

        public IImmutableList<string> Clear()
        {
            return ((IImmutableList<string>)_list).Clear();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IImmutableList<string>)_list).GetEnumerator();
        }

        public int IndexOf(string item, int index, int count, IEqualityComparer<string> equalityComparer)
        {
            return _list.IndexOf(item, index, count, equalityComparer);
        }

        public IImmutableList<string> Insert(int index, string element)
        {
            return ((IImmutableList<string>)_list).Insert(index, element);
        }

        public IImmutableList<string> InsertRange(int index, IEnumerable<string> items)
        {
            return ((IImmutableList<string>)_list).InsertRange(index, items);
        }

        public int LastIndexOf(string item, int index, int count, IEqualityComparer<string> equalityComparer)
        {
            return _list.LastIndexOf(item, index, count, equalityComparer);
        }

        public IImmutableList<string> Remove(string value, IEqualityComparer<string> equalityComparer)
        {
            return ((IImmutableList<string>)_list).Remove(value, equalityComparer);
        }

        public IImmutableList<string> RemoveAll(Predicate<string> match)
        {
            return ((IImmutableList<string>)_list).RemoveAll(match);
        }

        public IImmutableList<string> RemoveAt(int index)
        {
            return ((IImmutableList<string>)_list).RemoveAt(index);
        }

        public IImmutableList<string> RemoveRange(IEnumerable<string> items, IEqualityComparer<string> equalityComparer)
        {
            return ((IImmutableList<string>)_list).RemoveRange(items, equalityComparer);
        }

        public IImmutableList<string> RemoveRange(int index, int count)
        {
            return ((IImmutableList<string>)_list).RemoveRange(index, count);
        }

        public IImmutableList<string> Replace(string oldValue, string newValue, IEqualityComparer<string> equalityComparer)
        {
            return ((IImmutableList<string>)_list).Replace(oldValue, newValue, equalityComparer);
        }

        public IImmutableList<string> SetItem(int index, string value)
        {
            return ((IImmutableList<string>)_list).SetItem(index, value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IImmutableList<string>)_list).GetEnumerator();
        }
    }

    public class StringIImmutableStackWrapper : IImmutableStack<string>
    {
        private ImmutableStack<string> _stack = ImmutableStack.Create<string>();

        public StringIImmutableStackWrapper() { }

        public StringIImmutableStackWrapper(List<string> items)
        {
            _stack = ImmutableStack.CreateRange(items);
        }

        public bool IsEmpty => _stack.IsEmpty;

        public IImmutableStack<string> Clear()
        {
            return ((IImmutableStack<string>)_stack).Clear();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IImmutableStack<string>)_stack).GetEnumerator();
        }

        public string Peek()
        {
            return _stack.Peek();
        }

        public IImmutableStack<string> Pop()
        {
            return ((IImmutableStack<string>)_stack).Pop();
        }

        public IImmutableStack<string> Push(string value)
        {
            return ((IImmutableStack<string>)_stack).Push(value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IImmutableStack<string>)_stack).GetEnumerator();
        }
    }

    public class StringIImmutableQueueWrapper : IImmutableQueue<string>
    {
        private ImmutableQueue<string> _queue = ImmutableQueue.Create<string>();

        public StringIImmutableQueueWrapper() { }

        public StringIImmutableQueueWrapper(List<string> items)
        {
            _queue = ImmutableQueue.CreateRange(items);
        }

        public bool IsEmpty => _queue.IsEmpty;

        public IImmutableQueue<string> Clear()
        {
            return ((IImmutableQueue<string>)_queue).Clear();
        }

        public IImmutableQueue<string> Dequeue()
        {
            return ((IImmutableQueue<string>)_queue).Dequeue();
        }

        public IImmutableQueue<string> Enqueue(string value)
        {
            return ((IImmutableQueue<string>)_queue).Enqueue(value);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IImmutableQueue<string>)_queue).GetEnumerator();
        }

        public string Peek()
        {
            return _queue.Peek();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IImmutableQueue<string>)_queue).GetEnumerator();
        }
    }

    public class StringIImmutableSetWrapper : IImmutableSet<string>
    {
        private ImmutableHashSet<string> _set = ImmutableHashSet.Create<string>();

        public StringIImmutableSetWrapper() { }

        public StringIImmutableSetWrapper(List<string> items)
        {
            _set = ImmutableHashSet.CreateRange(items);
        }

        public int Count => _set.Count;

        public IImmutableSet<string> Add(string value)
        {
            return ((IImmutableSet<string>)_set).Add(value);
        }

        public IImmutableSet<string> Clear()
        {
            return ((IImmutableSet<string>)_set).Clear();
        }

        public bool Contains(string value)
        {
            return _set.Contains(value);
        }

        public IImmutableSet<string> Except(IEnumerable<string> other)
        {
            return ((IImmutableSet<string>)_set).Except(other);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IImmutableSet<string>)_set).GetEnumerator();
        }

        public IImmutableSet<string> Intersect(IEnumerable<string> other)
        {
            return ((IImmutableSet<string>)_set).Intersect(other);
        }

        public bool IsProperSubsetOf(IEnumerable<string> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<string> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<string> other)
        {
            return _set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<string> other)
        {
            return _set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<string> other)
        {
            return _set.Overlaps(other);
        }

        public IImmutableSet<string> Remove(string value)
        {
            return ((IImmutableSet<string>)_set).Remove(value);
        }

        public bool SetEquals(IEnumerable<string> other)
        {
            return _set.SetEquals(other);
        }

        public IImmutableSet<string> SymmetricExcept(IEnumerable<string> other)
        {
            return ((IImmutableSet<string>)_set).SymmetricExcept(other);
        }

        public bool TryGetValue(string equalValue, out string actualValue)
        {
            return _set.TryGetValue(equalValue, out actualValue);
        }

        public IImmutableSet<string> Union(IEnumerable<string> other)
        {
            return ((IImmutableSet<string>)_set).Union(other);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IImmutableSet<string>)_set).GetEnumerator();
        }
    }
}
