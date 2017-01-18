// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Configuration.Provider
{
    public class ProviderCollection : ICollection
    {
        private readonly Hashtable _hashtable;
        private bool _readOnly;

        public ProviderCollection()
        {
            _hashtable = new Hashtable(10, StringComparer.OrdinalIgnoreCase);
        }

        public ProviderBase this[string name] => _hashtable[name] as ProviderBase;

        public IEnumerator GetEnumerator()
        {
            return _hashtable.Values.GetEnumerator();
        }

        public int Count => _hashtable.Count;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            _hashtable.Values.CopyTo(array, index);
        }

        public virtual void Add(ProviderBase provider)
        {
            if (_readOnly)
                throw new NotSupportedException(SR.CollectionReadOnly);

            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if ((provider.Name == null) || (provider.Name.Length < 1))
                throw new ArgumentException(SR.Config_provider_name_null_or_empty);

            _hashtable.Add(provider.Name, provider);
        }

        public void Remove(string name)
        {
            if (_readOnly)
                throw new NotSupportedException(SR.CollectionReadOnly);
            _hashtable.Remove(name);
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
                throw new NotSupportedException(SR.CollectionReadOnly);
            _hashtable.Clear();
        }

        public void CopyTo(ProviderBase[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }
    }
}