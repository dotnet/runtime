// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using System.Text;

namespace System.Configuration
{
    public sealed class ConfigurationLockCollection : ICollection
    {
        private const string LockAll = "*";
        private readonly string _ignoreName;
        private readonly ConfigurationElement _thisElement;
        private readonly ArrayList _internalArraylist;
        private readonly HybridDictionary _internalDictionary;
        private string _seedList = string.Empty;

        internal ConfigurationLockCollection(ConfigurationElement thisElement)
            : this(thisElement, ConfigurationLockCollectionType.LockedAttributes)
        { }

        internal ConfigurationLockCollection(ConfigurationElement thisElement, ConfigurationLockCollectionType lockType)
            : this(thisElement, lockType, string.Empty)
        { }

        internal ConfigurationLockCollection(ConfigurationElement thisElement, ConfigurationLockCollectionType lockType,
            string ignoreName)
            : this(thisElement, lockType, ignoreName, null)
        { }

        internal ConfigurationLockCollection(ConfigurationElement thisElement, ConfigurationLockCollectionType lockType,
            string ignoreName, ConfigurationLockCollection parentCollection)
        {
            _thisElement = thisElement;
            LockType = lockType;
            _internalDictionary = new HybridDictionary();
            _internalArraylist = new ArrayList();
            IsModified = false;

            ExceptionList = (LockType == ConfigurationLockCollectionType.LockedExceptionList) ||
                (LockType == ConfigurationLockCollectionType.LockedElementsExceptionList);
            _ignoreName = ignoreName;

            if (parentCollection == null) return;

            foreach (string key in parentCollection) // seed the new collection
            {
                Add(key, ConfigurationValueFlags.Inherited); // add the local copy
                if (!ExceptionList) continue;

                if (_seedList.Length != 0)
                    _seedList += ",";
                _seedList += key;
            }
        }

        internal ConfigurationLockCollectionType LockType { get; }

        public bool IsModified { get; private set; }

        internal bool ExceptionList { get; }

        public string AttributeList
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                foreach (DictionaryEntry de in _internalDictionary)
                {
                    if (sb.Length != 0) sb.Append(',');
                    sb.Append(de.Key);
                }
                return sb.ToString();
            }
        }

        public bool HasParentElements
        {
            get
            {
                // return true if there is at least one element that was defined in the parent
                bool result = false;

                // Check to see if the exception list is empty as a result of a merge from config
                // If so the there were some parent elements because empty string is invalid in config.
                // and the only way to get an empty list is for the merged config to have no elements
                // in common.
                if (ExceptionList && (_internalDictionary.Count == 0) && !string.IsNullOrEmpty(_seedList))
                    return true;

                foreach (DictionaryEntry de in _internalDictionary)
                    if (((ConfigurationValueFlags)de.Value & ConfigurationValueFlags.Inherited) != 0)
                    {
                        result = true;
                        break;
                    }

                return result;
            }
        }

        public int Count => _internalDictionary.Count;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            _internalArraylist.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return _internalArraylist.GetEnumerator();
        }

        internal void ClearSeedList()
        {
            _seedList = string.Empty;
        }

        public void Add(string name)
        {
            if (((_thisElement.ItemLocked & ConfigurationValueFlags.Locked) != 0) &&
                ((_thisElement.ItemLocked & ConfigurationValueFlags.Inherited) != 0))
                throw new ConfigurationErrorsException(string.Format(SR.Config_base_attribute_locked, name));

            ConfigurationValueFlags flags = ConfigurationValueFlags.Modified;

            string attribToLockTrim = name.Trim();
            ConfigurationProperty propToLock = _thisElement.Properties[attribToLockTrim];
            if ((propToLock == null) && (attribToLockTrim != LockAll))
            {
                ConfigurationElementCollection collection = _thisElement as ConfigurationElementCollection;
                if ((collection == null) && (_thisElement.Properties.DefaultCollectionProperty != null))
                {
                    // this is not a collection but it may contain a default collection
                    collection =
                        _thisElement[_thisElement.Properties.DefaultCollectionProperty] as
                            ConfigurationElementCollection;
                }

                if ((collection == null) ||
                    (LockType == ConfigurationLockCollectionType.LockedAttributes) ||
                    // If the collection type is not element then the lock is bogus
                    (LockType == ConfigurationLockCollectionType.LockedExceptionList))
                    _thisElement.ReportInvalidLock(attribToLockTrim, LockType, null, null);
                else
                {
                    if (!collection.IsLockableElement(attribToLockTrim))
                        _thisElement.ReportInvalidLock(attribToLockTrim, LockType, null, collection.LockableElements);
                }
            }
            else
            {
                // the lock is in the property bag but is it the correct type?
                if ((propToLock != null) && propToLock.IsRequired)
                {
                    throw new ConfigurationErrorsException(string.Format(SR.Config_base_required_attribute_lock_attempt,
                        propToLock.Name));
                }

                if (attribToLockTrim != LockAll)
                {
                    if ((LockType == ConfigurationLockCollectionType.LockedElements) ||
                        (LockType == ConfigurationLockCollectionType.LockedElementsExceptionList))
                    {
                        // If it is an element then it must be derived from ConfigurationElement
                        if (!typeof(ConfigurationElement).IsAssignableFrom(propToLock?.Type))
                            _thisElement.ReportInvalidLock(attribToLockTrim, LockType, null, null);
                    }
                    else
                    {
                        // if it is a property then it cannot be derived from ConfigurationElement
                        if (typeof(ConfigurationElement).IsAssignableFrom(propToLock?.Type))
                            _thisElement.ReportInvalidLock(attribToLockTrim, LockType, null, null);
                    }
                }
            }

            if (_internalDictionary.Contains(name))
            {
                flags = ConfigurationValueFlags.Modified | (ConfigurationValueFlags)_internalDictionary[name];
                _internalDictionary.Remove(name); // not from parent
                _internalArraylist.Remove(name);
            }
            _internalDictionary.Add(name, flags); // not from parent
            _internalArraylist.Add(name);
            IsModified = true;
        }

        internal void Add(string name, ConfigurationValueFlags flags)
        {
            if ((flags != ConfigurationValueFlags.Inherited) && _internalDictionary.Contains(name))
            {
                // the user has an item declared as locked below a level where it is already locked
                // keep enough info so we can write out the lock if they save in modified mode
                flags = ConfigurationValueFlags.Modified | (ConfigurationValueFlags)_internalDictionary[name];
                _internalDictionary.Remove(name);
                _internalArraylist.Remove(name);
            }

            _internalDictionary.Add(name, flags); // not from parent
            _internalArraylist.Add(name);
        }

        internal bool DefinedInParent(string name)
        {
            if (name == null)
                return false;

            if (!ExceptionList)
            {
                return _internalDictionary.Contains(name) &&
                    (((ConfigurationValueFlags)_internalDictionary[name] & ConfigurationValueFlags.Inherited) != 0);
            }

            string parentListEnclosed = "," + _seedList + ",";
            if (name.Equals(_ignoreName) ||
                (parentListEnclosed.IndexOf("," + name + ",", StringComparison.Ordinal) >= 0))
                return true;
            return _internalDictionary.Contains(name) &&
                (((ConfigurationValueFlags)_internalDictionary[name] & ConfigurationValueFlags.Inherited) != 0);
        }

        internal bool IsValueModified(string name)
        {
            return _internalDictionary.Contains(name) &&
                (((ConfigurationValueFlags)_internalDictionary[name] & ConfigurationValueFlags.Modified) != 0);
        }

        internal void RemoveInheritedLocks()
        {
            StringCollection removeList = new StringCollection();
            foreach (string key in this) if (DefinedInParent(key)) removeList.Add(key);
            foreach (string key in removeList)
            {
                _internalDictionary.Remove(key);
                _internalArraylist.Remove(key);
            }
        }

        public void Remove(string name)
        {
            if (!_internalDictionary.Contains(name))
                throw new ConfigurationErrorsException(string.Format(SR.Config_base_collection_entry_not_found, name));

            // in a locked list you cannot remove items that were locked in the parent
            // in an exception list this is legal because it makes the list more restrictive
            if ((ExceptionList == false) &&
                (((ConfigurationValueFlags)_internalDictionary[name] & ConfigurationValueFlags.Inherited) != 0))
            {
                if (((ConfigurationValueFlags)_internalDictionary[name] & ConfigurationValueFlags.Modified) == 0)
                    throw new ConfigurationErrorsException(string.Format(SR.Config_base_attribute_locked, name));

                // allow the local one to be "removed" so it won't write out but throw if they try and remove
                // one that is only inherited
                ConfigurationValueFlags flags = (ConfigurationValueFlags)_internalDictionary[name];
                flags &= ~ConfigurationValueFlags.Modified;
                _internalDictionary[name] = flags;
                IsModified = true;
                return;
            }

            _internalDictionary.Remove(name);
            _internalArraylist.Remove(name);
            IsModified = true;
        }

        internal void ClearInternal(bool useSeedIfAvailble)
        {
            ArrayList removeList = new ArrayList();
            foreach (DictionaryEntry de in _internalDictionary)
                if ((((ConfigurationValueFlags)de.Value & ConfigurationValueFlags.Inherited) == 0)
                    || ExceptionList)
                    removeList.Add(de.Key);

            foreach (object removeKey in removeList)
            {
                _internalDictionary.Remove(removeKey);
                _internalArraylist.Remove(removeKey);
            }

            // Clearing an Exception list really means revert to parent
            if (useSeedIfAvailble && !string.IsNullOrEmpty(_seedList))
            {
                string[] keys = _seedList.Split(',');
                foreach (string key in keys) Add(key, ConfigurationValueFlags.Inherited);
            }
            IsModified = true;
        }

        public void Clear()
        {
            ClearInternal(true);
        }

        public bool Contains(string name)
        {
            if (ExceptionList && name.Equals(_ignoreName)) return true;
            return _internalDictionary.Contains(name);
        }

        public void CopyTo(string[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        internal void ResetModified()
        {
            IsModified = false;
        }

        public bool IsReadOnly(string name)
        {
            if (!_internalDictionary.Contains(name))
                throw new ConfigurationErrorsException(string.Format(SR.Config_base_collection_entry_not_found, name));
            return
                ((ConfigurationValueFlags)_internalDictionary[name] & ConfigurationValueFlags.Inherited) != 0;
        }

        public void SetFromList(string attributeList)
        {
            string[] splits = attributeList.Split(',', ';', ':');
            Clear();
            foreach (string name in splits)
            {
                string attribTrim = name.Trim();
                if (!Contains(attribTrim)) Add(attribTrim);
            }
        }
    }
}