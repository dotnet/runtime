// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Collections
{
    public partial class CaseInsensitiveComparer : System.Collections.IComparer
    {
        public CaseInsensitiveComparer() { }
        public CaseInsensitiveComparer(System.Globalization.CultureInfo culture) { }
        public static System.Collections.CaseInsensitiveComparer Default { get { throw null; } }
        public static System.Collections.CaseInsensitiveComparer DefaultInvariant { get { throw null; } }
        public int Compare(object a, object b) { throw null; }
    }
    [System.ObsoleteAttribute("Please use StringComparer instead.")]
    public partial class CaseInsensitiveHashCodeProvider : System.Collections.IHashCodeProvider
    {
        public CaseInsensitiveHashCodeProvider() { }
        public CaseInsensitiveHashCodeProvider(System.Globalization.CultureInfo culture) { }
        public static System.Collections.CaseInsensitiveHashCodeProvider Default { get { throw null; } }
        public static System.Collections.CaseInsensitiveHashCodeProvider DefaultInvariant { get { throw null; } }
        public int GetHashCode(object obj) { throw null; }
    }
    public abstract partial class CollectionBase : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        protected CollectionBase() { }
        protected CollectionBase(int capacity) { }
        public int Capacity { get { throw null; } set { } }
        public int Count { get { throw null; } }
        protected System.Collections.ArrayList InnerList { get { throw null; } }
        protected System.Collections.IList List { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Clear() { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        protected virtual void OnClear() { }
        protected virtual void OnClearComplete() { }
        protected virtual void OnInsert(int index, object value) { }
        protected virtual void OnInsertComplete(int index, object value) { }
        protected virtual void OnRemove(int index, object value) { }
        protected virtual void OnRemoveComplete(int index, object value) { }
        protected virtual void OnSet(int index, object oldValue, object newValue) { }
        protected virtual void OnSetComplete(int index, object oldValue, object newValue) { }
        protected virtual void OnValidate(object value) { }
        public void RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    public abstract partial class DictionaryBase : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        protected DictionaryBase() { }
        public int Count { get { throw null; } }
        protected System.Collections.IDictionary Dictionary { get { throw null; } }
        protected System.Collections.Hashtable InnerHashtable { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
        bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
        object System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { throw null; } }
        public void Clear() { }
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        protected virtual void OnClear() { }
        protected virtual void OnClearComplete() { }
        protected virtual object OnGet(object key, object currentValue) { throw null; }
        protected virtual void OnInsert(object key, object value) { }
        protected virtual void OnInsertComplete(object key, object value) { }
        protected virtual void OnRemove(object key, object value) { }
        protected virtual void OnRemoveComplete(object key, object value) { }
        protected virtual void OnSet(object key, object oldValue, object newValue) { }
        protected virtual void OnSetComplete(object key, object oldValue, object newValue) { }
        protected virtual void OnValidate(object key, object value) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        bool System.Collections.IDictionary.Contains(object key) { throw null; }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class Queue : System.Collections.ICollection, System.Collections.IEnumerable, System.ICloneable
    {
        public Queue() { }
        public Queue(System.Collections.ICollection col) { }
        public Queue(int capacity) { }
        public Queue(int capacity, float growFactor) { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsSynchronized { get { throw null; } }
        public virtual object SyncRoot { get { throw null; } }
        public virtual void Clear() { }
        public virtual object Clone() { throw null; }
        public virtual bool Contains(object obj) { throw null; }
        public virtual void CopyTo(System.Array array, int index) { }
        public virtual object Dequeue() { throw null; }
        public virtual void Enqueue(object obj) { }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public virtual object Peek() { throw null; }
        public static System.Collections.Queue Synchronized(System.Collections.Queue queue) { throw null; }
        public virtual object[] ToArray() { throw null; }
        public virtual void TrimToSize() { }
    }
    public abstract partial class ReadOnlyCollectionBase : System.Collections.ICollection, System.Collections.IEnumerable
    {
        protected ReadOnlyCollectionBase() { }
        public virtual int Count { get { throw null; } }
        protected System.Collections.ArrayList InnerList { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    public partial class SortedList : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.ICloneable
    {
        public SortedList() { }
        public SortedList(System.Collections.IComparer comparer) { }
        public SortedList(System.Collections.IComparer comparer, int capacity) { }
        public SortedList(System.Collections.IDictionary d) { }
        public SortedList(System.Collections.IDictionary d, System.Collections.IComparer comparer) { }
        public SortedList(int initialCapacity) { }
        public virtual int Capacity { get { throw null; } set { } }
        public virtual int Count { get { throw null; } }
        public virtual bool IsFixedSize { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual bool IsSynchronized { get { throw null; } }
        public virtual object this[object key] { get { throw null; } set { } }
        public virtual System.Collections.ICollection Keys { get { throw null; } }
        public virtual object SyncRoot { get { throw null; } }
        public virtual System.Collections.ICollection Values { get { throw null; } }
        public virtual void Add(object key, object value) { }
        public virtual void Clear() { }
        public virtual object Clone() { throw null; }
        public virtual bool Contains(object key) { throw null; }
        public virtual bool ContainsKey(object key) { throw null; }
        public virtual bool ContainsValue(object value) { throw null; }
        public virtual void CopyTo(System.Array array, int arrayIndex) { }
        public virtual object GetByIndex(int index) { throw null; }
        public virtual System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        public virtual object GetKey(int index) { throw null; }
        public virtual System.Collections.IList GetKeyList() { throw null; }
        public virtual System.Collections.IList GetValueList() { throw null; }
        public virtual int IndexOfKey(object key) { throw null; }
        public virtual int IndexOfValue(object value) { throw null; }
        public virtual void Remove(object key) { }
        public virtual void RemoveAt(int index) { }
        public virtual void SetByIndex(int index, object value) { }
        public static System.Collections.SortedList Synchronized(System.Collections.SortedList list) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public virtual void TrimToSize() { }
    }
    public partial class Stack : System.Collections.ICollection, System.Collections.IEnumerable, System.ICloneable
    {
        public Stack() { }
        public Stack(System.Collections.ICollection col) { }
        public Stack(int initialCapacity) { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsSynchronized { get { throw null; } }
        public virtual object SyncRoot { get { throw null; } }
        public virtual void Clear() { }
        public virtual object Clone() { throw null; }
        public virtual bool Contains(object obj) { throw null; }
        public virtual void CopyTo(System.Array array, int index) { }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public virtual object Peek() { throw null; }
        public virtual object Pop() { throw null; }
        public virtual void Push(object obj) { }
        public static System.Collections.Stack Synchronized(System.Collections.Stack stack) { throw null; }
        public virtual object[] ToArray() { throw null; }
    }
}
namespace System.Collections.Specialized
{
    public partial class CollectionsUtil
    {
        public CollectionsUtil() { }
        public static System.Collections.Hashtable CreateCaseInsensitiveHashtable() { throw null; }
        public static System.Collections.Hashtable CreateCaseInsensitiveHashtable(System.Collections.IDictionary d) { throw null; }
        public static System.Collections.Hashtable CreateCaseInsensitiveHashtable(int capacity) { throw null; }
        public static System.Collections.SortedList CreateCaseInsensitiveSortedList() { throw null; }
    }
}
