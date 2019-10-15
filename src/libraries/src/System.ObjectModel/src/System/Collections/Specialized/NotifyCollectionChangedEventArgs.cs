// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        private NotifyCollectionChangedAction _action;
        private IList _newItems;
        private IList _oldItems;
        private int _newStartingIndex = -1;
        private int _oldStartingIndex = -1;

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

            InitializeAdd(action, null, -1);
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item change.
        /// </summary>
        /// <param name="action">The action that caused the event; can only be Reset, Add or Remove action.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem)
        {
            if ((action != NotifyCollectionChangedAction.Add) && (action != NotifyCollectionChangedAction.Remove)
                    && (action != NotifyCollectionChangedAction.Reset))
            {
                throw new ArgumentException(SR.MustBeResetAddOrRemoveActionForCtor, nameof(action));
            }

            if (action == NotifyCollectionChangedAction.Reset)
            {
                if (changedItem != null)
                {
                    throw new ArgumentException(SR.ResetActionRequiresNullItem, nameof(action));
                }

                InitializeAdd(action, null, -1);
            }
            else
            {
                InitializeAddOrRemove(action, new object[] { changedItem }, -1);
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item change.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        /// <param name="index">The index where the change occurred.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem, int index)
        {
            if ((action != NotifyCollectionChangedAction.Add) && (action != NotifyCollectionChangedAction.Remove)
                    && (action != NotifyCollectionChangedAction.Reset))
            {
                throw new ArgumentException(SR.MustBeResetAddOrRemoveActionForCtor, nameof(action));
            }

            if (action == NotifyCollectionChangedAction.Reset)
            {
                if (changedItem != null)
                {
                    throw new ArgumentException(SR.ResetActionRequiresNullItem, nameof(action));
                }
                if (index != -1)
                {
                    throw new ArgumentException(SR.ResetActionRequiresIndexMinus1, nameof(action));
                }

                InitializeAdd(action, null, -1);
            }
            else
            {
                InitializeAddOrRemove(action, new object[] { changedItem }, index);
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item change.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems)
        {
            if ((action != NotifyCollectionChangedAction.Add) && (action != NotifyCollectionChangedAction.Remove)
                    && (action != NotifyCollectionChangedAction.Reset))
            {
                throw new ArgumentException(SR.MustBeResetAddOrRemoveActionForCtor, nameof(action));
            }

            if (action == NotifyCollectionChangedAction.Reset)
            {
                if (changedItems != null)
                {
                    throw new ArgumentException(SR.ResetActionRequiresNullItem, nameof(action));
                }

                InitializeAdd(action, null, -1);
            }
            else
            {
                if (changedItems == null)
                {
                    throw new ArgumentNullException(nameof(changedItems));
                }

                InitializeAddOrRemove(action, changedItems, -1);
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item change (or a reset).
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        /// <param name="startingIndex">The index where the change occurred.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems, int startingIndex)
        {
            if ((action != NotifyCollectionChangedAction.Add) && (action != NotifyCollectionChangedAction.Remove)
                    && (action != NotifyCollectionChangedAction.Reset))
            {
                throw new ArgumentException(SR.MustBeResetAddOrRemoveActionForCtor, nameof(action));
            }

            if (action == NotifyCollectionChangedAction.Reset)
            {
                if (changedItems != null)
                {
                    throw new ArgumentException(SR.ResetActionRequiresNullItem, nameof(action));
                }
                if (startingIndex != -1)
                {
                    throw new ArgumentException(SR.ResetActionRequiresIndexMinus1, nameof(action));
                }

                InitializeAdd(action, null, -1);
            }
            else
            {
                if (changedItems == null)
                {
                    throw new ArgumentNullException(nameof(changedItems));
                }
                if (startingIndex < -1)
                {
                    throw new ArgumentException(SR.IndexCannotBeNegative, nameof(startingIndex));
                }

                InitializeAddOrRemove(action, changedItems, startingIndex);
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItem">The new item replacing the original item.</param>
        /// <param name="oldItem">The original item that is replaced.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object newItem, object oldItem)
        {
            if (action != NotifyCollectionChangedAction.Replace)
            {
                throw new ArgumentException(SR.Format(SR.WrongActionForCtor, NotifyCollectionChangedAction.Replace), nameof(action));
            }

            InitializeMoveOrReplace(action, new object[] { newItem }, new object[] { oldItem }, -1, -1);
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItem">The new item replacing the original item.</param>
        /// <param name="oldItem">The original item that is replaced.</param>
        /// <param name="index">The index of the item being replaced.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object newItem, object oldItem, int index)
        {
            if (action != NotifyCollectionChangedAction.Replace)
            {
                throw new ArgumentException(SR.Format(SR.WrongActionForCtor, NotifyCollectionChangedAction.Replace), nameof(action));
            }

            InitializeMoveOrReplace(action, new object[] { newItem }, new object[] { oldItem }, index, index);
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItems">The new items replacing the original items.</param>
        /// <param name="oldItems">The original items that are replaced.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems)
        {
            if (action != NotifyCollectionChangedAction.Replace)
            {
                throw new ArgumentException(SR.Format(SR.WrongActionForCtor, NotifyCollectionChangedAction.Replace), nameof(action));
            }
            if (newItems == null)
            {
                throw new ArgumentNullException(nameof(newItems));
            }
            if (oldItems == null)
            {
                throw new ArgumentNullException(nameof(oldItems));
            }

            InitializeMoveOrReplace(action, newItems, oldItems, -1, -1);
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
            if (newItems == null)
            {
                throw new ArgumentNullException(nameof(newItems));
            }
            if (oldItems == null)
            {
                throw new ArgumentNullException(nameof(oldItems));
            }

            InitializeMoveOrReplace(action, newItems, oldItems, startingIndex, startingIndex);
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Move event.
        /// </summary>
        /// <param name="action">Can only be a Move action.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        /// <param name="index">The new index for the changed item.</param>
        /// <param name="oldIndex">The old index for the changed item.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem, int index, int oldIndex)
        {
            if (action != NotifyCollectionChangedAction.Move)
            {
                throw new ArgumentException(SR.Format(SR.WrongActionForCtor, NotifyCollectionChangedAction.Move), nameof(action));
            }
            if (index < 0)
            {
                throw new ArgumentException(SR.IndexCannotBeNegative, nameof(index));
            }

            object[] changedItems = new object[] { changedItem };
            InitializeMoveOrReplace(action, changedItems, changedItems, index, oldIndex);
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Move event.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        /// <param name="index">The new index for the changed items.</param>
        /// <param name="oldIndex">The old index for the changed items.</param>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems, int index, int oldIndex)
        {
            if (action != NotifyCollectionChangedAction.Move)
            {
                throw new ArgumentException(SR.Format(SR.WrongActionForCtor, NotifyCollectionChangedAction.Move), nameof(action));
            }
            if (index < 0)
            {
                throw new ArgumentException(SR.IndexCannotBeNegative, nameof(index));
            }

            InitializeMoveOrReplace(action, changedItems, changedItems, index, oldIndex);
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs with given fields (no validation). Used by WinRT marshaling.
        /// </summary>
        internal NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems, int newIndex, int oldIndex)
        {
            _action = action;
            _newItems = (newItems == null) ? null : new ReadOnlyList(newItems);
            _oldItems = (oldItems == null) ? null : new ReadOnlyList(oldItems);
            _newStartingIndex = newIndex;
            _oldStartingIndex = oldIndex;
        }

        private void InitializeAddOrRemove(NotifyCollectionChangedAction action, IList changedItems, int startingIndex)
        {
            if (action == NotifyCollectionChangedAction.Add)
            {
                InitializeAdd(action, changedItems, startingIndex);
            }
            else
            {
                Debug.Assert(action == NotifyCollectionChangedAction.Remove, $"Unsupported action: {action}");
                InitializeRemove(action, changedItems, startingIndex);
            }
        }

        private void InitializeAdd(NotifyCollectionChangedAction action, IList newItems, int newStartingIndex)
        {
            _action = action;
            _newItems = (newItems == null) ? null : new ReadOnlyList(newItems);
            _newStartingIndex = newStartingIndex;
        }

        private void InitializeRemove(NotifyCollectionChangedAction action, IList oldItems, int oldStartingIndex)
        {
            _action = action;
            _oldItems = (oldItems == null) ? null : new ReadOnlyList(oldItems);
            _oldStartingIndex = oldStartingIndex;
        }

        private void InitializeMoveOrReplace(NotifyCollectionChangedAction action, IList newItems, IList oldItems, int startingIndex, int oldStartingIndex)
        {
            InitializeAdd(action, newItems, startingIndex);
            InitializeRemove(action, oldItems, oldStartingIndex);
        }

        /// <summary>
        /// The action that caused the event.
        /// </summary>
        public NotifyCollectionChangedAction Action => _action;

        /// <summary>
        /// The items affected by the change.
        /// </summary>
        public IList NewItems => _newItems;

        /// <summary>
        /// The old items affected by the change (for Replace events).
        /// </summary>
        public IList OldItems => _oldItems;

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
    public delegate void NotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e);

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

        public object this[int index]
        {
            get => _list[index];
            set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        public object SyncRoot => _list.SyncRoot;

        public int Add(object value)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        public void Clear()
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        public bool Contains(object value) => _list.Contains(value);

        public void CopyTo(Array array, int index)
        {
            _list.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator() => _list.GetEnumerator();

        public int IndexOf(object value) => _list.IndexOf(value);

        public void Insert(int index, object value)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        public void Remove(object value)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }
    }
}
