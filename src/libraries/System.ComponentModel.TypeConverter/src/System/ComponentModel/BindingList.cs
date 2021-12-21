// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.ComponentModel
{
    [Serializable]
    [TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class BindingList<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> :
        Collection<T>, IBindingList, ICancelAddNew, IRaiseItemChangedEvents
    {
        private int addNewPos = -1; // Do not rename (binary serialization)
        private bool raiseListChangedEvents = true; // Do not rename (binary serialization)
        private bool raiseItemChangedEvents; // Do not rename (binary serialization)

        [NonSerialized]
        private PropertyDescriptorCollection? _itemTypeProperties;

        [NonSerialized]
        private PropertyChangedEventHandler? _propertyChangedEventHandler;

        [NonSerialized]
        private AddingNewEventHandler? _onAddingNew;

        [NonSerialized]
        private ListChangedEventHandler? _onListChanged;

        [NonSerialized]
        private int _lastChangeIndex = -1;

        private bool allowNew = true; // Do not rename (binary serialization)
        private bool allowEdit = true; // Do not rename (binary serialization)
        private bool allowRemove = true; // Do not rename (binary serialization)
        private bool userSetAllowNew; // Do not rename (binary serialization)

        #region Constructors

        [RequiresUnreferencedCode("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
        public BindingList() => Initialize();

        /// <summary>
        /// Constructor that allows substitution of the inner list with a custom list.
        /// </summary>
        [RequiresUnreferencedCode("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
        public BindingList(IList<T> list) : base(list)
        {
            Initialize();
        }

        [RequiresUnreferencedCode("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
        private void Initialize()
        {
            // Set the default value of AllowNew based on whether type T has a default constructor
            allowNew = ItemTypeHasDefaultConstructor;

            // Check for INotifyPropertyChanged
            if (typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T)))
            {
                // Supports INotifyPropertyChanged
                raiseItemChangedEvents = true;

                // Loop thru the items already in the collection and hook their change notification.
                foreach (T item in Items)
                {
                    HookPropertyChanged(item);
                }
            }
        }

        private static bool ItemTypeHasDefaultConstructor
        {
            get
            {
                Type itemType = typeof(T);

                if (itemType.IsPrimitive)
                {
                    return true;
                }

                const BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance;
                return itemType.GetConstructor(BindingFlags, null, Type.EmptyTypes, null) != null;
            }
        }

        #endregion

        #region AddingNew event

        /// <summary>
        /// Event that allows a custom item to be provided as the new item added to the list by AddNew().
        /// </summary>
        public event AddingNewEventHandler AddingNew
        {
            add
            {
                bool allowNewWasTrue = AllowNew;
                _onAddingNew += value;
                if (allowNewWasTrue != AllowNew)
                {
                    FireListChanged(ListChangedType.Reset, -1);
                }
            }
            remove
            {
                bool allowNewWasTrue = AllowNew;
                _onAddingNew -= value;
                if (allowNewWasTrue != AllowNew)
                {
                    FireListChanged(ListChangedType.Reset, -1);
                }
            }
        }

        /// <summary>
        /// Raises the AddingNew event.
        /// </summary>
        protected virtual void OnAddingNew(AddingNewEventArgs e) => _onAddingNew?.Invoke(this, e);

        // Private helper method
        private object? FireAddingNew()
        {
            AddingNewEventArgs e = new AddingNewEventArgs(null);
            OnAddingNew(e);
            return e.NewObject;
        }

        #endregion

        #region ListChanged event

        /// <summary>
        /// Event that reports changes to the list or to items in the list.
        /// </summary>
        public event ListChangedEventHandler ListChanged
        {
            add => _onListChanged += value;
            remove => _onListChanged -= value;
        }

        /// <summary>
        /// Raises the ListChanged event.
        /// </summary>
        protected virtual void OnListChanged(ListChangedEventArgs e) => _onListChanged?.Invoke(this, e);

        public bool RaiseListChangedEvents
        {
            get => raiseListChangedEvents;
            set => raiseListChangedEvents = value;
        }

        public void ResetBindings() => FireListChanged(ListChangedType.Reset, -1);

        public void ResetItem(int position)
        {
            FireListChanged(ListChangedType.ItemChanged, position);
        }

        // Private helper method
        private void FireListChanged(ListChangedType type, int index)
        {
            if (raiseListChangedEvents)
            {
                OnListChanged(new ListChangedEventArgs(type, index));
            }
        }

        #endregion

        #region Collection<T> overrides

        // Collection<T> funnels all list changes through the four virtual methods below.
        // We override these so that we can commit any pending new item and fire the proper ListChanged events.

        protected override void ClearItems()
        {
            EndNew(addNewPos);

            if (raiseItemChangedEvents)
            {
                foreach (T item in Items)
                {
                    UnhookPropertyChanged(item);
                }
            }

            base.ClearItems();
            FireListChanged(ListChangedType.Reset, -1);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "BindingList ctor is marked with RequiresUnreferencedCode.")]
        protected override void InsertItem(int index, T item)
        {
            EndNew(addNewPos);
            base.InsertItem(index, item);

            if (raiseItemChangedEvents)
            {
                HookPropertyChanged(item);
            }

            FireListChanged(ListChangedType.ItemAdded, index);
        }

        protected override void RemoveItem(int index)
        {
            // Need to all RemoveItem if this on the AddNew item
            if (!allowRemove && !(addNewPos >= 0 && addNewPos == index))
            {
                throw new NotSupportedException();
            }

            EndNew(addNewPos);

            if (raiseItemChangedEvents)
            {
                UnhookPropertyChanged(this[index]);
            }

            base.RemoveItem(index);
            FireListChanged(ListChangedType.ItemDeleted, index);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "BindingList ctor is marked with RequiresUnreferencedCode.")]
        protected override void SetItem(int index, T item)
        {
            if (raiseItemChangedEvents)
            {
                UnhookPropertyChanged(this[index]);
            }

            base.SetItem(index, item);

            if (raiseItemChangedEvents)
            {
                HookPropertyChanged(item);
            }

            FireListChanged(ListChangedType.ItemChanged, index);
        }

        #endregion

        #region ICancelAddNew interface

        /// <summary>
        /// If item added using AddNew() is still cancellable, then remove that item from the list.
        /// </summary>
        public virtual void CancelNew(int itemIndex)
        {
            if (addNewPos >= 0 && addNewPos == itemIndex)
            {
                RemoveItem(addNewPos);
                addNewPos = -1;
            }
        }

        /// <summary>
        /// If item added using AddNew() is still cancellable, then commit that item.
        /// </summary>
        public virtual void EndNew(int itemIndex)
        {
            if (addNewPos >= 0 && addNewPos == itemIndex)
            {
                addNewPos = -1;
            }
        }

        #endregion

        #region IBindingList interface

        /// <summary>
        /// Adds a new item to the list. Calls <see cref="AddNewCore" /> to create and add the item.
        ///
        /// Add operations are cancellable via the <see cref="ICancelAddNew" /> interface. The position of the
        /// new item is tracked until the add operation is either cancelled by a call to <see cref="CancelNew" />,
        /// explicitly commited by a call to <see cref="EndNew" />, or implicitly commmited some other operation
        /// changes the contents of the list (such as an Insert or Remove). When an add operation is
        /// cancelled, the new item is removed from the list.
        /// </summary>
        public T AddNew() => (T)((this as IBindingList).AddNew())!;

        object? IBindingList.AddNew()
        {
            // Create new item and add it to list
            object? newItem = AddNewCore();

            // Record position of new item (to support cancellation later on)
            addNewPos = (newItem != null) ? IndexOf((T)newItem) : -1;

            // Return new item to caller
            return newItem;
        }

        private bool AddingNewHandled => _onAddingNew != null && _onAddingNew.GetInvocationList().Length > 0;

        /// <summary>
        /// Creates a new item and adds it to the list.
        ///
        /// The base implementation raises the AddingNew event to allow an event handler to
        /// supply a custom item to add to the list. Otherwise an item of type T is created.
        /// The new item is then added to the end of the list.
        /// </summary>
        protected virtual object? AddNewCore()
        {
            // Allow event handler to supply the new item for us
            object? newItem = FireAddingNew();

            // If event hander did not supply new item, create one ourselves
            if (newItem == null)
            {
                newItem = Activator.CreateInstance(typeof(T));
            }

            // Add item to end of list. Note: If event handler returned an item not of type T,
            // the cast below will trigger an InvalidCastException. This is by design.
            Add((T)newItem!);

            // Return new item to caller
            return newItem;
        }

        public bool AllowNew
        {
            get
            {
                // If the user set AllowNew, return what they set. If we have a default constructor, allowNew will be
                // true and we should just return true.
                if (userSetAllowNew || allowNew)
                {
                    return allowNew;
                }
                // Even if the item doesn't have a default constructor, the user can hook AddingNew to provide an item.
                // If there's a handler for this, we should allow new.
                return AddingNewHandled;
            }
            set
            {
                bool oldAllowNewValue = AllowNew;
                userSetAllowNew = true;
                // Note that we don't want to set allowNew only if AllowNew didn't match value,
                // since AllowNew can depend on onAddingNew handler
                allowNew = value;
                if (oldAllowNewValue != value)
                {
                    FireListChanged(ListChangedType.Reset, -1);
                }
            }
        }

        bool IBindingList.AllowNew => AllowNew;

        public bool AllowEdit
        {
            get => allowEdit;
            set
            {
                if (allowEdit != value)
                {
                    allowEdit = value;
                    FireListChanged(ListChangedType.Reset, -1);
                }
            }
        }

        bool IBindingList.AllowEdit => AllowEdit;

        public bool AllowRemove
        {
            get => allowRemove;
            set
            {
                if (allowRemove != value)
                {
                    allowRemove = value;
                    FireListChanged(ListChangedType.Reset, -1);
                }
            }
        }

        bool IBindingList.AllowRemove => AllowRemove;

        bool IBindingList.SupportsChangeNotification => SupportsChangeNotificationCore;

        protected virtual bool SupportsChangeNotificationCore => true;

        bool IBindingList.SupportsSearching => SupportsSearchingCore;

        protected virtual bool SupportsSearchingCore => false;

        bool IBindingList.SupportsSorting => SupportsSortingCore;

        protected virtual bool SupportsSortingCore => false;

        bool IBindingList.IsSorted => IsSortedCore;

        protected virtual bool IsSortedCore => false;

        PropertyDescriptor? IBindingList.SortProperty => SortPropertyCore;

        protected virtual PropertyDescriptor? SortPropertyCore => null;

        ListSortDirection IBindingList.SortDirection => SortDirectionCore;

        protected virtual ListSortDirection SortDirectionCore => ListSortDirection.Ascending;

        void IBindingList.ApplySort(PropertyDescriptor prop, ListSortDirection direction)
        {
            ApplySortCore(prop, direction);
        }

        protected virtual void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            throw new NotSupportedException();
        }

        void IBindingList.RemoveSort() => RemoveSortCore();

        protected virtual void RemoveSortCore()
        {
            throw new NotSupportedException();
        }

        int IBindingList.Find(PropertyDescriptor prop, object key) => FindCore(prop, key);

        protected virtual int FindCore(PropertyDescriptor prop, object key)
        {
            throw new NotSupportedException();
        }

        void IBindingList.AddIndex(PropertyDescriptor prop)
        {
            // Not supported
        }

        void IBindingList.RemoveIndex(PropertyDescriptor prop)
        {
            // Not supported
        }

        #endregion

        #region Property Change Support

        [RequiresUnreferencedCode("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
        private void HookPropertyChanged(T item)
        {
            // Note: inpc may be null if item is null, so always check.
            if (item is INotifyPropertyChanged inpc)
            {
                if (_propertyChangedEventHandler == null)
                {
                    _propertyChangedEventHandler = new PropertyChangedEventHandler(Child_PropertyChanged);
                }
                inpc.PropertyChanged += _propertyChangedEventHandler;
            }
        }

        private void UnhookPropertyChanged(T item)
        {
            // Note: inpc may be null if item is null, so always check.
            if (item is INotifyPropertyChanged inpc && _propertyChangedEventHandler != null)
            {
                inpc.PropertyChanged -= _propertyChangedEventHandler;
            }
        }

        [RequiresUnreferencedCode("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
        private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs? e)
        {
            if (RaiseListChangedEvents)
            {
                if (sender == null || e == null || string.IsNullOrEmpty(e.PropertyName))
                {
                    // Fire reset event (per INotifyPropertyChanged spec)
                    ResetBindings();
                }
                else
                {
                    // The change event is broken should someone pass an item to us that is not
                    // of type T. Still, if they do so, detect it and ignore. It is an incorrect
                    // and rare enough occurrence that we do not want to slow the mainline path
                    // with "is" checks.
                    T item;

                    try
                    {
                        item = (T)sender;
                    }
                    catch (InvalidCastException)
                    {
                        ResetBindings();
                        return;
                    }

                    // Find the position of the item. This should never be -1. If it is,
                    // somehow the item has been removed from our list without our knowledge.
                    int pos = _lastChangeIndex;

                    if (pos < 0 || pos >= Count || !this[pos]!.Equals(item))
                    {
                        pos = IndexOf(item);
                        _lastChangeIndex = pos;
                    }

                    if (pos == -1)
                    {
                        // The item was removed from the list but we still get change notifications or
                        // the sender is invalid and was never added to the list.
                        UnhookPropertyChanged(item);
                        ResetBindings();
                    }
                    else
                    {
                        // Get the property descriptor
                        if (null == _itemTypeProperties)
                        {
                            // Get Shape
                            _itemTypeProperties = TypeDescriptor.GetProperties(typeof(T));
                            Debug.Assert(_itemTypeProperties != null);
                        }

                        PropertyDescriptor? pd = _itemTypeProperties.Find(e.PropertyName, true);

                        // Create event args. If there was no matching property descriptor,
                        // we raise the list changed anyway.
                        ListChangedEventArgs args = new ListChangedEventArgs(ListChangedType.ItemChanged, pos, pd);

                        // Fire the ItemChanged event
                        OnListChanged(args);
                    }
                }
            }
        }

        #endregion

        #region IRaiseItemChangedEvents interface

        /// <summary>
        /// Returns false to indicate that BindingList&lt;T&gt; does NOT raise ListChanged events
        /// of type ItemChanged as a result of property changes on individual list items
        /// unless those items support INotifyPropertyChanged.
        /// </summary>
        bool IRaiseItemChangedEvents.RaisesItemChangedEvents => raiseItemChangedEvents;

        #endregion
    }
}
