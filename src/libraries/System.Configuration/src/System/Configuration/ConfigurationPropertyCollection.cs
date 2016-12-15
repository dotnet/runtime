// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Configuration
{
    public class ConfigurationPropertyCollection : ICollection
    {
        private readonly ArrayList _items = new ArrayList();

        internal ConfigurationProperty DefaultCollectionProperty
            => this[ConfigurationProperty.s_defaultCollectionPropertyName];

        public ConfigurationProperty this[string name]
        {
            get
            {
                for (int index = 0; index < _items.Count; index++)
                {
                    ConfigurationProperty cp = (ConfigurationProperty)_items[index];
                    if (cp.Name == name) return (ConfigurationProperty)_items[index];
                }
                return null;
            }
        }

        public int Count => _items.Count;

        public bool IsSynchronized => false;

        public object SyncRoot => _items;

        void ICollection.CopyTo(Array array, int index)
        {
            _items.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public void CopyTo(ConfigurationProperty[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        public bool Contains(string name)
        {
            for (int index = 0; index < _items.Count; index++)
            {
                ConfigurationProperty cp = (ConfigurationProperty)_items[index];
                if (cp.Name == name) return true;
            }
            return false;
        }

        public void Add(ConfigurationProperty property)
        {
            if (Contains(property.Name) != true) _items.Add(property);
        }

        public bool Remove(string name)
        {
            for (int index = 0; index < _items.Count; index++)
            {
                ConfigurationProperty cp = (ConfigurationProperty)_items[index];
                if (cp.Name != name) continue;

                _items.RemoveAt(index);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}