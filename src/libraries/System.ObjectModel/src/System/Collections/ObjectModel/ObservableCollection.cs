// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.ObjectModel
{
    /// <summary>
    /// Implementation of a dynamic data collection based on generic Collection&lt;T&gt;,
    /// implementing INotifyCollectionChanged to notify listeners
    /// when items get added, removed or the whole list is refreshed.
    /// </summary>
    [Serializable]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class ObservableCollection<T> : Collection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private SimpleMonitor? _monitor; // Lazily allocated only when a subclass calls BlockReentrancy() or during serialization. Do not rename (binary serialization)

        [NonSerialized]
        private int _blockReentrancyCount;

        [NonSerialized]
        private bool _skipRaisingEvents;

        /// <summary>
        /// <c>true</c> to opt into raising <see cref="NotifyCollectionChangedEventArgs"/> with list
        /// of items when a range is inserted, removed or replaced. Instead of resets
        /// </summary>
        private static bool RaiseBatchCollectionChangedEvents => false;

        /// <summary>
        /// Initializes a new instance of ObservableCollection that is empty and has default initial capacity.
        /// </summary>
        public ObservableCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ObservableCollection class that contains
        /// elements copied from the specified collection and has sufficient capacity
        /// to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <remarks>
        /// The elements are copied onto the ObservableCollection in the
        /// same order they are read by the enumerator of the collection.
        /// </remarks>
        /// <exception cref="ArgumentNullException"> collection is a null reference </exception>
        public ObservableCollection(IEnumerable<T> collection) : base(new List<T>(collection ?? throw new ArgumentNullException(nameof(collection))))
        {
        }

        /// <summary>
        /// Initializes a new instance of the ObservableCollection class
        /// that contains elements copied from the specified list
        /// </summary>
        /// <param name="list">The list whose elements are copied to the new list.</param>
        /// <remarks>
        /// The elements are copied onto the ObservableCollection in the
        /// same order they are read by the enumerator of the list.
        /// </remarks>
        /// <exception cref="ArgumentNullException"> list is a null reference </exception>
        public ObservableCollection(List<T> list) : base(new List<T>(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        /// <summary>
        /// Move item at oldIndex to newIndex.
        /// </summary>
        public void Move(int oldIndex, int newIndex) => MoveItem(oldIndex, newIndex);


        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add => PropertyChanged += value;
            remove => PropertyChanged -= value;
        }

        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        [field: NonSerialized]
        public virtual event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when the list is being cleared;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void ClearItems()
        {
            CheckReentrancy();
            base.ClearItems();
            OnCountPropertyChanged();
            OnIndexerPropertyChanged();
            OnCollectionReset();
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when an item is removed from list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void RemoveItem(int index)
        {
            CheckReentrancy();
            T removedItem = this[index];

            base.RemoveItem(index);

            if (!_skipRaisingEvents)
            {
                OnCountPropertyChanged();
                OnIndexerPropertyChanged();
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
            }
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when a count of items is removed from the list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void RemoveItemsRange(int index, int count)
        {
            CheckReentrancy();

            NotifyCollectionChangedEventArgs collectionChangedEventArgs = EventArgsCache.ResetCollectionChanged;
            bool skipEvents = _skipRaisingEvents;
            if (!skipEvents)
            {
                _skipRaisingEvents = true;

                if (RaiseBatchCollectionChangedEvents && count > 0 && CollectionChanged is not null)
                {
                    T[] removedItems = new T[count];
                    for (int i = 0; i < count; i++)
                    {
                        removedItems[i] = this[index + i];
                    }

                    collectionChangedEventArgs = new(NotifyCollectionChangedAction.Remove, removedItems, index);
                }
            }

            try
            {
                base.RemoveItemsRange(index, count);
            }
            finally
            {
                if (!skipEvents)
                {
                    _skipRaisingEvents = false;
                }
            }

            if (count > 0 && !_skipRaisingEvents)
            {
                OnCountPropertyChanged();
                OnIndexerPropertyChanged();
                OnCollectionChanged(collectionChangedEventArgs);
            }
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when a collection of items is added to list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void ReplaceItemsRange(int index, int count, IEnumerable<T> collection)
        {
            CheckReentrancy();

            int countBefore = default;
            bool skipEvents = _skipRaisingEvents;
            if (!skipEvents)
            {
                _skipRaisingEvents = true;
                countBefore = Count;
            }

            NotifyCollectionChangedEventArgs collectionChangedEventArgs = EventArgsCache.ResetCollectionChanged;
            if (!skipEvents && RaiseBatchCollectionChangedEvents && CollectionChanged is not null)
            {
                T[] itemsToReplace = new T[count];
                for (int i = 0; i < count; i++)
                {
                    itemsToReplace[i] = this[i + index];
                }

                IList newItems = collection as IList ?? new List<T>(collection);
                collectionChangedEventArgs = new(NotifyCollectionChangedAction.Replace, newItems, itemsToReplace, index);
            }

            try
            {
                base.ReplaceItemsRange(index, count, collection);
            }
            finally
            {
                if (!skipEvents)
                {
                    _skipRaisingEvents = false;
                }
            }

            if (!skipEvents)
            {
                if (countBefore != Count)
                {
                    OnCountPropertyChanged();
                    OnIndexerPropertyChanged();
                    OnCollectionChanged(collectionChangedEventArgs);
                }
                else if (count > 0)
                {
                    // We replaced positive number of items with the same number of items, only the contents changed
                    OnIndexerPropertyChanged();
                    OnCollectionChanged(collectionChangedEventArgs);
                }
            }
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when an item is added to list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void InsertItem(int index, T item)
        {
            CheckReentrancy();
            base.InsertItem(index, item);

            if (!_skipRaisingEvents)
            {
                OnCountPropertyChanged();
                OnIndexerPropertyChanged();
                OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
            }
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when a collection of items is added to list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void InsertItemsRange(int index, IEnumerable<T> collection)
        {
            CheckReentrancy();

            int countBefore = default;
            bool skipEvents = _skipRaisingEvents;
            if (!skipEvents)
            {
                _skipRaisingEvents = true;
                countBefore = Count;
            }

            try
            {
                base.InsertItemsRange(index, collection);
            }
            finally
            {
                if (!skipEvents)
                {
                    _skipRaisingEvents = false;
                }
            }

            if (!_skipRaisingEvents)
            {
                NotifyCollectionChangedEventArgs collectionChangedEventArgs = EventArgsCache.ResetCollectionChanged;
                if (RaiseBatchCollectionChangedEvents && CollectionChanged is not null)
                {
                    IList newItems = collection as IList ?? new List<T>(collection);
                    collectionChangedEventArgs = new(NotifyCollectionChangedAction.Add, newItems, index);
                }

                if (countBefore != Count)
                {
                    OnCountPropertyChanged();
                    OnIndexerPropertyChanged();
                    OnCollectionChanged(collectionChangedEventArgs);
                }
            }
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when an item is set in list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void SetItem(int index, T item)
        {
            CheckReentrancy();
            T originalItem = this[index];
            base.SetItem(index, item);

            OnIndexerPropertyChanged();
            OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, item, index);
        }

        /// <summary>
        /// Called by base class ObservableCollection&lt;T&gt; when an item is to be moved within the list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            CheckReentrancy();

            T removedItem = this[oldIndex];

            base.RemoveItem(oldIndex);
            base.InsertItem(newIndex, removedItem);

            OnIndexerPropertyChanged();
            OnCollectionChanged(NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex);
        }

        /// <summary>
        /// Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        [field: NonSerialized]
        protected virtual event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raise CollectionChanged event to any listeners.
        /// Properties/methods modifying this ObservableCollection will raise
        /// a collection changed event through this virtual method.
        /// </summary>
        /// <remarks>
        /// When overriding this method, either call its base implementation
        /// or call <see cref="BlockReentrancy"/> to guard against reentrant collection changes.
        /// </remarks>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler? handler = CollectionChanged;
            if (handler != null)
            {
                // Not calling BlockReentrancy() here to avoid the SimpleMonitor allocation.
                _blockReentrancyCount++;
                try
                {
                    handler(this, e);
                }
                finally
                {
                    _blockReentrancyCount--;
                }
            }
        }

        /// <summary>
        /// Disallow reentrant attempts to change this collection. E.g. an event handler
        /// of the CollectionChanged event is not allowed to make changes to this collection.
        /// </summary>
        /// <remarks>
        /// typical usage is to wrap e.g. a OnCollectionChanged call with a using() scope:
        /// <code>
        ///         using (BlockReentrancy())
        ///         {
        ///             CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, item, index));
        ///         }
        /// </code>
        /// </remarks>
        protected IDisposable BlockReentrancy()
        {
            _blockReentrancyCount++;
            return EnsureMonitorInitialized();
        }

        /// <summary> Check and assert for reentrant attempts to change this collection. </summary>
        /// <exception cref="InvalidOperationException"> raised when changing the collection
        /// while another collection change is still being notified to other listeners </exception>
        protected void CheckReentrancy()
        {
            if (_blockReentrancyCount > 0)
            {
                // we can allow changes if there's only one listener - the problem
                // only arises if reentrant changes make the original event args
                // invalid for later listeners.  This keeps existing code working
                // (e.g. Selector.SelectedItems).
                NotifyCollectionChangedEventHandler? handler = CollectionChanged;
                if (handler != null && !handler.HasSingleTarget)
                    throw new InvalidOperationException(SR.ObservableCollectionReentrancyNotAllowed);
            }
        }

        /// <summary>
        /// Helper to raise a PropertyChanged event for the Count property
        /// </summary>
        private void OnCountPropertyChanged() => OnPropertyChanged(EventArgsCache.CountPropertyChanged);

        /// <summary>
        /// Helper to raise a PropertyChanged event for the Indexer property
        /// </summary>
        private void OnIndexerPropertyChanged() => OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);

        /// <summary>
        /// Helper to raise CollectionChanged event to any listeners
        /// </summary>
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }

        /// <summary>
        /// Helper to raise CollectionChanged event to any listeners
        /// </summary>
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item, int index, int oldIndex)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
        }

        /// <summary>
        /// Helper to raise CollectionChanged event to any listeners
        /// </summary>
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object? oldItem, object? newItem, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
        }

        /// <summary>
        /// Helper to raise CollectionChanged event with action == Reset to any listeners
        /// </summary>
        private void OnCollectionReset() => OnCollectionChanged(EventArgsCache.ResetCollectionChanged);

        private SimpleMonitor EnsureMonitorInitialized() => _monitor ??= new SimpleMonitor(this);

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            EnsureMonitorInitialized();
            _monitor!._busyCount = _blockReentrancyCount;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_monitor != null)
            {
                _blockReentrancyCount = _monitor._busyCount;
                _monitor._collection = this;
            }
        }

        // this class helps prevent reentrant calls
        [Serializable]
        [TypeForwardedFrom("WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
        private sealed class SimpleMonitor : IDisposable
        {
            internal int _busyCount; // Only used during (de)serialization to maintain compatibility with desktop. Do not rename (binary serialization)

            [NonSerialized]
            internal ObservableCollection<T> _collection;

            public SimpleMonitor(ObservableCollection<T> collection)
            {
                Debug.Assert(collection != null);
                _collection = collection;
            }

            public void Dispose() => _collection._blockReentrancyCount--;
        }
    }

    internal static class EventArgsCache
    {
        internal static readonly PropertyChangedEventArgs CountPropertyChanged = new PropertyChangedEventArgs("Count");
        internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new PropertyChangedEventArgs("Item[]");
        internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
    }
}
