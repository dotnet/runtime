// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Collections.ObjectModel
{
    public abstract partial class KeyedCollection<TKey, TItem> : System.Collections.ObjectModel.Collection<TItem>
    {
        protected KeyedCollection() { }
        protected KeyedCollection(System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        protected KeyedCollection(System.Collections.Generic.IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold) { }
        public System.Collections.Generic.IEqualityComparer<TKey> Comparer { get { return default(System.Collections.Generic.IEqualityComparer<TKey>); } }
        protected System.Collections.Generic.IDictionary<TKey, TItem> Dictionary { get { return default(System.Collections.Generic.IDictionary<TKey, TItem>); } }
        public TItem this[TKey key] { get { return default(TItem); } }
        protected void ChangeItemKey(TItem item, TKey newKey) { }
        protected override void ClearItems() { }
        public bool Contains(TKey key) { return default(bool); }
        protected abstract TKey GetKeyForItem(TItem item);
        protected override void InsertItem(int index, TItem item) { }
        public bool Remove(TKey key) { return default(bool); }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, TItem item) { }
    }
    public partial class ObservableCollection<T> : System.Collections.ObjectModel.Collection<T>, System.Collections.Specialized.INotifyCollectionChanged, System.ComponentModel.INotifyPropertyChanged
    {
        public ObservableCollection() { }
        public ObservableCollection(System.Collections.Generic.IEnumerable<T> collection) { }
        public virtual event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged { add { } remove { } }
        protected virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        protected System.IDisposable BlockReentrancy() { return default(System.IDisposable); }
        protected void CheckReentrancy() { }
        protected override void ClearItems() { }
        protected override void InsertItem(int index, T item) { }
        public void Move(int oldIndex, int newIndex) { }
        protected virtual void MoveItem(int oldIndex, int newIndex) { }
        protected virtual void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e) { }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, T item) { }
    }
    public partial class ReadOnlyDictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public ReadOnlyDictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary) { }
        public int Count { get { return default(int); } }
        protected System.Collections.Generic.IDictionary<TKey, TValue> Dictionary { get { return default(System.Collections.Generic.IDictionary<TKey, TValue>); } }
        public TValue this[TKey key] { get { return default(TValue); } }
        public System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.KeyCollection Keys { get { return default(System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.KeyCollection); } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.IsReadOnly { get { return default(bool); } }
        TValue System.Collections.Generic.IDictionary<TKey, TValue>.this[TKey key] { get { return default(TValue); } set { } }
        System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey, TValue>.Keys { get { return default(System.Collections.Generic.ICollection<TKey>); } }
        System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey, TValue>.Values { get { return default(System.Collections.Generic.ICollection<TValue>); } }
        System.Collections.Generic.IEnumerable<TKey> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Keys { get { return default(System.Collections.Generic.IEnumerable<TKey>); } }
        System.Collections.Generic.IEnumerable<TValue> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Values { get { return default(System.Collections.Generic.IEnumerable<TValue>); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IDictionary.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IDictionary.IsReadOnly { get { return default(bool); } }
        object System.Collections.IDictionary.this[object key] { get { return default(object); } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { return default(System.Collections.ICollection); } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { return default(System.Collections.ICollection); } }
        public System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.ValueCollection Values { get { return default(System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.ValueCollection); } }
        public bool ContainsKey(TKey key) { return default(bool); }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>); }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { return default(bool); }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { return default(bool); }
        void System.Collections.Generic.IDictionary<TKey, TValue>.Add(TKey key, TValue value) { }
        bool System.Collections.Generic.IDictionary<TKey, TValue>.Remove(TKey key) { return default(bool); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        void System.Collections.IDictionary.Clear() { }
        bool System.Collections.IDictionary.Contains(object key) { return default(bool); }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public bool TryGetValue(TKey key, out TValue value) { value = default(TValue); return default(bool); }
        public sealed partial class KeyCollection : System.Collections.Generic.ICollection<TKey>, System.Collections.Generic.IEnumerable<TKey>, System.Collections.Generic.IReadOnlyCollection<TKey>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            internal KeyCollection() { }
            public int Count { get { return default(int); } }
            bool System.Collections.Generic.ICollection<TKey>.IsReadOnly { get { return default(bool); } }
            bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
            object System.Collections.ICollection.SyncRoot { get { return default(object); } }
            public void CopyTo(TKey[] array, int arrayIndex) { }
            public System.Collections.Generic.IEnumerator<TKey> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<TKey>); }
            void System.Collections.Generic.ICollection<TKey>.Add(TKey item) { }
            void System.Collections.Generic.ICollection<TKey>.Clear() { }
            bool System.Collections.Generic.ICollection<TKey>.Contains(TKey item) { return default(bool); }
            bool System.Collections.Generic.ICollection<TKey>.Remove(TKey item) { return default(bool); }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        }
        public sealed partial class ValueCollection : System.Collections.Generic.ICollection<TValue>, System.Collections.Generic.IEnumerable<TValue>, System.Collections.Generic.IReadOnlyCollection<TValue>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            internal ValueCollection() { }
            public int Count { get { return default(int); } }
            bool System.Collections.Generic.ICollection<TValue>.IsReadOnly { get { return default(bool); } }
            bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
            object System.Collections.ICollection.SyncRoot { get { return default(object); } }
            public void CopyTo(TValue[] array, int arrayIndex) { }
            public System.Collections.Generic.IEnumerator<TValue> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<TValue>); }
            void System.Collections.Generic.ICollection<TValue>.Add(TValue item) { }
            void System.Collections.Generic.ICollection<TValue>.Clear() { }
            bool System.Collections.Generic.ICollection<TValue>.Contains(TValue item) { return default(bool); }
            bool System.Collections.Generic.ICollection<TValue>.Remove(TValue item) { return default(bool); }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        }
    }
    public partial class ReadOnlyObservableCollection<T> : System.Collections.ObjectModel.ReadOnlyCollection<T>, System.Collections.Specialized.INotifyCollectionChanged, System.ComponentModel.INotifyPropertyChanged
    {
        public ReadOnlyObservableCollection(System.Collections.ObjectModel.ObservableCollection<T> list) : base(default(System.Collections.Generic.IList<T>)) { }
        protected virtual event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged { add { } remove { } }
        protected virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }
        event System.Collections.Specialized.NotifyCollectionChangedEventHandler System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged { add { } remove { } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        protected virtual void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args) { }
    }
}
namespace System.Collections.Specialized
{
    public partial interface INotifyCollectionChanged
    {
        event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;
    }
    public enum NotifyCollectionChangedAction
    {
        Add = 0,
        Move = 3,
        Remove = 1,
        Replace = 2,
        Reset = 4,
    }
    public partial class NotifyCollectionChangedEventArgs : System.EventArgs
    {
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, System.Collections.IList changedItems) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, System.Collections.IList newItems, System.Collections.IList oldItems) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, System.Collections.IList newItems, System.Collections.IList oldItems, int startingIndex) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, System.Collections.IList changedItems, int startingIndex) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, System.Collections.IList changedItems, int index, int oldIndex) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, object changedItem) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, object changedItem, int index) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, object changedItem, int index, int oldIndex) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, object newItem, object oldItem) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, object newItem, object oldItem, int index) { }
        public System.Collections.Specialized.NotifyCollectionChangedAction Action { get { return default(System.Collections.Specialized.NotifyCollectionChangedAction); } }
        public System.Collections.IList NewItems { get { return default(System.Collections.IList); } }
        public int NewStartingIndex { get { return default(int); } }
        public System.Collections.IList OldItems { get { return default(System.Collections.IList); } }
        public int OldStartingIndex { get { return default(int); } }
    }
    public delegate void NotifyCollectionChangedEventHandler(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e);
}
namespace System.ComponentModel
{
    public partial class DataErrorsChangedEventArgs : System.EventArgs
    {
        public DataErrorsChangedEventArgs(string propertyName) { }
        public virtual string PropertyName { get { return default(string); } }
    }
    public partial interface INotifyDataErrorInfo
    {
        bool HasErrors { get; }
        event System.EventHandler<System.ComponentModel.DataErrorsChangedEventArgs> ErrorsChanged;
        System.Collections.IEnumerable GetErrors(string propertyName);
    }
    public partial interface INotifyPropertyChanged
    {
        event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }
    public partial interface INotifyPropertyChanging
    {
        event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;
    }
    public partial class PropertyChangedEventArgs : System.EventArgs
    {
        public PropertyChangedEventArgs(string propertyName) { }
        public virtual string PropertyName { get { return default(string); } }
    }
    public delegate void PropertyChangedEventHandler(object sender, System.ComponentModel.PropertyChangedEventArgs e);
    public partial class PropertyChangingEventArgs : System.EventArgs
    {
        public PropertyChangingEventArgs(string propertyName) { }
        public virtual string PropertyName { get { return default(string); } }
    }
    public delegate void PropertyChangingEventHandler(object sender, System.ComponentModel.PropertyChangingEventArgs e);
}
namespace System.Windows.Input
{
    public partial interface ICommand
    {
        event System.EventHandler CanExecuteChanged;
        bool CanExecute(object parameter);
        void Execute(object parameter);
    }
}
