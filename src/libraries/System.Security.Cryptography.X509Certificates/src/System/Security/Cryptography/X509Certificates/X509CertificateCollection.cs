// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates
{
    public partial class X509CertificateCollection : ICollection, IEnumerable, IList
    {
        private readonly List<X509Certificate> _list;

        public X509CertificateCollection()
        {
            _list = new List<X509Certificate>();
        }

        public X509CertificateCollection(X509Certificate[] value)
            : this()
        {
            AddRange(value);
        }

        public X509CertificateCollection(X509CertificateCollection value)
            : this()
        {
            AddRange(value);
        }

        public int Count
        {
            get { return _list.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return NonGenericList.SyncRoot; }
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        object IList.this[int index]
        {
            get
            {
                return NonGenericList[index];
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                NonGenericList[index] = value;
            }
        }

        public X509Certificate this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _list[index] = value;
            }
        }

        public int Add(X509Certificate value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            int index = _list.Count;
            _list.Add(value);
            return index;
        }

        public void AddRange(X509Certificate[] value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            for (int i = 0; i < value.Length; i++)
            {
                Add(value[i]);
            }
        }

        public void AddRange(X509CertificateCollection value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            for (int i = 0; i < value.Count; i++)
            {
                Add(value[i]);
            }
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(X509Certificate value)
        {
            return _list.Contains(value);
        }

        public void CopyTo(X509Certificate[] array, int index)
        {
            _list.CopyTo(array, index);
        }

        public X509CertificateEnumerator GetEnumerator()
        {
            return new X509CertificateEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            foreach (X509Certificate cert in _list)
            {
                hashCode += cert.GetHashCode();
            }
            return hashCode;
        }

        public int IndexOf(X509Certificate value)
        {
            return _list.IndexOf(value);
        }

        public void Insert(int index, X509Certificate value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            _list.Insert(index, value);
        }

        public void Remove(X509Certificate value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            _list.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            NonGenericList.CopyTo(array, index);
        }

        int IList.Add(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return NonGenericList.Add(value);
        }

        bool IList.Contains(object value)
        {
            return NonGenericList.Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return NonGenericList.IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            NonGenericList.Insert(index, value);
        }

        void IList.Remove(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            NonGenericList.Remove(value);
        }

        private IList NonGenericList
        {
            get { return _list; }
        }

        internal void GetEnumerator(out List<X509Certificate>.Enumerator enumerator)
        {
            enumerator = _list.GetEnumerator();
        }
    }
}
