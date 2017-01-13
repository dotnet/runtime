// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Configuration
{
    public class SettingsPropertyValueCollection : IEnumerable, ICloneable, ICollection
    {
        private Hashtable _indices = null;
        private ArrayList _values = null;
        private bool _readOnly = false;

        public SettingsPropertyValueCollection()
        {
            _indices = new Hashtable(10, StringComparer.CurrentCultureIgnoreCase);
            _values = new ArrayList();
        }

        public void Add(SettingsPropertyValue property)
        {
            if (_readOnly)
                throw new NotSupportedException();

            int pos = _values.Add(property);

            try
            {
                _indices.Add(property.Name, pos);
            }
            catch (Exception)
            {
                _values.RemoveAt(pos);
                throw;
            }
        }

        public void Remove(string name)
        {
            if (_readOnly)
                throw new NotSupportedException();

            object pos = _indices[name];

            if (pos == null || !(pos is int))
                return;

            int ipos = (int)pos;

            if (ipos >= _values.Count)
                return;

            _values.RemoveAt(ipos);
            _indices.Remove(name);

            ArrayList al = new ArrayList();

            foreach (DictionaryEntry de in _indices)
                if ((int)de.Value > ipos)
                    al.Add(de.Key);

            foreach (string key in al)
                _indices[key] = ((int)_indices[key]) - 1;
        }

        public SettingsPropertyValue this[string name]
        {
            get
            {
                object pos = _indices[name];

                if (pos == null || !(pos is int))
                    return null;

                int ipos = (int)pos;

                if (ipos >= _values.Count)
                    return null;

                return (SettingsPropertyValue)_values[ipos];
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public object Clone()
        {
            return new SettingsPropertyValueCollection(_indices, _values);
        }

        public void SetReadOnly()
        {
            if (_readOnly)
                return;

            _readOnly = true;
            _values = ArrayList.ReadOnly(_values);
        }

        public void Clear()
        {
            _values.Clear();
            _indices.Clear();
        }

        public int Count { get { return _values.Count; } }

        public bool IsSynchronized { get { return false; } }

        public object SyncRoot { get { return this; } }

        public void CopyTo(Array array, int index)
        {
            _values.CopyTo(array, index);
        }

        private SettingsPropertyValueCollection(Hashtable indices, ArrayList values)
        {
            _indices = (Hashtable)indices.Clone();
            _values = (ArrayList)values.Clone();
        }
    }

}
