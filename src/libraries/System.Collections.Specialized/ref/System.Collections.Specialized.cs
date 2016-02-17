// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Collections.Specialized
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct BitVector32
    {
        public BitVector32(System.Collections.Specialized.BitVector32 value) { throw new System.NotImplementedException(); }
        public BitVector32(int data) { throw new System.NotImplementedException(); }
        public int Data { get { return default(int); } }
        public int this[System.Collections.Specialized.BitVector32.Section section] { get { return default(int); } set { } }
        public bool this[int bit] { get { return default(bool); } set { } }
        public static int CreateMask() { return default(int); }
        public static int CreateMask(int previous) { return default(int); }
        public static System.Collections.Specialized.BitVector32.Section CreateSection(short maxValue) { return default(System.Collections.Specialized.BitVector32.Section); }
        public static System.Collections.Specialized.BitVector32.Section CreateSection(short maxValue, System.Collections.Specialized.BitVector32.Section previous) { return default(System.Collections.Specialized.BitVector32.Section); }
        public override bool Equals(object o) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override string ToString() { return default(string); }
        public static string ToString(System.Collections.Specialized.BitVector32 value) { return default(string); }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Section
        {
            public short Mask { get { return default(short); } }
            public short Offset { get { return default(short); } }
            public bool Equals(System.Collections.Specialized.BitVector32.Section obj) { return default(bool); }
            public override bool Equals(object o) { return default(bool); }
            public override int GetHashCode() { return default(int); }
            public static bool operator ==(System.Collections.Specialized.BitVector32.Section a, System.Collections.Specialized.BitVector32.Section b) { return default(bool); }
            public static bool operator !=(System.Collections.Specialized.BitVector32.Section a, System.Collections.Specialized.BitVector32.Section b) { return default(bool); }
            public override string ToString() { return default(string); }
            public static string ToString(System.Collections.Specialized.BitVector32.Section value) { return default(string); }
        }
    }
    public partial class HybridDictionary : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public HybridDictionary() { }
        public HybridDictionary(bool caseInsensitive) { }
        public HybridDictionary(int initialSize) { }
        public HybridDictionary(int initialSize, bool caseInsensitive) { }
        public int Count { get { return default(int); } }
        public bool IsFixedSize { get { return default(bool); } }
        public bool IsReadOnly { get { return default(bool); } }
        public bool IsSynchronized { get { return default(bool); } }
        public object this[object key] { get { return default(object); } set { } }
        public System.Collections.ICollection Keys { get { return default(System.Collections.ICollection); } }
        public object SyncRoot { get { return default(object); } }
        public System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
        public void Add(object key, object value) { }
        public void Clear() { }
        public bool Contains(object key) { return default(bool); }
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
        public void Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
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
        public int Count { get { return default(int); } }
        public bool IsFixedSize { get { return default(bool); } }
        public bool IsReadOnly { get { return default(bool); } }
        public bool IsSynchronized { get { return default(bool); } }
        public object this[object key] { get { return default(object); } set { } }
        public System.Collections.ICollection Keys { get { return default(System.Collections.ICollection); } }
        public object SyncRoot { get { return default(object); } }
        public System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
        public void Add(object key, object value) { }
        public void Clear() { }
        public bool Contains(object key) { return default(bool); }
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
        public void Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public abstract partial class NameObjectCollectionBase : System.Collections.ICollection, System.Collections.IEnumerable
    {
        protected NameObjectCollectionBase() { }
        protected NameObjectCollectionBase(System.Collections.IEqualityComparer equalityComparer) { }
        protected NameObjectCollectionBase(int capacity) { }
        protected NameObjectCollectionBase(int capacity, System.Collections.IEqualityComparer equalityComparer) { }
        public virtual int Count { get { return default(int); } }
        protected bool IsReadOnly { get { return default(bool); } set { } }
        public virtual System.Collections.Specialized.NameObjectCollectionBase.KeysCollection Keys { get { return default(System.Collections.Specialized.NameObjectCollectionBase.KeysCollection); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        protected void BaseAdd(string name, object value) { }
        protected void BaseClear() { }
        protected object BaseGet(int index) { return default(object); }
        protected object BaseGet(string name) { return default(object); }
        protected string[] BaseGetAllKeys() { return default(string[]); }
        protected object[] BaseGetAllValues() { return default(object[]); }
        protected object[] BaseGetAllValues(System.Type type) { return default(object[]); }
        protected string BaseGetKey(int index) { return default(string); }
        protected bool BaseHasKeys() { return default(bool); }
        protected void BaseRemove(string name) { }
        protected void BaseRemoveAt(int index) { }
        protected void BaseSet(int index, object value) { }
        protected void BaseSet(string name, object value) { }
        public virtual System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        public partial class KeysCollection : System.Collections.ICollection, System.Collections.IEnumerable
        {
            internal KeysCollection() { }
            public int Count { get { return default(int); } }
            public string this[int index] { get { return default(string); } }
            bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
            object System.Collections.ICollection.SyncRoot { get { return default(object); } }
            public virtual string Get(int index) { return default(string); }
            public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        }
    }
    public partial class NameValueCollection : System.Collections.Specialized.NameObjectCollectionBase
    {
        public NameValueCollection() { }
        public NameValueCollection(System.Collections.IEqualityComparer equalityComparer) { }
        public NameValueCollection(System.Collections.Specialized.NameValueCollection col) { }
        public NameValueCollection(int capacity) { }
        public NameValueCollection(int capacity, System.Collections.IEqualityComparer equalityComparer) { }
        public NameValueCollection(int capacity, System.Collections.Specialized.NameValueCollection col) { }
        public virtual string[] AllKeys { get { return default(string[]); } }
        public string this[int index] { get { return default(string); } }
        public string this[string name] { get { return default(string); } set { } }
        public void Add(System.Collections.Specialized.NameValueCollection c) { }
        public virtual void Add(string name, string value) { }
        public virtual void Clear() { }
        public void CopyTo(System.Array dest, int index) { }
        public virtual string Get(int index) { return default(string); }
        public virtual string Get(string name) { return default(string); }
        public virtual string GetKey(int index) { return default(string); }
        public virtual string[] GetValues(int index) { return default(string[]); }
        public virtual string[] GetValues(string name) { return default(string[]); }
        public bool HasKeys() { return default(bool); }
        protected void InvalidateCachedArrays() { }
        public virtual void Remove(string name) { }
        public virtual void Set(string name, string value) { }
    }
    public partial class OrderedDictionary : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.Collections.Specialized.IOrderedDictionary
    {
        public OrderedDictionary() { }
        public OrderedDictionary(System.Collections.IEqualityComparer comparer) { }
        public OrderedDictionary(int capacity) { }
        public OrderedDictionary(int capacity, System.Collections.IEqualityComparer comparer) { }
        public int Count { get { return default(int); } }
        public bool IsReadOnly { get { return default(bool); } }
        public object this[int index] { get { return default(object); } set { } }
        public object this[object key] { get { return default(object); } set { } }
        public System.Collections.ICollection Keys { get { return default(System.Collections.ICollection); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IDictionary.IsFixedSize { get { return default(bool); } }
        public System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
        public void Add(object key, object value) { }
        public System.Collections.Specialized.OrderedDictionary AsReadOnly() { return default(System.Collections.Specialized.OrderedDictionary); }
        public void Clear() { }
        public bool Contains(object key) { return default(bool); }
        public void CopyTo(System.Array array, int index) { }
        public virtual System.Collections.IDictionaryEnumerator GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
        public void Insert(int index, object key, object value) { }
        public void Remove(object key) { }
        public void RemoveAt(int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public partial class StringCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public StringCollection() { }
        public int Count { get { return default(int); } }
        public bool IsReadOnly { get { return default(bool); } }
        public bool IsSynchronized { get { return default(bool); } }
        public string this[int index] { get { return default(string); } set { } }
        public object SyncRoot { get { return default(object); } }
        bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        public int Add(string value) { return default(int); }
        public void AddRange(string[] value) { }
        public void Clear() { }
        public bool Contains(string value) { return default(bool); }
        public void CopyTo(string[] array, int index) { }
        public System.Collections.Specialized.StringEnumerator GetEnumerator() { return default(System.Collections.Specialized.StringEnumerator); }
        public int IndexOf(string value) { return default(int); }
        public void Insert(int index, string value) { }
        public void Remove(string value) { }
        public void RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        int System.Collections.IList.Add(object value) { return default(int); }
        bool System.Collections.IList.Contains(object value) { return default(bool); }
        int System.Collections.IList.IndexOf(object value) { return default(int); }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    public partial class StringDictionary : System.Collections.IEnumerable
    {
        public StringDictionary() { }
        public virtual int Count { get { return default(int); } }
        public virtual bool IsSynchronized { get { return default(bool); } }
        public virtual string this[string key] { get { return default(string); } set { } }
        public virtual System.Collections.ICollection Keys { get { return default(System.Collections.ICollection); } }
        public virtual object SyncRoot { get { return default(object); } }
        public virtual System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
        public virtual void Add(string key, string value) { }
        public virtual void Clear() { }
        public virtual bool ContainsKey(string key) { return default(bool); }
        public virtual bool ContainsValue(string value) { return default(bool); }
        public virtual void CopyTo(System.Array array, int index) { }
        public virtual System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public virtual void Remove(string key) { }
    }
    public partial class StringEnumerator
    {
        internal StringEnumerator() { }
        public string Current { get { return default(string); } }
        public bool MoveNext() { return default(bool); }
        public void Reset() { }
    }
}
