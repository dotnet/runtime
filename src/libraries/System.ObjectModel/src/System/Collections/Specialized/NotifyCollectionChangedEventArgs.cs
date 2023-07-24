// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Collections.Specialized
{
    /// <summary>
    /// Arguments for the CollectionChanged event.
    /// A collection that supports INotifyCollectionChangedThis raises this event
    /// whenever an item is added or removed, or when the contents of the collection
    /// changes dramatically.
    /// </summary>
    public class NotifyCollectionChangedEventArgs : EventArgs
    {
        private readonly NotifyCollectionChangedAction _action;
        private readonly IList? _newItems;
        private readonly IList? _oldItems;
        private readonly int _newStartingIndex = -1;
        private readonly int _oldStartingIndex = -1;

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a reset change.
        /// </summary>
        /// <param name="action">The action that caused the event (must be Reset).</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action)
        {
            if (action != NotifyCollectionChangedAction.Reset)
            {
                throw new ArgumentException(SR.Format(SR.WrongActionForCtor, NotifyCollectionChangedAction.Reset), nameof(action));
            }

            _action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item change.
        /// </summary>
        /// <param name="action">The action that caused the event; can only be Reset, Add or Remove action.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem) :
            this(action, changedItem, -1)
        {
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item change.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        /// <param name="index">The index where the change occurred.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem, int index)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (changedItem != null)
                    {
                        throw new ArgumentException(SR.ResetActionRequiresNullItem, nameof(action));
                    }
                    if (index != -1)
                    {
                        throw new ArgumentException(SR.ResetActionRequiresIndexMinus1, nameof(action));
                    }
                    break;

                case NotifyCollectionChangedAction.Add:
                    _newItems = new SingleItemReadOnlyList(changedItem);
                    _newStartingIndex = index;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    _oldItems = new SingleItemReadOnlyList(changedItem);
                    _oldStartingIndex = index;
                    break;

                default:
                    throw new ArgumentException(SR.MustBeResetAddOrRemoveActionForCtor, nameof(action));
            }

            _action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item change.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList? changedItems) :
            this(action, changedItems, -1)
        {
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item change (or a reset).
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        /// <param name="startingIndex">The index where the change occurred.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList? changedItems, int startingIndex)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (changedItems != null)
                    {
                        throw new ArgumentException(SR.ResetActionRequiresNullItem, nameof(action));
                    }
                    if (startingIndex != -1)
                    {
                        throw new ArgumentException(SR.ResetActionRequiresIndexMinus1, nameof(action));
                    }
                    break;

                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    ArgumentNullException.ThrowIfNull(changedItems);
                    ArgumentOutOfRangeException.ThrowIfLessThan(startingIndex, -1);

                    if (action == NotifyCollectionChangedAction.Add)
                    {
                        _newItems = new ReadOnlyList(changedItems);
                        _newStartingIndex = startingIndex;
                    }
                    else
                    {
                        _oldItems = new ReadOnlyList(changedItems);
                        _oldStartingIndex = startingIndex;
                    }
                    break;

                default:
                    throw new ArgumentException(SR.MustBeResetAddOrRemoveActionForCtor, nameof(action));
            }

            _action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItem">The new item replacing the original item.</param>
        /// <param name="oldItem">The original item that is replaced.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? newItem, object? oldItem) :
            this(action, newItem, oldItem, -1)
        {
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItem">The new item replacing the original item.</param>
        /// <param name="oldItem">The original item that is replaced.</param>
        /// <param name="index">The index of the item being replaced.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? newItem, object? oldItem, int index)
        {
            if (action != NotifyCollectionChangedAction.Replace)
            {
                throw new ArgumentException(SR.Format(SR.WrongActionForCtor, NotifyCollectionChangedAction.Replace), nameof(action));
            }

            _action = action;
            _newItems = new SingleItemReadOnlyList(newItem);
            _oldItems = new SingleItemReadOnlyList(oldItem);
            _newStartingIndex = _oldStartingIndex = index;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItems">The new items replacing the original items.</param>
        /// <param name="oldItems">The original items that are replaced.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems) :
            this(action, newItems, oldItems, -1)
        {
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItems">The new items replacing the original items.</param>
        /// <param name="oldItems">The original items that are replaced.</param>
        /// <param name="startingIndex">The starting index of the items being replaced.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems, int startingIndex)
        {
            if (action != NotifyCollectionChangedAction.Replace)
            {
                throw new ArgumentException(SR.Format(SR.WrongActionForCtor, NotifyCollectionChangedAction.Replace), nameof(action));
            }
            ArgumentNullException.ThrowIfNull(newItems);
            ArgumentNullException.ThrowIfNull(oldItems);

            _action = action;
            _newItems = new ReadOnlyList(newItems);
            _oldItems = new ReadOnlyList(oldItems);
            _newStartingIndex = _oldStartingIndex = startingIndex;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Move event.
        /// </summary>
        /// <param name="action">Can only be a Move action.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        /// <param name="index">The new index for the changed item.</param>
        /// <param name="oldIndex">The old index for the changed item.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem, int index, int oldIndex)
        {
            if (action != NotifyCollectionChangedAction.Move)
            {
                throw new ArgumentException(SR.Format(SR.WrongActionForCtor, NotifyCollectionChangedAction.Move), nameof(action));
            }
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            _action = action;
            _newItems = _oldItems = new SingleItemReadOnlyList(changedItem);
            _newStartingIndex = index;
            _oldStartingIndex = oldIndex;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Move event.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        /// <param name="index">The new index for the changed items.</param>
        /// <param name="oldIndex">The old index for the changed items.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList? changedItems, int index, int oldIndex)
        {
            if (action != NotifyCollectionChangedAction.Move)
            {
                throw new ArgumentException(SR.Format(SR.WrongActionForCtor, NotifyCollectionChangedAction.Move), nameof(action));
            }
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            _action = action;
            _newItems = _oldItems = changedItems is not null ? new ReadOnlyList(changedItems) : null;
            _newStartingIndex = index;
            _oldStartingIndex = oldIndex;
        }

        /// <summary>
        /// The action that caused the event.
        /// </summary>
        public NotifyCollectionChangedAction Action => _action;

        /// <summary>
        /// The items affected by the change.
        /// </summary>
        public IList? NewItems => _newItems;

        /// <summary>
        /// The old items affected by the change (for Replace events).
        /// </summary>
        public IList? OldItems => _oldItems;

        /// <summary>
        /// The index where the change occurred.
        /// </summary>
        public int NewStartingIndex => _newStartingIndex;

        /// <summary>
        /// The old index where the change occurred (for Move events).
        /// </summary>
        public int OldStartingIndex => _oldStartingIndex;
    }

    /// <summary>
    /// The delegate to use for handlers that receive the CollectionChanged event.
    /// </summary>
    public delegate void NotifyCollectionChangedEventHandler(object? sender, NotifyCollectionChangedEventArgs e);

    internal sealed class ReadOnlyList : IList
    {
        private readonly IList _list;

        internal ReadOnlyList(IList list)
        {
            Debug.Assert(list != null);
            _list = list;
        }

        public int Count => _list.Count;

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public bool IsSynchronized => _list.IsSynchronized;

        public object? this[int index]
        {
            get => _list[index];
            set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        public object SyncRoot => _list.SyncRoot;

        public int Add(object? value) => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        public void Clear() => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        public bool Contains(object? value) => _list.Contains(value);

        public void CopyTo(Array array, int index) => _list.CopyTo(array, index);

        public IEnumerator GetEnumerator() => _list.GetEnumerator();

        public int IndexOf(object? value) => _list.IndexOf(value);

        public void Insert(int index, object? value) => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        public void Remove(object? value) => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        public void RemoveAt(int index) => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
    }

    internal sealed class SingleItemReadOnlyList : IList
    {
        private readonly object? _item;

        public SingleItemReadOnlyList(object? item) => _item = item;

        public object? this[int index]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
                return _item;
            }
            set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        public bool IsFixedSize => true;

        public bool IsReadOnly => true;

        public int Count => 1;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public IEnumerator GetEnumerator()
        {
            yield return _item;
        }

        public bool Contains(object? value) => _item is null ? value is null : _item.Equals(value);

        public int IndexOf(object? value) => Contains(value) ? 0 : -1;

        public void CopyTo(Array array, int index)
        {
            CollectionHelpers.ValidateCopyToArguments(1, array, index);
            array.SetValue(_item, index);
        }

        public int Add(object? value) => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        public void Clear() => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        public void Insert(int index, object? value) => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        public void Remove(object? value) => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        public void RemoveAt(int index) => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
    }
}
