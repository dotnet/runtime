// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Configuration
{
    public class SettingsPropertyCollection : IEnumerable, ICloneable, ICollection
    {
        private Hashtable _hashtable = null;
        private bool _readOnly = false;

        public SettingsPropertyCollection()
        {
            _hashtable = new Hashtable(10, StringComparer.CurrentCultureIgnoreCase);
        }

        public void Add(SettingsProperty property)
        {
            if (_readOnly)
                throw new NotSupportedException();

            OnAdd(property);
            _hashtable.Add(property.Name, property);
            try
            {
                OnAddComplete(property);
            }
            catch
            {
                _hashtable.Remove(property.Name);
                throw;
            }
        }

        public void Remove(string name)
        {
            if (_readOnly)
                throw new NotSupportedException();
            SettingsProperty toRemove = (SettingsProperty)_hashtable[name];
            if (toRemove == null)
                return;
            OnRemove(toRemove);
            _hashtable.Remove(name);
            try
            {
                OnRemoveComplete(toRemove);
            }
            catch
            {
                _hashtable.Add(name, toRemove);
                throw;
            }

        }

        public SettingsProperty this[string name]
        {
            get
            {
                return _hashtable[name] as SettingsProperty;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _hashtable.Values.GetEnumerator();
        }

        public object Clone()
        {
            return new SettingsPropertyCollection(_hashtable);
        }

        public void SetReadOnly()
        {
            if (_readOnly)
                return;
            _readOnly = true;
        }

        public void Clear()
        {
            if (_readOnly)
                throw new NotSupportedException();
            OnClear();
            _hashtable.Clear();
            OnClearComplete();
        }

        // On* Methods for deriving classes to override.  These have
        // been modeled after the CollectionBase class but have
        // been stripped of their "index" parameters as there is no
        // visible index to this collection.

        protected virtual void OnAdd(SettingsProperty property) { }

        protected virtual void OnAddComplete(SettingsProperty property) { }

        protected virtual void OnClear() { }

        protected virtual void OnClearComplete() { }

        protected virtual void OnRemove(SettingsProperty property) { }

        protected virtual void OnRemoveComplete(SettingsProperty property) { }

        // ICollection interface
        public int Count { get { return _hashtable.Count; } }
        public bool IsSynchronized { get { return false; } }
        public object SyncRoot { get { return this; } }

        public void CopyTo(Array array, int index)
        {
            _hashtable.Values.CopyTo(array, index);
        }

        private SettingsPropertyCollection(Hashtable h)
        {
            _hashtable = (Hashtable)h.Clone();
        }
    }
}
