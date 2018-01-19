// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Collections.Specialized
{
    public partial struct BitVector32
    {
        private int _dummy;
        public BitVector32(System.Collections.Specialized.BitVector32 value) { throw null; }
        public BitVector32(int data) { throw null; }
        public int Data { get { throw null; } }
        public int this[System.Collections.Specialized.BitVector32.Section section] { get { throw null; } set { } }
        public bool this[int bit] { get { throw null; } set { } }
        public static int CreateMask() { throw null; }
        public static int CreateMask(int previous) { throw null; }
        public static System.Collections.Specialized.BitVector32.Section CreateSection(short maxValue) { throw null; }
        public static System.Collections.Specialized.BitVector32.Section CreateSection(short maxValue, System.Collections.Specialized.BitVector32.Section previous) { throw null; }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
        public static string ToString(System.Collections.Specialized.BitVector32 value) { throw null; }
        public readonly partial struct Section
        {
            private readonly int _dummy;
            public short Mask { get { throw null; } }
            public short Offset { get { throw null; } }
            public bool Equals(System.Collections.Specialized.BitVector32.Section obj) { throw null; }
            public override bool Equals(object o) { throw null; }
            public override int GetHashCode() { throw null; }
            public static bool operator ==(System.Collections.Specialized.BitVector32.Section a, System.Collections.Specialized.BitVector32.Section b) { throw null; }
            public static bool operator !=(System.Collections.Specialized.BitVector32.Section a, System.Collections.Specialized.BitVector32.Section b) { throw null; }
            public override string ToString() { throw null; }
            public static string ToString(System.Collections.Specialized.BitVector32.Section value) { throw null; }
        }
    }
    public partial class HybridDictionary : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public HybridDictionary() { }
        public HybridDictionary(bool caseInsensitive) { }
        public HybridDictionary(int initialSize) { }
        public HybridDictionary(int initialSize, bool caseInsensitive) { }
        public int Count { get { throw null; } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public object this[object key] { get { throw null; } set { } }
        public System.Collections.ICollection Keys { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public void Add(object key, object value) { }
        public void Clear() { }
        public bool Contains(object key) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        public void Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial interface IOrderedDictionary : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        object this[int index] { get; set; }
        new System.Collections.IDictionaryEnumerator GetEnumerator();
        void Insert(int index, object key, object value);
        void RemoveAt(int index);
    }
    public partial class ListDictionary : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public ListDictionary() { }
        public ListDictionary(System.Collections.IComparer comparer) { }
        public int Count { get { throw null; } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public object this[object key] { get { throw null; } set { } }
        public System.Collections.ICollection Keys { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public void Add(object key, object value) { }
        public void Clear() { }
        public bool Contains(object key) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        public void Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public abstract partial class NameObjectCollectionBase : System.Collections.ICollection, System.Collections.IEnumerable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        protected NameObjectCollectionBase() { }
        protected NameObjectCollectionBase(System.Collections.IEqualityComparer equalityComparer) { }
        [System.ObsoleteAttribute("Please use NameObjectCollectionBase(IEqualityComparer) instead.")]
        protected NameObjectCollectionBase(System.Collections.IHashCodeProvider hashProvider, System.Collections.IComparer comparer) { }
        protected NameObjectCollectionBase(int capacity) { }
        protected NameObjectCollectionBase(int capacity, System.Collections.IEqualityComparer equalityComparer) { }
        [System.ObsoleteAttribute("Please use NameObjectCollectionBase(Int32, IEqualityComparer) instead.")]
        protected NameObjectCollectionBase(int capacity, System.Collections.IHashCodeProvider hashProvider, System.Collections.IComparer comparer) { }
        protected NameObjectCollectionBase(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual int Count { get { throw null; } }
        protected bool IsReadOnly { get { throw null; } set { } }
        public virtual System.Collections.Specialized.NameObjectCollectionBase.KeysCollection Keys { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        protected void BaseAdd(string name, object value) { }
        protected void BaseClear() { }
        protected object BaseGet(int index) { throw null; }
        protected object BaseGet(string name) { throw null; }
        protected string[] BaseGetAllKeys() { throw null; }
        protected object[] BaseGetAllValues() { throw null; }
        protected object[] BaseGetAllValues(System.Type type) { throw null; }
        protected string BaseGetKey(int index) { throw null; }
        protected bool BaseHasKeys() { throw null; }
        protected void BaseRemove(string name) { }
        protected void BaseRemoveAt(int index) { }
        protected void BaseSet(int index, object value) { }
        protected void BaseSet(string name, object value) { }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual void OnDeserialization(object sender) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        public partial class KeysCollection : System.Collections.ICollection, System.Collections.IEnumerable
        {
            internal KeysCollection() { }
            public int Count { get { throw null; } }
            public string this[int index] { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            public virtual string Get(int index) { throw null; }
            public System.Collections.IEnumerator GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        }
    }
    public partial class NameValueCollection : System.Collections.Specialized.NameObjectCollectionBase
    {
        public NameValueCollection() { }
        public NameValueCollection(System.Collections.IEqualityComparer equalityComparer) { }
        [System.ObsoleteAttribute("Please use NameValueCollection(IEqualityComparer) instead.")]
        public NameValueCollection(System.Collections.IHashCodeProvider hashProvider, System.Collections.IComparer comparer) { }
        public NameValueCollection(System.Collections.Specialized.NameValueCollection col) { }
        public NameValueCollection(int capacity) { }
        public NameValueCollection(int capacity, System.Collections.IEqualityComparer equalityComparer) { }
        [System.ObsoleteAttribute("Please use NameValueCollection(Int32, IEqualityComparer) instead.")]
        public NameValueCollection(int capacity, System.Collections.IHashCodeProvider hashProvider, System.Collections.IComparer comparer) { }
        public NameValueCollection(int capacity, System.Collections.Specialized.NameValueCollection col) { }
        protected NameValueCollection(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual string[] AllKeys { get { throw null; } }
        public string this[int index] { get { throw null; } }
        public string this[string name] { get { throw null; } set { } }
        public void Add(System.Collections.Specialized.NameValueCollection c) { }
        public virtual void Add(string name, string value) { }
        public virtual void Clear() { }
        public void CopyTo(System.Array dest, int index) { }
        public virtual string Get(int index) { throw null; }
        public virtual string Get(string name) { throw null; }
        public virtual string GetKey(int index) { throw null; }
        public virtual string[] GetValues(int index) { throw null; }
        public virtual string[] GetValues(string name) { throw null; }
        public bool HasKeys() { throw null; }
        protected void InvalidateCachedArrays() { }
        public virtual void Remove(string name) { }
        public virtual void Set(string name, string value) { }
    }
    public partial class OrderedDictionary : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.Collections.Specialized.IOrderedDictionary, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public OrderedDictionary() { }
        public OrderedDictionary(System.Collections.IEqualityComparer comparer) { }
        public OrderedDictionary(int capacity) { }
        public OrderedDictionary(int capacity, System.Collections.IEqualityComparer comparer) { }
        protected OrderedDictionary(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public object this[int index] { get { throw null; } set { } }
        public object this[object key] { get { throw null; } set { } }
        public System.Collections.ICollection Keys { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public void Add(object key, object value) { }
        public System.Collections.Specialized.OrderedDictionary AsReadOnly() { throw null; }
        public void Clear() { }
        public bool Contains(object key) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public virtual System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void Insert(int index, object key, object value) { }
        protected virtual void OnDeserialization(object sender) { }
        public void Remove(object key) { }
        public void RemoveAt(int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
    }
    public partial class StringCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public StringCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public string this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public int Add(string value) { throw null; }
        public void AddRange(string[] value) { }
        public void Clear() { }
        public bool Contains(string value) { throw null; }
        public void CopyTo(string[] array, int index) { }
        public System.Collections.Specialized.StringEnumerator GetEnumerator() { throw null; }
        public int IndexOf(string value) { throw null; }
        public void Insert(int index, string value) { }
        public void Remove(string value) { }
        public void RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    public partial class StringDictionary : System.Collections.IEnumerable
    {
        public StringDictionary() { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsSynchronized { get { throw null; } }
        public virtual string this[string key] { get { throw null; } set { } }
        public virtual System.Collections.ICollection Keys { get { throw null; } }
        public virtual object SyncRoot { get { throw null; } }
        public virtual System.Collections.ICollection Values { get { throw null; } }
        public virtual void Add(string key, string value) { }
        public virtual void Clear() { }
        public virtual bool ContainsKey(string key) { throw null; }
        public virtual bool ContainsValue(string value) { throw null; }
        public virtual void CopyTo(System.Array array, int index) { }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public virtual void Remove(string key) { }
    }
    public partial class StringEnumerator
    {
        internal StringEnumerator() { }
        public string Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
}
