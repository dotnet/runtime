// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class SimpleTestClassWithNonGenericCollectionWrappers : ITestClass
    {
        public WrapperForIList MyIListWrapper { get; set; }
        public WrapperForIDictionary MyIDictionaryWrapper { get; set; }
        public HashtableWrapper MyHashtableWrapper { get; set; }
        public ArrayListWrapper MyArrayListWrapper { get; set; }
        public SortedListWrapper MySortedListWrapper { get; set; }
        public StackWrapper MyStackWrapper { get; set; }
        public QueueWrapper MyQueueWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyIListWrapper"" : [""Hello""]," +
            @"""MyIDictionaryWrapper"" : {""key"" : ""value""}," +
            @"""MyHashtableWrapper"" : {""key"" : ""value""}," +
            @"""MyArrayListWrapper"" : [""Hello""]," +
            @"""MySortedListWrapper"" : {""key"" : ""value""}," +
            @"""MyStackWrapper"" : [""Hello""]," +
            @"""MyQueueWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize()
        {
            MyIListWrapper = new WrapperForIList() { "Hello" };
            MyIDictionaryWrapper = new WrapperForIDictionary() { { "key", "value" } };
            MyHashtableWrapper = new HashtableWrapper(new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("key", "value" ) });
            MyArrayListWrapper = new ArrayListWrapper() { "Hello" };
            MySortedListWrapper = new SortedListWrapper() { { "key", "value" } };
            MyStackWrapper = new StackWrapper();
            MyQueueWrapper = new QueueWrapper();

            MyStackWrapper.Push("Hello");
            MyQueueWrapper.Enqueue("Hello");
        }

        public void Verify()
        {
            Assert.Equal("Hello", ((JsonElement)MyIListWrapper[0]).GetString());
            Assert.Equal("value", ((JsonElement)MyIDictionaryWrapper["key"]).GetString());
            Assert.Equal("value", ((JsonElement)MyHashtableWrapper["key"]).GetString());
            Assert.Equal("Hello", ((JsonElement)MyArrayListWrapper[0]).GetString());
            Assert.Equal("value", ((JsonElement)MySortedListWrapper["key"]).GetString());
            Assert.Equal("Hello", ((JsonElement)MyStackWrapper.Peek()).GetString());
            Assert.Equal("Hello", ((JsonElement)MyQueueWrapper.Peek()).GetString());
        }
    }

    public class SimpleTestClassWithIEnumerableWrapper
    {
        public WrapperForIEnumerable MyIEnumerableWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyIEnumerableWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyIEnumerableWrapper = new WrapperForIEnumerable(new List<object> { "Hello" });
        }
    }

    public class SimpleTestClassWithICollectionWrapper
    {
        public WrapperForICollection MyICollectionWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyICollectionWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyICollectionWrapper = new WrapperForICollection(new List<object> { "Hello" });
        }
    }

    public class SimpleTestClassWithStackWrapper
    {
        public StackWrapper MyStackWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyStackWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyStackWrapper = new StackWrapper(new List<object> { "Hello" });
        }
    }

    public class SimpleTestClassWithQueueWrapper
    {
        public QueueWrapper MyQueueWrapper { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""MyQueueWrapper"" : [""Hello""]" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        // Call only when testing serialization.
        public void Initialize()
        {
            MyQueueWrapper = new QueueWrapper(new List<object> { "Hello" });
        }
    }

    public class WrapperForIEnumerable : IEnumerable
    {
        private readonly List<object> _list;

        public WrapperForIEnumerable()
        {
            _list = new List<object>();
        }

        public WrapperForIEnumerable(IEnumerable<object> items)
        {
            _list = new List<object>(items);
        }

        public IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    public class WrapperForIEnumerablePrivateConstructor : WrapperForIEnumerable
    {
        private WrapperForIEnumerablePrivateConstructor() { }
    }

    public class WrapperForIEnumerableInternalConstructor : WrapperForIEnumerable
    {
        internal WrapperForIEnumerableInternalConstructor() { }
    }

    public class WrapperForICollection : ICollection
    {
        private readonly List<object> _list = new List<object>();

        public WrapperForICollection() { }

        public WrapperForICollection(List<object> items)
        {
            foreach (object item in items)
            {
                _list.Add(item);
            }
        }

        public int Count => _list.Count;

        public bool IsSynchronized => ((ICollection)_list).IsSynchronized;

        public object SyncRoot => ((ICollection)_list).SyncRoot;

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_list).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    public class WrapperForICollectionPrivateConstructor : WrapperForICollection
    {
        private WrapperForICollectionPrivateConstructor() { }
    }

    public class WrapperForICollectionInternalConstructor : WrapperForICollection
    {
        internal WrapperForICollectionInternalConstructor() { }
    }

    public class ReadOnlyWrapperForIList : WrapperForIList
    {
        public override bool IsReadOnly => true;
    }

    public class WrapperForIList : IList
    {
        private readonly List<object> _list;

        public WrapperForIList()
        {
            _list = new List<object>();
        }

        public WrapperForIList(IEnumerable<object> items)
        {
            _list = new List<object>(items);
        }

        public WrapperForIList(IEnumerable items)
        {
            _list = new List<object>();

            foreach (object item in items)
            {
                _list.Add(item);
            }
        }

        public object this[int index] { get => ((IList)_list)[index]; set => ((IList)_list)[index] = value; }

        public bool IsFixedSize => ((IList)_list).IsFixedSize;

        public virtual bool IsReadOnly => ((IList)_list).IsReadOnly;

        public int Count => _list.Count;

        public bool IsSynchronized => ((IList)_list).IsSynchronized;

        public object SyncRoot => ((IList)_list).SyncRoot;

        public int Add(object value)
        {
            return ((IList)_list).Add(value);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(object value)
        {
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return ((IList)_list).GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return _list.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            _list.Insert(index, value);
        }

        public void Remove(object value)
        {
            _list.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
    }

    public class WrapperForIListPrivateConstructor : WrapperForIList
    {
        private WrapperForIListPrivateConstructor() { }
    }
    public class WrapperForIListInternalConstructor : WrapperForIList
    {
        internal WrapperForIListInternalConstructor() { }
    }

    public class ReadOnlyWrapperForIDictionary : WrapperForIDictionary
    {
        public override bool IsReadOnly => true;
    }

    public class WrapperForIDictionary : IDictionary
    {
        private readonly Dictionary<string, object> _dictionary = new Dictionary<string, object>();

        public object this[object key] { get => ((IDictionary)_dictionary)[key]; set => ((IDictionary)_dictionary)[key] = value; }

        public bool IsFixedSize => ((IDictionary)_dictionary).IsFixedSize;

        public virtual bool IsReadOnly => ((IDictionary)_dictionary).IsReadOnly;

        public ICollection Keys => ((IDictionary)_dictionary).Keys;

        public ICollection Values => ((IDictionary)_dictionary).Values;

        public int Count => _dictionary.Count;

        public bool IsSynchronized => ((IDictionary)_dictionary).IsSynchronized;

        public object SyncRoot => ((IDictionary)_dictionary).SyncRoot;

        public void Add(object key, object value)
        {
            ((IDictionary)_dictionary).Add(key, value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(object key)
        {
            return ((IDictionary)_dictionary).Contains(key);
        }

        public void CopyTo(Array array, int index)
        {
            ((IDictionary)_dictionary).CopyTo(array, index);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return ((IDictionary)_dictionary).GetEnumerator();
        }

        public void Remove(object key)
        {
            ((IDictionary)_dictionary).Remove(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary)_dictionary).GetEnumerator();
        }
    }

    public class WrapperForIDictionaryPrivateConstructor : WrapperForIDictionary
    {
        private WrapperForIDictionaryPrivateConstructor() { }
    }

    public class WrapperForIDictionaryInternalConstructor : WrapperForIDictionary
    {
        internal WrapperForIDictionaryInternalConstructor() { }
    }

    public class StackWrapper : Stack
    {
        public StackWrapper() { }

        public StackWrapper(List<object> items)
        {
            foreach (object item in items)
            {
                Push(item);
            }
        }
    }

    public class QueueWrapper : Queue
    {
        public QueueWrapper() { }

        public QueueWrapper(List<object> items)
        {
            foreach (object item in items)
            {
                Enqueue(item);
            }
        }
    }

    public class HashtableWrapper : Hashtable
    {
        public HashtableWrapper() { }

        public HashtableWrapper(List<KeyValuePair<string, object>> items)
        {
            foreach (KeyValuePair<string, object> item in items)
            {
                Add(item.Key, item.Value);
            }
        }
    }

    public class ArrayListWrapper : ArrayList
    {
        public ArrayListWrapper() { }

        public ArrayListWrapper(List<object> items)
        {
            foreach (object item in items)
            {
                Add(item);
            }
        }
    }

    public class SortedListWrapper : SortedList
    {
        public SortedListWrapper() { }

        public SortedListWrapper(List<KeyValuePair<string, object>> items)
        {
            foreach (KeyValuePair<string, object> item in items)
            {
                Add(item.Key, item.Value);
            }
        }
    }

    public interface IDerivedIList : IList { }


    public struct StructWrapperForIList : IList
    {
        private List<object> _list;

        private void InitializeIfNull()
        {
            if (_list == null)
            {
                _list = new List<object>();
            }
        }

        public object this[int index]
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

        public bool IsFixedSize => ((IList)_list).IsFixedSize;

        public bool IsReadOnly => false;

        public int Count => _list == null ? 0 : _list.Count;

        public bool IsSynchronized => ((IList)_list).IsSynchronized;

        public object SyncRoot => ((IList)_list).SyncRoot;

        public int Add(object value)
        {
            InitializeIfNull();
            return ((IList)_list).Add(value);
        }

        public void Clear()
        {
            InitializeIfNull();
            _list.Clear();
        }

        public bool Contains(object value)
        {
            InitializeIfNull();
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            InitializeIfNull();
            ((IList)_list).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            InitializeIfNull();
            return ((IList)_list).GetEnumerator();
        }

        public int IndexOf(object value)
        {
            InitializeIfNull();
            return _list.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            InitializeIfNull();
            _list.Insert(index, value);
        }

        public void Remove(object value)
        {
            InitializeIfNull();
            _list.Remove(value);
        }

        public void RemoveAt(int index)
        {
            InitializeIfNull();
            _list.RemoveAt(index);
        }
    }

    public struct StructWrapperForIDictionary : IDictionary
    {
        private Dictionary<string, object> _dictionary;

        private void InitializeIfNull()
        {
            if (_dictionary == null)
            {
                _dictionary = new Dictionary<string, object>();
            }
        }

        public object this[object key]
        {
            get
            {
                InitializeIfNull();
                return ((IDictionary)_dictionary)[key];
            }
            set
            {
                InitializeIfNull();
                ((IDictionary)_dictionary)[key] = value;
            }
        }

        public bool IsFixedSize => ((IDictionary)_dictionary).IsFixedSize;

        public bool IsReadOnly => false;

        public ICollection Keys => ((IDictionary)_dictionary).Keys;

        public ICollection Values => ((IDictionary)_dictionary).Values;

        public int Count => _dictionary.Count;

        public bool IsSynchronized => ((IDictionary)_dictionary).IsSynchronized;

        public object SyncRoot => ((IDictionary)_dictionary).SyncRoot;

        public void Add(object key, object value)
        {
            InitializeIfNull();
            ((IDictionary)_dictionary).Add(key, value);
        }

        public void Clear()
        {
            InitializeIfNull();
            _dictionary.Clear();
        }

        public bool Contains(object key)
        {
            InitializeIfNull();
            return ((IDictionary)_dictionary).Contains(key);
        }

        public void CopyTo(Array array, int index)
        {
            InitializeIfNull();
            ((IDictionary)_dictionary).CopyTo(array, index);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            InitializeIfNull();
            return ((IDictionary)_dictionary).GetEnumerator();
        }

        public void Remove(object key)
        {
            InitializeIfNull();
            ((IDictionary)_dictionary).Remove(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            InitializeIfNull();
            return ((IDictionary)_dictionary).GetEnumerator();
        }
    }

    public class ClassWithStructIDictionaryWrapper
    {
        public StructWrapperForIDictionary Dictionary { get; set; }
    }

    public class ClassWithStructIListWrapper
    {
        public StructWrapperForIList List { get; set; }
    }

    public class SimpleTestClassWithStructCollectionWrappers : ITestClass
    {
        public StructWrapperForIList List { get; set; }
        public StructWrapperForIDictionary Dictionary { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""List"" : [""Hello""]," +
            @"""Dictionary"" : {""key1"" : ""value1""}" +
            @"}";

        public void Initialize()
        {
            List = new StructWrapperForIList() { "Hello" };
            Dictionary = new StructWrapperForIDictionary { { "key1", "value1" } };
        }

        public void Verify()
        {
            Assert.Equal(1, List.Count);
            Assert.Equal("Hello", ((JsonElement)List[0]).GetString());
            Assert.Equal(1, Dictionary.Keys.Count);
            Assert.Equal("value1", ((JsonElement)Dictionary["key1"]).GetString());
        }
    }

    public struct SimpleTestStructWithNullableStructCollectionWrappers : ITestClass
    {
        public StructWrapperForIList? List { get; set; }
        public StructWrapperForIDictionary? Dictionary { get; set; }

        public static readonly string s_json =
            @"{" +
            @"""List"" : [""Hello""]," +
            @"""Dictionary"" : {""key1"" : ""value1""}" +
            @"}";

        public void Initialize()
        {
            List = new StructWrapperForIList() { "Hello" };
            Dictionary = new StructWrapperForIDictionary { { "key1", "value1" } };
        }

        public void Verify()
        {
            Assert.Equal(1, List.Value.Count);
            Assert.Equal("Hello", ((JsonElement)List.Value[0]).GetString());
            Assert.Equal(1, Dictionary.Value.Keys.Count);
            Assert.Equal("value1", ((JsonElement)Dictionary.Value["key1"]).GetString());
        }
    }
}
