// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Xml;

namespace System.Configuration
{
    [DebuggerDisplay("Count = {Count}")]
    public abstract class ConfigurationElementCollection : ConfigurationElement, ICollection
    {
        internal const string DefaultAddItemName = "add";
        internal const string DefaultRemoveItemName = "remove";
        internal const string DefaultClearItemsName = "clear";
        private readonly IComparer _comparer;
        private string _addElement = DefaultAddItemName;
        private string _clearElement = DefaultClearItemsName;
        private bool _collectionCleared;
        private bool _emitClearTag;
        private int _inheritedCount; // Total number of inherited items
        private bool _modified;
        private bool _readOnly;

        private int _removedItemCount; // Number of items removed for this collection (not including parent)
        private string _removeElement = DefaultRemoveItemName;
        internal bool InternalAddToEnd = false;
        internal string InternalElementTagName = string.Empty;

        protected ConfigurationElementCollection() { }

        protected ConfigurationElementCollection(IComparer comparer)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            _comparer = comparer;
        }

        private ArrayList Items { get; } = new ArrayList();

        protected internal string AddElementName
        {
            get { return _addElement; }
            set
            {
                _addElement = value;
                if (BaseConfigurationRecord.IsReservedAttributeName(value))
                    throw new ArgumentException(string.Format(SR.Item_name_reserved, DefaultAddItemName, value));
            }
        }

        protected internal string RemoveElementName
        {
            get { return _removeElement; }
            set
            {
                if (BaseConfigurationRecord.IsReservedAttributeName(value))
                    throw new ArgumentException(string.Format(SR.Item_name_reserved, DefaultRemoveItemName, value));
                _removeElement = value;
            }
        }

        protected internal string ClearElementName
        {
            get { return _clearElement; }
            set
            {
                if (BaseConfigurationRecord.IsReservedAttributeName(value))
                    throw new ArgumentException(string.Format(SR.Item_name_reserved, DefaultClearItemsName, value));
                _clearElement = value;
            }
        }

        public bool EmitClear
        {
            get { return _emitClearTag; }
            set
            {
                if (IsReadOnly()) throw new ConfigurationErrorsException(SR.Config_base_read_only);
                if (value)
                {
                    CheckLockedElement(_clearElement, null); // has clear been locked?
                    CheckLockedElement(_removeElement, null); // has remove been locked? Clear implies remove
                }
                _modified = true;
                _emitClearTag = value;
            }
        }

        protected virtual string ElementName => "";

        internal string LockableElements
        {
            get
            {
                if ((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMap) ||
                    (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
                {
                    string elementNames = "'" + AddElementName + "'"; // Must have an add
                    if (RemoveElementName.Length != 0)
                        elementNames += ", '" + RemoveElementName + "'";
                    if (ClearElementName.Length != 0)
                        elementNames += ", '" + ClearElementName + "'";
                    return elementNames;
                }

                if (!string.IsNullOrEmpty(ElementName)) return "'" + ElementName + "'";
                return string.Empty;
            }
        }

        protected virtual bool ThrowOnDuplicate
            => (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMap) ||
            (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate);

        public virtual ConfigurationElementCollectionType CollectionType
            => ConfigurationElementCollectionType.AddRemoveClearMap;

        public int Count => Items.Count - _removedItemCount;

        public bool IsSynchronized => false;

        public object SyncRoot => null;

        void ICollection.CopyTo(Array arr, int index)
        {
            foreach (Entry entry in Items)
                if (entry.EntryType != EntryType.Removed) arr.SetValue(entry.Value, index++);
        }

        public IEnumerator GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        internal override void AssociateContext(BaseConfigurationRecord configRecord)
        {
            base.AssociateContext(configRecord);

            foreach (Entry entry in Items)
                entry.Value?.AssociateContext(configRecord);
        }

        protected internal override bool IsModified()
        {
            if (_modified) return true;

            if (base.IsModified()) return true;

            foreach (Entry entry in Items)
            {
                if (entry.EntryType == EntryType.Removed) continue;

                ConfigurationElement elem = entry.Value;
                if (elem.IsModified()) return true;
            }

            return false;
        }

        protected internal override void ResetModified()
        {
            _modified = false;
            base.ResetModified();

            foreach (Entry entry in Items)
            {
                if (entry.EntryType == EntryType.Removed) continue;

                ConfigurationElement elem = entry.Value;
                elem.ResetModified();
            }
        }

        public override bool IsReadOnly()
        {
            return _readOnly;
        }

        protected internal override void SetReadOnly()
        {
            _readOnly = true;
            foreach (Entry entry in Items)
            {
                if (entry.EntryType == EntryType.Removed) continue;

                ConfigurationElement elem = entry.Value;
                elem.SetReadOnly();
            }
        }

        internal virtual IEnumerator GetEnumeratorImpl()
        {
            return new Enumerator(Items, this);
        }

        internal IEnumerator GetElementsEnumerator()
        {
            // Return an enumerator over the collection's config elements.
            // This is different then the std GetEnumerator because the second one
            // can return different set of items if overriden in a derived class

            return new Enumerator(Items, this);
        }

        public override bool Equals(object compareTo)
        {
            if (compareTo == null || compareTo.GetType() != GetType())
                return false;

            ConfigurationElementCollection compareToElem = (ConfigurationElementCollection)compareTo;
            if (Count != compareToElem.Count)
                return false;

            foreach (Entry thisEntry in Items)
            {
                bool found = false;
                foreach (Entry compareEntry in compareToElem.Items)
                {
                    if (!Equals(thisEntry.Value, compareEntry.Value)) continue;
                    found = true;
                    break;
                }

                if (found == false)
                {
                    // not in the collection must be different
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hHashCode = 0;
            foreach (Entry thisEntry in Items)
            {
                ConfigurationElement elem = thisEntry.Value;
                hHashCode ^= elem.GetHashCode();
            }
            return hHashCode;
        }


        protected internal override void Unmerge(ConfigurationElement sourceElement,
            ConfigurationElement parentElement,
            ConfigurationSaveMode saveMode)
        {
            base.Unmerge(sourceElement, parentElement, saveMode);
            if (sourceElement == null) return;

            ConfigurationElementCollection parentCollection = parentElement as ConfigurationElementCollection;
            ConfigurationElementCollection sourceCollection = sourceElement as ConfigurationElementCollection;
            Hashtable inheritance = new Hashtable();
            _lockedAllExceptAttributesList = sourceElement._lockedAllExceptAttributesList;
            _lockedAllExceptElementsList = sourceElement._lockedAllExceptElementsList;
            _itemLockedFlag = sourceElement._itemLockedFlag;
            _lockedAttributesList = sourceElement._lockedAttributesList;
            _lockedElementsList = sourceElement._lockedElementsList;

            AssociateContext(sourceElement._configRecord);

            if (parentElement != null)
            {
                if (parentElement._lockedAttributesList != null)
                {
                    _lockedAttributesList = UnMergeLockList(sourceElement._lockedAttributesList,
                        parentElement._lockedAttributesList, saveMode);
                }
                if (parentElement._lockedElementsList != null)
                {
                    _lockedElementsList = UnMergeLockList(sourceElement._lockedElementsList,
                        parentElement._lockedElementsList, saveMode);
                }
                if (parentElement._lockedAllExceptAttributesList != null)
                {
                    _lockedAllExceptAttributesList = UnMergeLockList(sourceElement._lockedAllExceptAttributesList,
                        parentElement._lockedAllExceptAttributesList, saveMode);
                }
                if (parentElement._lockedAllExceptElementsList != null)
                {
                    _lockedAllExceptElementsList = UnMergeLockList(sourceElement._lockedAllExceptElementsList,
                        parentElement._lockedAllExceptElementsList, saveMode);
                }
            }

            if ((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMap) ||
                (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
            {
                // When writing out portable configurations the <clear/> tag should be written
                _collectionCleared = sourceCollection._collectionCleared;
                EmitClear = ((saveMode == ConfigurationSaveMode.Full) && (_clearElement.Length != 0)) ||
                    ((saveMode == ConfigurationSaveMode.Modified) && _collectionCleared) || sourceCollection.EmitClear;

                if ((parentCollection != null) && (EmitClear != true))
                {
                    foreach (Entry entry in parentCollection.Items)
                        if (entry.EntryType != EntryType.Removed)
                            inheritance[entry.GetKey(this)] = InheritedType.InParent;
                }

                foreach (Entry entry in sourceCollection.Items)
                {
                    if (entry.EntryType == EntryType.Removed) continue;

                    if (inheritance.Contains(entry.GetKey(this)))
                    {
                        Entry parentEntry =
                            (Entry)parentCollection.Items[parentCollection.RealIndexOf(entry.Value)];

                        ConfigurationElement elem = entry.Value;
                        if (elem.Equals(parentEntry.Value))
                        {
                            // in modified mode we consider any change to be different than the parent
                            inheritance[entry.GetKey(this)] = InheritedType.InBothSame;
                            if (saveMode != ConfigurationSaveMode.Modified) continue;

                            if (elem.IsModified())
                                inheritance[entry.GetKey(this)] = InheritedType.InBothDiff;
                            else
                            {
                                if (elem.ElementPresent)
                                {
                                    // This is when the source file contained the entry but it was an
                                    // exact copy.  We don't want to emit a remove so we treat it as
                                    // a special case.
                                    inheritance[entry.GetKey(this)] = InheritedType.InBothCopyNoRemove;
                                }
                            }
                        }
                        else
                        {
                            inheritance[entry.GetKey(this)] = InheritedType.InBothDiff;
                            if ((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate)
                                && (entry.EntryType == EntryType.Added))
                            {
                                // this is a special case for deailing with defect number 529517
                                // this code allow the config to write out the same xml when no remove was
                                // present during deserialization.
                                inheritance[entry.GetKey(this)] = InheritedType.InBothCopyNoRemove;
                            }
                        }
                    }
                    else
                    {
                        // not in parent
                        inheritance[entry.GetKey(this)] = InheritedType.InSelf;
                    }
                }

                if ((parentCollection != null) && (EmitClear != true))
                {
                    foreach (Entry entry in parentCollection.Items)
                    {
                        if (entry.EntryType == EntryType.Removed) continue;
                        InheritedType tp = (InheritedType)inheritance[entry.GetKey(this)];
                        if ((tp != InheritedType.InParent) && (tp != InheritedType.InBothDiff)) continue;
                        ConfigurationElement elem = CallCreateNewElement(entry.GetKey(this).ToString());

                        elem.Reset(entry.Value); // copy this entry
                        BaseAdd(elem, ThrowOnDuplicate, true);
                        // Add it (so that is once existed in the temp
                        BaseRemove(entry.GetKey(this), false); // now remove it to for a remove instruction
                    }
                }

                foreach (Entry entry in sourceCollection.Items)
                {
                    if (entry.EntryType == EntryType.Removed) continue;
                    InheritedType tp = (InheritedType)inheritance[entry.GetKey(this)];

                    if ((tp != InheritedType.InSelf) && (tp != InheritedType.InBothDiff) &&
                        (tp != InheritedType.InBothCopyNoRemove))
                        continue;
                    ConfigurationElement elem = CallCreateNewElement(entry.GetKey(this).ToString());

                    elem.Unmerge(entry.Value, null, saveMode);

                    if (tp == InheritedType.InSelf)
                        elem.RemoveAllInheritedLocks(); // If the key changed only local locks are kept

                    BaseAdd(elem, ThrowOnDuplicate, true); // Add it
                }
            }
            else
            {
                if ((CollectionType != ConfigurationElementCollectionType.BasicMap) &&
                    (CollectionType != ConfigurationElementCollectionType.BasicMapAlternate))
                    return;

                foreach (Entry entry in sourceCollection.Items)
                {
                    bool foundKeyInParent = false;
                    Entry parentEntrySaved = null;

                    if ((entry.EntryType != EntryType.Added) && (entry.EntryType != EntryType.Replaced)) continue;
                    bool inParent = false;

                    if (parentCollection != null)
                    {
                        foreach (Entry parentEntry in parentCollection.Items)
                        {
                            if (Equals(entry.GetKey(this), parentEntry.GetKey(this)))
                            {
                                // for basic map collection where the key is actually an element
                                // we do not want the merging behavior or data will not get written
                                // out for the properties if they match the first element deamed to be a parent
                                // For example <allow verbs="NewVerb" users="*"/> will loose the users because
                                // an entry exists in the root element.
                                if (!IsElementName(entry.GetKey(this).ToString()))
                                {
                                    // For elements which are not keyed by the element name
                                    // need to be unmerged
                                    foundKeyInParent = true;
                                    parentEntrySaved = parentEntry;
                                }
                            }

                            if (!Equals(entry.Value, parentEntry.Value)) continue;

                            foundKeyInParent = true;
                            inParent = true; // in parent and the same exact values
                            parentEntrySaved = parentEntry;
                            break;
                        }
                    }

                    ConfigurationElement elem = CallCreateNewElement(entry.GetKey(this).ToString());

                    if (!foundKeyInParent)
                    {
                        // Unmerge is similar to a reset when used like this
                        // except that it handles the different update modes
                        // which Reset does not understand
                        elem.Unmerge(entry.Value, null, saveMode); // copy this entry
                        BaseAdd(-1, elem, true); // Add it
                    }
                    else
                    {
                        ConfigurationElement sourceItem = entry.Value;
                        if (inParent && ((saveMode != ConfigurationSaveMode.Modified) || !sourceItem.IsModified()) &&
                            (saveMode != ConfigurationSaveMode.Full))
                            continue;

                        elem.Unmerge(entry.Value, parentEntrySaved.Value, saveMode);
                        BaseAdd(-1, elem, true); // Add it
                    }
                }
            }
        }

        protected internal override void Reset(ConfigurationElement parentElement)
        {
            ConfigurationElementCollection parentCollection = parentElement as ConfigurationElementCollection;
            ResetLockLists(parentElement);

            if (parentCollection != null)
            {
                foreach (Entry entry in parentCollection.Items)
                {
                    ConfigurationElement elem = CallCreateNewElement(entry.GetKey(this).ToString());
                    elem.Reset(entry.Value);

                    if (((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMap) ||
                        (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate)) &&
                        ((entry.EntryType == EntryType.Added) ||
                        (entry.EntryType == EntryType.Replaced)))
                    {
                        // do not add removed items from the parent
                        BaseAdd(elem, true, true); // This version combines dups and throws (unless overridden)
                    }
                    else
                    {
                        if ((CollectionType == ConfigurationElementCollectionType.BasicMap) ||
                            (CollectionType == ConfigurationElementCollectionType.BasicMapAlternate))
                            BaseAdd(-1, elem, true); // this version appends regardless of if it is a dup.
                    }
                }
                _inheritedCount = Count; // After reset the count is the number of items actually inherited.
            }
        }

        public void CopyTo(ConfigurationElement[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        protected virtual void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, ThrowOnDuplicate);
        }

        protected internal void BaseAdd(ConfigurationElement element, bool throwIfExists)
        {
            BaseAdd(element, throwIfExists, false);
        }

        private void BaseAdd(ConfigurationElement element, bool throwIfExists, bool ignoreLocks)
        {
            bool flagAsReplaced = false;
            bool localAddToEnd = InternalAddToEnd;

            if (IsReadOnly()) throw new ConfigurationErrorsException(SR.Config_base_read_only);

            if (LockItem && (ignoreLocks == false))
                throw new ConfigurationErrorsException(string.Format(SR.Config_base_element_locked, _addElement));

            object key = GetElementKeyInternal(element);
            int iFoundItem = -1;
            for (int index = 0; index < Items.Count; index++)
            {
                Entry entry = (Entry)Items[index];
                if (!CompareKeys(key, entry.GetKey(this))) continue;

                if ((entry.Value != null) && entry.Value.LockItem && (ignoreLocks == false))
                    throw new ConfigurationErrorsException(SR.Config_base_collection_item_locked);

                if ((entry.EntryType != EntryType.Removed) && throwIfExists)
                {
                    if (!element.Equals(entry.Value))
                    {
                        throw new ConfigurationErrorsException(
                            string.Format(SR.Config_base_collection_entry_already_exists, key),
                            element.PropertyFileName(""), element.PropertyLineNumber(""));
                    }

                    entry.Value = element;
                    return;
                }

                if (entry.EntryType != EntryType.Added)
                {
                    if (((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMap) ||
                        (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate)) &&
                        (entry.EntryType == EntryType.Removed) &&
                        (_removedItemCount > 0))
                        _removedItemCount--; // account for the value
                    entry.EntryType = EntryType.Replaced;
                    flagAsReplaced = true;
                }

                if (localAddToEnd ||
                    (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
                {
                    iFoundItem = index;
                    if (entry.EntryType == EntryType.Added)
                    {
                        // this is a special case for defect number 529517 to emulate Everett behavior
                        localAddToEnd = true;
                    }
                    break;
                }

                // check to see if the element is trying to set a locked property.
                if (ignoreLocks == false)
                {
                    element.HandleLockedAttributes(entry.Value);
                    // copy the lock from the removed element before setting the new element
                    element.MergeLocks(entry.Value);
                }

                entry.Value = element;
                _modified = true;
                return;
            }

            // Brand new item.
            if (iFoundItem >= 0)
            {
                Items.RemoveAt(iFoundItem);

                // if the item being removed was inherited adjust the cout
                if ((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate) &&
                    (iFoundItem > Count + _removedItemCount - _inheritedCount))
                    _inheritedCount--;
            }
            BaseAddInternal(localAddToEnd ? -1 : iFoundItem, element, flagAsReplaced, ignoreLocks);
            _modified = true;
        }

        protected int BaseIndexOf(ConfigurationElement element)
        {
            int index = 0;
            object key = GetElementKeyInternal(element);
            foreach (Entry entry in Items)
            {
                if (entry.EntryType == EntryType.Removed) continue;
                if (CompareKeys(key, entry.GetKey(this))) return index;
                index++;
            }
            return -1;
        }

        internal int RealIndexOf(ConfigurationElement element)
        {
            int index = 0;
            object key = GetElementKeyInternal(element);
            foreach (Entry entry in Items)
            {
                if (CompareKeys(key, entry.GetKey(this))) return index;
                index++;
            }
            return -1;
        }

        private void BaseAddInternal(int index, ConfigurationElement element, bool flagAsReplaced, bool ignoreLocks)
        {
            // Allow the element to initialize itself after its
            // constructor has been run so that it may access
            // virtual methods.

            element.AssociateContext(_configRecord);
            element.CallInit();

            if (IsReadOnly()) throw new ConfigurationErrorsException(SR.Config_base_read_only);

            if (!ignoreLocks)
            {
                // during reset we ignore locks so we can copy the elements
                if ((CollectionType == ConfigurationElementCollectionType.BasicMap) ||
                    (CollectionType == ConfigurationElementCollectionType.BasicMapAlternate))
                {
                    if (BaseConfigurationRecord.IsReservedAttributeName(ElementName))
                        throw new ArgumentException(string.Format(SR.Basicmap_item_name_reserved, ElementName));
                    CheckLockedElement(ElementName, null);
                }

                if ((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMap) ||
                    (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
                    CheckLockedElement(_addElement, null);
            }

            if ((CollectionType == ConfigurationElementCollectionType.BasicMapAlternate) ||
                (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
            {
                if (index == -1)
                {
                    // insert before inherited, but after any removed
                    index = Count + _removedItemCount - _inheritedCount;
                }
                else
                {
                    if ((index > Count + _removedItemCount - _inheritedCount) && (flagAsReplaced == false))
                        throw new ConfigurationErrorsException(SR.Config_base_cannot_add_items_below_inherited_items);
                }
            }

            if ((CollectionType == ConfigurationElementCollectionType.BasicMap) &&
                (index >= 0) &&
                (index < _inheritedCount))
                throw new ConfigurationErrorsException(SR.Config_base_cannot_add_items_above_inherited_items);

            EntryType entryType = flagAsReplaced == false ? EntryType.Added : EntryType.Replaced;

            object key = GetElementKeyInternal(element);

            if (index >= 0)
            {
                if (index > Items.Count)
                    throw new ConfigurationErrorsException(string.Format(SR.IndexOutOfRange, index));
                Items.Insert(index, new Entry(entryType, key, element));
            }
            else
            {
                Items.Add(new Entry(entryType, key, element));
            }

            _modified = true;
        }

        protected virtual void BaseAdd(int index, ConfigurationElement element)
        {
            BaseAdd(index, element, false);
        }

        private void BaseAdd(int index, ConfigurationElement element, bool ignoreLocks)
        {
            if (IsReadOnly()) throw new ConfigurationErrorsException(SR.Config_base_read_only);
            if (index < -1) throw new ConfigurationErrorsException(string.Format(SR.IndexOutOfRange, index));

            if ((index != -1) &&
                ((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMap) ||
                (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate)))
            {
                // If it's an AddRemoveClearMap*** collection, turn the index passed into into a real internal index
                int realIndex = 0;

                if (index > 0)
                {
                    foreach (Entry entryfound in Items)
                    {
                        if (entryfound.EntryType != EntryType.Removed) index--;
                        if (index == 0) break;
                        realIndex++;
                    }
                    index = ++realIndex;
                }

                // check for duplicates
                object key = GetElementKeyInternal(element);
                foreach (Entry entry in Items)
                {
                    if (!CompareKeys(key, entry.GetKey(this))
                        || (entry.EntryType == EntryType.Removed))
                        continue;

                    if (!element.Equals(entry.Value))
                    {
                        throw new ConfigurationErrorsException(
                            string.Format(SR.Config_base_collection_entry_already_exists, key),
                            element.PropertyFileName(""), element.PropertyLineNumber(""));
                    }

                    return;
                }
            }

            BaseAddInternal(index, element, false, ignoreLocks);
        }

        protected internal void BaseRemove(object key)
        {
            BaseRemove(key, false);
        }

        private void BaseRemove(object key, bool throwIfMissing)
        {
            if (IsReadOnly())
                throw new ConfigurationErrorsException(SR.Config_base_read_only);

            int index = 0;
            foreach (Entry entry in Items)
            {
                if (CompareKeys(key, entry.GetKey(this)))
                {
                    if (entry.Value == null) // A phoney delete is already present
                    {
                        if (throwIfMissing)
                        {
                            throw new ConfigurationErrorsException(
                                string.Format(SR.Config_base_collection_entry_not_found, key));
                        }
                        return;
                    }

                    if (entry.Value.LockItem)
                        throw new ConfigurationErrorsException(string.Format(SR.Config_base_attribute_locked, key));

                    if (entry.Value.ElementPresent == false)
                        CheckLockedElement(_removeElement, null); // has remove been locked?

                    switch (entry.EntryType)
                    {
                        case EntryType.Added:
                            if ((CollectionType != ConfigurationElementCollectionType.AddRemoveClearMap) &&
                                (CollectionType != ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
                            {
                                if (CollectionType == ConfigurationElementCollectionType.BasicMapAlternate)
                                {
                                    if (index >= Count - _inheritedCount)
                                    {
                                        throw new ConfigurationErrorsException(
                                            SR.Config_base_cannot_remove_inherited_items);
                                    }
                                }
                                if (CollectionType == ConfigurationElementCollectionType.BasicMap)
                                {
                                    if (index < _inheritedCount)
                                    {
                                        throw new ConfigurationErrorsException(
                                            SR.Config_base_cannot_remove_inherited_items);
                                    }
                                }

                                Items.RemoveAt(index);
                            }
                            else
                            {
                                // don't really remove it from the collection just mark it removed
                                entry.EntryType = EntryType.Removed;
                                _removedItemCount++;
                            }
                            break;
                        case EntryType.Removed:
                            if (throwIfMissing)
                                throw new ConfigurationErrorsException(SR.Config_base_collection_entry_already_removed);
                            break;
                        default:
                            if ((CollectionType != ConfigurationElementCollectionType.AddRemoveClearMap) &&
                                (CollectionType != ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
                            {
                                throw new ConfigurationErrorsException(
                                    SR.Config_base_collection_elements_may_not_be_removed);
                            }
                            entry.EntryType = EntryType.Removed;
                            _removedItemCount++;
                            break;
                    }
                    _modified = true;
                    return;
                }
                index++;
            }

            // Note because it is possible for removes to get orphaned by the API they will
            // not cause a throw from the base classes.  The scenerio is:
            //  Add an item in a parent level
            //  remove the item in a child level
            //  remove the item at the parent level.

            if (throwIfMissing)
                throw new ConfigurationErrorsException(string.Format(SR.Config_base_collection_entry_not_found, key));

            if ((CollectionType != ConfigurationElementCollectionType.AddRemoveClearMap) &&
                (CollectionType != ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
                return;

            if (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate)
            {
                Items.Insert(Count + _removedItemCount - _inheritedCount,
                    new Entry(EntryType.Removed, key, null));
            }
            else
                Items.Add(new Entry(EntryType.Removed, key, null));

            _removedItemCount++;
        }

        protected internal ConfigurationElement BaseGet(object key)
        {
            foreach (Entry entry in Items)
                if (entry.EntryType != EntryType.Removed)
                    if (CompareKeys(key, entry.GetKey(this))) return entry.Value;
            return null;
        }

        protected internal bool BaseIsRemoved(object key)
        {
            foreach (Entry entry in Items)
                if (CompareKeys(key, entry.GetKey(this)))
                    return entry.EntryType == EntryType.Removed;
            return false;
        }

        protected internal ConfigurationElement BaseGet(int index)
        {
            if (index < 0) throw new ConfigurationErrorsException(string.Format(SR.IndexOutOfRange, index));

            int virtualIndex = 0;
            Entry entry = null;

            foreach (Entry entryfound in Items)
            {
                if ((virtualIndex == index) && (entryfound.EntryType != EntryType.Removed))
                {
                    entry = entryfound;
                    break;
                }
                if (entryfound.EntryType != EntryType.Removed) virtualIndex++;
            }

            if (entry != null)
                return entry.Value;

            throw new ConfigurationErrorsException(string.Format(SR.IndexOutOfRange, index));
        }

        protected internal object[] BaseGetAllKeys()
        {
            object[] keys = new object[Count];
            int index = 0;
            foreach (Entry entry in Items)
            {
                if (entry.EntryType == EntryType.Removed) continue;
                keys[index] = entry.GetKey(this);
                index++;
            }
            return keys;
        }

        protected internal object BaseGetKey(int index)
        {
            int virtualIndex = 0;
            Entry entry = null;
            if (index < 0) throw new ConfigurationErrorsException(string.Format(SR.IndexOutOfRange, index));

            foreach (Entry entryfound in Items)
            {
                if ((virtualIndex == index) && (entryfound.EntryType != EntryType.Removed))
                {
                    entry = entryfound;
                    break;
                }

                if (entryfound.EntryType != EntryType.Removed) virtualIndex++;
            }

            // Entry entry = (Entry)_items[index];
            if (entry == null) throw new ConfigurationErrorsException(string.Format(SR.IndexOutOfRange, index));

            return entry.GetKey(this);
        }

        protected internal void BaseClear()
        {
            if (IsReadOnly()) throw new ConfigurationErrorsException(SR.Config_base_read_only);

            CheckLockedElement(_clearElement, null); // has clear been locked?
            CheckLockedElement(_removeElement, null); // has remove been locked? Clear implies remove

            _modified = true;
            _collectionCleared = true;
            if (((CollectionType == ConfigurationElementCollectionType.BasicMap) ||
                (CollectionType == ConfigurationElementCollectionType.BasicMapAlternate))
                && (_inheritedCount > 0))
            {
                int removeIndex = 0;
                if (CollectionType == ConfigurationElementCollectionType.BasicMapAlternate)
                    removeIndex = 0; // Inherited items are at the bottom and cannot be removed
                if (CollectionType == ConfigurationElementCollectionType.BasicMap)
                    removeIndex = _inheritedCount; // inherited items are at the top and cannot be removed
                while (Count - _inheritedCount > 0) Items.RemoveAt(removeIndex);
            }
            else
            {
                // do not clear any locked items
                // _items.Clear();
                int inheritedRemoved = 0;
                int removedRemoved = 0;
                int initialCount = Count;

                // check for locks before removing any items from the collection
                for (int checkIndex = 0; checkIndex < Items.Count; checkIndex++)
                {
                    Entry entry = (Entry)Items[checkIndex];
                    if ((entry.Value != null) && entry.Value.LockItem)
                        throw new ConfigurationErrorsException(SR.Config_base_collection_item_locked_cannot_clear);
                }

                for (int removeIndex = Items.Count - 1; removeIndex >= 0; removeIndex--)
                {
                    Entry entry = (Entry)Items[removeIndex];
                    if (((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMap) &&
                        (removeIndex < _inheritedCount)) ||
                        ((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate) &&
                        (removeIndex >= initialCount - _inheritedCount)))
                        inheritedRemoved++;
                    if (entry.EntryType == EntryType.Removed) removedRemoved++;

                    Items.RemoveAt(removeIndex);
                }
                _inheritedCount -= inheritedRemoved;
                _removedItemCount -= removedRemoved;
            }
        }

        protected internal void BaseRemoveAt(int index)
        {
            if (IsReadOnly()) throw new ConfigurationErrorsException(SR.Config_base_read_only);
            int virtualIndex = 0;
            Entry entry = null;

            foreach (Entry entryfound in Items)
            {
                if ((virtualIndex == index) && (entryfound.EntryType != EntryType.Removed))
                {
                    entry = entryfound;
                    break;
                }

                if (entryfound.EntryType != EntryType.Removed) virtualIndex++;
            }

            if (entry == null) throw new ConfigurationErrorsException(string.Format(SR.IndexOutOfRange, index));
            if (entry.Value.LockItem)
            {
                throw new ConfigurationErrorsException(string.Format(SR.Config_base_attribute_locked,
                    entry.GetKey(this)));
            }

            if (entry.Value.ElementPresent == false)
                CheckLockedElement(_removeElement, null); // has remove been locked?

            switch (entry.EntryType)
            {
                case EntryType.Added:
                    if ((CollectionType != ConfigurationElementCollectionType.AddRemoveClearMap) &&
                        (CollectionType != ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
                    {
                        if ((CollectionType == ConfigurationElementCollectionType.BasicMapAlternate) &&
                            (index >= Count - _inheritedCount))
                            throw new ConfigurationErrorsException(SR.Config_base_cannot_remove_inherited_items);

                        if ((CollectionType == ConfigurationElementCollectionType.BasicMap) && (index < _inheritedCount))
                            throw new ConfigurationErrorsException(SR.Config_base_cannot_remove_inherited_items);

                        Items.RemoveAt(index);
                    }
                    else
                    {
                        // don't really remove it from the collection just mark it removed
                        if (entry.Value.ElementPresent == false)
                            CheckLockedElement(_removeElement, null); // has remove been locked?

                        entry.EntryType = EntryType.Removed;
                        _removedItemCount++;
                    }

                    break;

                case EntryType.Removed:
                    throw new ConfigurationErrorsException(SR.Config_base_collection_entry_already_removed);

                default:
                    if ((CollectionType != ConfigurationElementCollectionType.AddRemoveClearMap) &&
                        (CollectionType != ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
                        throw new ConfigurationErrorsException(SR.Config_base_collection_elements_may_not_be_removed);

                    entry.EntryType = EntryType.Removed;
                    _removedItemCount++;
                    break;
            }
            _modified = true;
        }

        protected internal override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
        {
            ConfigurationElementCollectionType type = CollectionType;
            bool dataToWrite = false;

            dataToWrite |= base.SerializeElement(writer, serializeCollectionKey);

            if ((type == ConfigurationElementCollectionType.AddRemoveClearMap) ||
                (type == ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
            {
                // it is possible that the collection only has to be cleared and contains
                // no real elements
                if (_emitClearTag && (_clearElement.Length != 0))
                {
                    if (writer != null)
                    {
                        writer.WriteStartElement(_clearElement);
                        writer.WriteEndElement();
                    }
                    dataToWrite = true;
                }
            }

            foreach (Entry entry in Items)
                switch (type)
                {
                    case ConfigurationElementCollectionType.BasicMap:
                    case ConfigurationElementCollectionType.BasicMapAlternate:
                        if ((entry.EntryType == EntryType.Added) || (entry.EntryType == EntryType.Replaced))
                        {
                            if (!string.IsNullOrEmpty(ElementName))
                            {
                                if (BaseConfigurationRecord.IsReservedAttributeName(ElementName))
                                {
                                    throw new ArgumentException(string.Format(SR.Basicmap_item_name_reserved,
                                        ElementName));
                                }

                                dataToWrite |= entry.Value.SerializeToXmlElement(writer, ElementName);
                            }
                            else dataToWrite |= entry.Value.SerializeElement(writer, false);
                        }
                        break;
                    case ConfigurationElementCollectionType.AddRemoveClearMap:
                    case ConfigurationElementCollectionType.AddRemoveClearMapAlternate:
                        if (((entry.EntryType == EntryType.Removed) ||
                            (entry.EntryType == EntryType.Replaced)) &&
                            (entry.Value != null))
                        {
                            writer?.WriteStartElement(_removeElement);
                            entry.Value.SerializeElement(writer, true);
                            writer?.WriteEndElement();
                            dataToWrite = true;
                        }

                        if ((entry.EntryType == EntryType.Added) || (entry.EntryType == EntryType.Replaced))
                            dataToWrite |= entry.Value.SerializeToXmlElement(writer, _addElement);

                        break;
                }

            return dataToWrite;
        }

        protected override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
        {
            if ((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMap) ||
                (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
            {
                if (elementName == _addElement)
                {
                    ConfigurationElement elem = CallCreateNewElement();
                    elem.ResetLockLists(this);
                    elem.DeserializeElement(reader, false);
                    BaseAdd(elem);
                }
                else
                {
                    if (elementName == _removeElement)
                    {
                        ConfigurationElement elem = CallCreateNewElement();
                        elem.ResetLockLists(this);
                        elem.DeserializeElement(reader, true);
                        if (IsElementRemovable(elem)) BaseRemove(GetElementKeyInternal(elem), false);
                    }
                    else
                    {
                        if (elementName != _clearElement) return false;

                        if (reader.AttributeCount > 0)
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                string propertyName = reader.Name;
                                throw new ConfigurationErrorsException(
                                    string.Format(SR.Config_base_unrecognized_attribute, propertyName), reader);
                            }
                        }

                        CheckLockedElement(elementName, reader);
                        reader.MoveToElement();
                        BaseClear(); //
                        _emitClearTag = true;
                    }
                }
            }
            else
            {
                if (elementName == ElementName)
                {
                    if (BaseConfigurationRecord.IsReservedAttributeName(elementName))
                        throw new ArgumentException(string.Format(SR.Basicmap_item_name_reserved, elementName));
                    ConfigurationElement elem = CallCreateNewElement();
                    elem.ResetLockLists(this);
                    elem.DeserializeElement(reader, false);
                    BaseAdd(elem);
                }
                else
                {
                    if (!IsElementName(elementName)) return false;

                    // this section handle the collection like the allow deny senario which
                    if (BaseConfigurationRecord.IsReservedAttributeName(elementName))
                        throw new ArgumentException(string.Format(SR.Basicmap_item_name_reserved, elementName));

                    // have multiple tags for the collection
                    ConfigurationElement elem = CallCreateNewElement(elementName);
                    elem.ResetLockLists(this);
                    elem.DeserializeElement(reader, false);
                    BaseAdd(-1, elem);
                }
            }
            return true;
        }

        private ConfigurationElement CallCreateNewElement(string elementName)
        {
            ConfigurationElement elem = CreateNewElement(elementName);
            elem.AssociateContext(_configRecord);
            elem.CallInit();
            return elem;
        }

        private ConfigurationElement CallCreateNewElement()
        {
            ConfigurationElement elem = CreateNewElement();
            elem.AssociateContext(_configRecord);
            elem.CallInit();
            return elem;
        }

        protected virtual ConfigurationElement CreateNewElement(string elementName)
        {
            return CreateNewElement();
        }

        protected abstract ConfigurationElement CreateNewElement();
        protected abstract object GetElementKey(ConfigurationElement element);

        internal object GetElementKeyInternal(ConfigurationElement element)
        {
            object key = GetElementKey(element);
            if (key == null)
                throw new ConfigurationErrorsException(SR.Config_base_invalid_element_key);
            return key;
        }

        protected virtual bool IsElementRemovable(ConfigurationElement element)
        {
            return true;
        }

        private bool CompareKeys(object key1, object key2)
        {
            if (_comparer != null) return _comparer.Compare(key1, key2) == 0;
            return key1.Equals(key2);
        }

        protected virtual bool IsElementName(string elementName)
        {
            return false;
        }

        internal bool IsLockableElement(string elementName)
        {
            if ((CollectionType == ConfigurationElementCollectionType.AddRemoveClearMap) ||
                (CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate))
            {
                return (elementName == AddElementName) ||
                    (elementName == RemoveElementName) ||
                    (elementName == ClearElementName);
            }
            return (elementName == ElementName) || IsElementName(elementName);
        }

        private enum InheritedType
        {
            InNeither = 0,
            InParent = 1,
            InSelf = 2,
            InBothSame = 3,
            InBothDiff = 4,
            InBothCopyNoRemove = 5,
        }

        private enum EntryType
        {
            Inherited,
            Replaced,
            Removed,
            Added,
        }

        private class Entry
        {
            private readonly object _key;
            internal EntryType EntryType;
            internal ConfigurationElement Value;

            internal Entry(EntryType type, object key, ConfigurationElement value)
            {
                EntryType = type;
                _key = key;
                Value = value;
            }

            internal object GetKey(ConfigurationElementCollection thisCollection)
            {
                // For items that have been really inserted...
                return Value != null ? thisCollection.GetElementKeyInternal(Value) : _key;
            }
        }

        private class Enumerator : IDictionaryEnumerator
        {
            private readonly IEnumerator _itemsEnumerator;
            private readonly ConfigurationElementCollection _thisCollection;
            private DictionaryEntry _current;

            internal Enumerator(ArrayList items, ConfigurationElementCollection collection)
            {
                _itemsEnumerator = items.GetEnumerator();
                _thisCollection = collection;
            }

            bool IEnumerator.MoveNext()
            {
                while (_itemsEnumerator.MoveNext())
                {
                    Entry entry = (Entry)_itemsEnumerator.Current;
                    if (entry.EntryType == EntryType.Removed) continue;
                    _current.Key = entry.GetKey(_thisCollection) != null ? entry.GetKey(_thisCollection) : "key";
                    _current.Value = entry.Value;
                    return true;
                }
                return false;
            }

            void IEnumerator.Reset()
            {
                _itemsEnumerator.Reset();
            }

            object IEnumerator.Current => _current.Value;

            DictionaryEntry IDictionaryEnumerator.Entry => _current;

            object IDictionaryEnumerator.Key => _current.Key;

            object IDictionaryEnumerator.Value => _current.Value;
        }
    }
}
