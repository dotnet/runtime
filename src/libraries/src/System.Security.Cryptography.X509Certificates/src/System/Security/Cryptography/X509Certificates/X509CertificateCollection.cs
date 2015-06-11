// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using Interlocked = System.Threading.Interlocked;

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public partial class X509CertificateCollection : ICollection, IEnumerable, IList
    {
        public X509CertificateCollection()
        {
            _list = new LowLevelListWithIList<Object>();
        }

        public X509CertificateCollection(X509Certificate[] value)
            : this()
        {
            this.AddRange(value);
        }

        public X509CertificateCollection(X509CertificateCollection value)
            : this()
        {
            this.AddRange(value);
        }

        public int Count
        {
            get { return _list.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        Object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        Object IList.this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);

                return _list[index];
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
                if (value == null)
                    throw new ArgumentNullException("value");

                _list[index] = value;
            }
        }


        public X509Certificate this[int index]
        {
            get
            {
                // Note: If a non-X509Certificate was inserted at this position, the result InvalidCastException is the defined behavior.
                return (X509Certificate)(List[index]);
            }
            set
            {
                List[index] = value;
            }
        }

        public int Add(X509Certificate value)
        {
            return List.Add(value);
        }

        public void AddRange(X509Certificate[] value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            for (int i = 0; i < value.Length; i++)
            {
                this.Add(value[i]);
            }
        }

        public void AddRange(X509CertificateCollection value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            for (int i = 0; i < value.Count; i++)
            {
                this.Add(value[i]);
            }
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(X509Certificate value)
        {
            foreach (X509Certificate cert in List)
            {
                if (cert.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(X509Certificate[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public X509CertificateEnumerator GetEnumerator()
        {
            return new X509CertificateEnumerator(((IList<Object>)_list).GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new X509CertificateEnumerator(((IList<Object>)_list).GetEnumerator());
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            foreach (X509Certificate cert in this)
            {
                hashCode += cert.GetHashCode();
            }
            return hashCode;
        }

        public int IndexOf(X509Certificate value)
        {
            return List.IndexOf(value);
        }

        public void Insert(int index, X509Certificate value)
        {
            List.Insert(index, value);
        }

        public void Remove(X509Certificate value)
        {
            List.Remove(value);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
            _list.RemoveAt(index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            List.CopyTo(array, index);
        }

        int IList.Add(Object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            int index = _list.Count;
            _list.Add(value);
            return index;
        }

        bool IList.Contains(Object value)
        {
            return _list.Contains(value);
        }

        int IList.IndexOf(Object value)
        {
            return _list.IndexOf(value);
        }

        void IList.Insert(int index, Object value)
        {
            _list.Insert(index, value);
        }

        void IList.Remove(Object value)
        {
            _list.Remove(value);
        }

        internal IList List
        {
            get { return this; }
        }

        private LowLevelListWithIList<Object> _list;
        private Object _syncRoot;
    }
}

