// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Data.Odbc
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class OdbcErrorCollection : ICollection
    {
        private readonly ArrayList _items = new ArrayList(); // Do not rename (binary serialization)

        internal OdbcErrorCollection()
        {
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return this; }
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }

        public OdbcError this[int i]
        {
            get
            {
                return (OdbcError)_items[i]!;
            }
        }

        internal void Add(OdbcError error)
        {
            _items.Add(error);
        }

        public void CopyTo(Array array, int i)
        {
            _items.CopyTo(array, i);
        }

        public void CopyTo(OdbcError[] array, int i)
        {
            _items.CopyTo(array, i);
        }

        public IEnumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        internal void SetSource(string Source)
        {
            foreach (object error in _items)
            {
                ((OdbcError)error).SetSource(Source);
            }
        }
    }
}
