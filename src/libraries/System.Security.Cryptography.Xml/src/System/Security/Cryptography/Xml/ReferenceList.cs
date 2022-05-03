// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Security.Cryptography.Xml
{
    public sealed class ReferenceList : IList
    {
        private readonly ArrayList _references;

        public ReferenceList()
        {
            _references = new ArrayList();
        }

        public IEnumerator GetEnumerator()
        {
            return _references.GetEnumerator();
        }

        public int Count
        {
            get { return _references.Count; }
        }

        public int Add(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!(value is DataReference) && !(value is KeyReference))
                throw new ArgumentException(SR.Cryptography_Xml_IncorrectObjectType, nameof(value));

            return _references.Add(value);
        }

        public void Clear()
        {
            _references.Clear();
        }

        public bool Contains(object value)
        {
            return _references.Contains(value);
        }

        public int IndexOf(object value)
        {
            return _references.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!(value is DataReference) && !(value is KeyReference))
                throw new ArgumentException(SR.Cryptography_Xml_IncorrectObjectType, nameof(value));

            _references.Insert(index, value);
        }

        public void Remove(object value)
        {
            _references.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _references.RemoveAt(index);
        }

        public EncryptedReference Item(int index)
        {
            return (EncryptedReference)_references[index];
        }

        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public EncryptedReference this[int index]
        {
            get
            {
                return Item(index);
            }
            set
            {
                ((IList)this)[index] = value;
            }
        }

        /// <internalonly/>
        object IList.this[int index]
        {
            get { return _references[index]; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!(value is DataReference) && !(value is KeyReference))
                    throw new ArgumentException(SR.Cryptography_Xml_IncorrectObjectType, nameof(value));

                _references[index] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
            _references.CopyTo(array, index);
        }

        bool IList.IsFixedSize
        {
            get { return _references.IsFixedSize; }
        }

        bool IList.IsReadOnly
        {
            get { return _references.IsReadOnly; }
        }

        public object SyncRoot
        {
            get { return _references.SyncRoot; }
        }

        public bool IsSynchronized
        {
            get { return _references.IsSynchronized; }
        }
    }
}
