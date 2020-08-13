// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Collections.ObjectModel
{
    public abstract partial class KeyedCollection<TKey, TItem> : System.Collections.ObjectModel.Collection<TItem> where TKey : notnull
    {
        protected KeyedCollection() { }
        protected KeyedCollection(System.Collections.Generic.IEqualityComparer<TKey>? comparer) { }
        protected KeyedCollection(System.Collections.Generic.IEqualityComparer<TKey>? comparer, int dictionaryCreationThreshold) { }
        public System.Collections.Generic.IEqualityComparer<TKey> Comparer { get { throw null; } }
        protected System.Collections.Generic.IDictionary<TKey, TItem>? Dictionary { get { throw null; } }
        public TItem this[TKey key] { get { throw null; } }
        protected void ChangeItemKey(TItem item, TKey newKey) { }
        protected override void ClearItems() { }
        public bool Contains(TKey key) { throw null; }
        protected abstract TKey GetKeyForItem(TItem item);
        protected override void InsertItem(int index, TItem item) { }
        public bool Remove(TKey key) { throw null; }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, TItem item) { }
        public bool TryGetValue(TKey key, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TItem item) { throw null; }
    }
    public partial class ObservableCollection<T> : System.Collections.ObjectModel.Collection<T>, System.Collections.Specialized.INotifyCollectionChanged, System.ComponentModel.INotifyPropertyChanged
    {
        public ObservableCollection() { }
        public ObservableCollection(System.Collections.Generic.IEnumerable<T> collection) { }
        public ObservableCollection(System.Collections.Generic.List<T> list) { }
        public virtual event System.Collections.Specialized.NotifyCollectionChangedEventHandler? CollectionChanged { add { } remove { } }
        protected virtual event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        event System.ComponentModel.PropertyChangedEventHandler? System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        protected System.IDisposable BlockReentrancy() { throw null; }
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
    public partial class ReadOnlyDictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable where TKey : notnull
    {
        public ReadOnlyDictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary) { }
        public int Count { get { throw null; } }
        protected System.Collections.Generic.IDictionary<TKey, TValue> Dictionary { get { throw null; } }
        public TValue this[TKey key] { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.KeyCollection Keys { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.IsReadOnly { get { throw null; } }
        TValue System.Collections.Generic.IDictionary<TKey, TValue>.this[TKey key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey, TValue>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey, TValue>.Values { get { throw null; } }
        System.Collections.Generic.IEnumerable<TKey> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Keys { get { throw null; } }
        System.Collections.Generic.IEnumerable<TValue> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Values { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
        bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
        object? System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.ValueCollection Values { get { throw null; } }
        public bool ContainsKey(TKey key) { throw null; }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> GetEnumerator() { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
        void System.Collections.Generic.IDictionary<TKey, TValue>.Add(TKey key, TValue value) { }
        bool System.Collections.Generic.IDictionary<TKey, TValue>.Remove(TKey key) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object? value) { }
        void System.Collections.IDictionary.Clear() { }
        bool System.Collections.IDictionary.Contains(object key) { throw null; }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(TKey key, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TValue value) { throw null; }
        public sealed partial class KeyCollection : System.Collections.Generic.ICollection<TKey>, System.Collections.Generic.IEnumerable<TKey>, System.Collections.Generic.IReadOnlyCollection<TKey>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            internal KeyCollection() { }
            public int Count { get { throw null; } }
            bool System.Collections.Generic.ICollection<TKey>.IsReadOnly { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            public void CopyTo(TKey[] array, int arrayIndex) { }
            public System.Collections.Generic.IEnumerator<TKey> GetEnumerator() { throw null; }
            void System.Collections.Generic.ICollection<TKey>.Add(TKey item) { }
            void System.Collections.Generic.ICollection<TKey>.Clear() { }
            bool System.Collections.Generic.ICollection<TKey>.Contains(TKey item) { throw null; }
            bool System.Collections.Generic.ICollection<TKey>.Remove(TKey item) { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        }
        public sealed partial class ValueCollection : System.Collections.Generic.ICollection<TValue>, System.Collections.Generic.IEnumerable<TValue>, System.Collections.Generic.IReadOnlyCollection<TValue>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            internal ValueCollection() { }
            public int Count { get { throw null; } }
            bool System.Collections.Generic.ICollection<TValue>.IsReadOnly { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            public void CopyTo(TValue[] array, int arrayIndex) { }
            public System.Collections.Generic.IEnumerator<TValue> GetEnumerator() { throw null; }
            void System.Collections.Generic.ICollection<TValue>.Add(TValue item) { }
            void System.Collections.Generic.ICollection<TValue>.Clear() { }
            bool System.Collections.Generic.ICollection<TValue>.Contains(TValue item) { throw null; }
            bool System.Collections.Generic.ICollection<TValue>.Remove(TValue item) { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        }
    }
    public partial class ReadOnlyObservableCollection<T> : System.Collections.ObjectModel.ReadOnlyCollection<T>, System.Collections.Specialized.INotifyCollectionChanged, System.ComponentModel.INotifyPropertyChanged
    {
        public ReadOnlyObservableCollection(System.Collections.ObjectModel.ObservableCollection<T> list) : base (default(System.Collections.Generic.IList<T>)) { }
        protected virtual event System.Collections.Specialized.NotifyCollectionChangedEventHandler? CollectionChanged { add { } remove { } }
        protected virtual event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        event System.Collections.Specialized.NotifyCollectionChangedEventHandler? System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged { add { } remove { } }
        event System.ComponentModel.PropertyChangedEventHandler? System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        protected virtual void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { }
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args) { }
    }
}
namespace System.Collections.Specialized
{
    public partial interface INotifyCollectionChanged
    {
        event System.Collections.Specialized.NotifyCollectionChangedEventHandler? CollectionChanged;
    }
    public enum NotifyCollectionChangedAction
    {
        Add = 0,
        Remove = 1,
        Replace = 2,
        Move = 3,
        Reset = 4,
    }
    public partial class NotifyCollectionChangedEventArgs : System.EventArgs
    {
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, System.Collections.IList? changedItems) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, System.Collections.IList newItems, System.Collections.IList oldItems) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, System.Collections.IList newItems, System.Collections.IList oldItems, int startingIndex) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, System.Collections.IList? changedItems, int startingIndex) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, System.Collections.IList? changedItems, int index, int oldIndex) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, object? changedItem) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, object? changedItem, int index) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, object? changedItem, int index, int oldIndex) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, object? newItem, object? oldItem) { }
        public NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction action, object? newItem, object? oldItem, int index) { }
        public System.Collections.Specialized.NotifyCollectionChangedAction Action { get { throw null; } }
        public System.Collections.IList? NewItems { get { throw null; } }
        public int NewStartingIndex { get { throw null; } }
        public System.Collections.IList? OldItems { get { throw null; } }
        public int OldStartingIndex { get { throw null; } }
    }
    public delegate void NotifyCollectionChangedEventHandler(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e);
}
namespace System.ComponentModel
{
    public partial class DataErrorsChangedEventArgs : System.EventArgs
    {
        public DataErrorsChangedEventArgs(string? propertyName) { }
        public virtual string? PropertyName { get { throw null; } }
    }
    public partial interface INotifyDataErrorInfo
    {
        bool HasErrors { get; }
        event System.EventHandler<System.ComponentModel.DataErrorsChangedEventArgs>? ErrorsChanged;
        System.Collections.IEnumerable GetErrors(string? propertyName);
    }
    public partial interface INotifyPropertyChanged
    {
        event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
    public partial interface INotifyPropertyChanging
    {
        event System.ComponentModel.PropertyChangingEventHandler? PropertyChanging;
    }
    public partial class PropertyChangedEventArgs : System.EventArgs
    {
        public PropertyChangedEventArgs(string? propertyName) { }
        public virtual string? PropertyName { get { throw null; } }
    }
    public delegate void PropertyChangedEventHandler(object? sender, System.ComponentModel.PropertyChangedEventArgs e);
    public partial class PropertyChangingEventArgs : System.EventArgs
    {
        public PropertyChangingEventArgs(string? propertyName) { }
        public virtual string? PropertyName { get { throw null; } }
    }
    public delegate void PropertyChangingEventHandler(object? sender, System.ComponentModel.PropertyChangingEventArgs e);
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public sealed partial class TypeConverterAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.TypeConverterAttribute Default;
        public TypeConverterAttribute() { }
        public TypeConverterAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] string typeName) { }
        public TypeConverterAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type type) { }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        public string ConverterTypeName { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, Inherited=true)]
    public sealed partial class TypeDescriptionProviderAttribute : System.Attribute
    {
        public TypeDescriptionProviderAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string typeName) { }
        public TypeDescriptionProviderAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type type) { }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public string TypeName { get { throw null; } }
    }
}
namespace System.Reflection
{
    public partial interface ICustomTypeProvider
    {
        System.Type GetCustomType();
    }
}
namespace System.Windows.Input
{
    [System.ComponentModel.TypeConverterAttribute("System.Windows.Input.CommandConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
    [System.Windows.Markup.ValueSerializerAttribute("System.Windows.Input.CommandValueSerializer, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
    public partial interface ICommand
    {
        event System.EventHandler? CanExecuteChanged;
        bool CanExecute(object? parameter);
        void Execute(object? parameter);
    }
}
namespace System.Windows.Markup
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Enum | System.AttributeTargets.Interface | System.AttributeTargets.Method | System.AttributeTargets.Property | System.AttributeTargets.Struct, AllowMultiple=false, Inherited=true)]
    public sealed partial class ValueSerializerAttribute : System.Attribute
    {
        public ValueSerializerAttribute(string valueSerializerTypeName) { }
        public ValueSerializerAttribute(System.Type valueSerializerType) { }
        public System.Type ValueSerializerType { get { throw null; } }
        public string ValueSerializerTypeName { get { throw null; } }
    }
}
