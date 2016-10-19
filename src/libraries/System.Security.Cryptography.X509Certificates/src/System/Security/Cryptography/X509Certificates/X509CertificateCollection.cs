// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates
{
    [Serializable]
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
                    throw new ArgumentNullException(nameof(value));

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
                    throw new ArgumentNullException(nameof(value));

                _list[index] = value;
            }
        }

        public int Add(X509Certificate value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            int index = _list.Count;
            _list.Add(value);
            return index;
        }

        public void AddRange(X509Certificate[] value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            for (int i = 0; i < value.Length; i++)
            {
                Add(value[i]);
            }
        }

        public void AddRange(X509CertificateCollection value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

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
                throw new ArgumentNullException(nameof(value));

            _list.Insert(index, value);
        }

        public void Remove(X509Certificate value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            bool removed = _list.Remove(value);

            // This throws on full framework, so it will also throw here.
            if (!removed)
            {
                throw new ArgumentException(SR.Arg_RemoveArgNotFound);
            }
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
                throw new ArgumentNullException(nameof(value));

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
                throw new ArgumentNullException(nameof(value));

            NonGenericList.Insert(index, value);
        }

        void IList.Remove(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // On full framework this method throws when removing an item that
            // is not present in the collection, and that behavior needs to be
            // preserved.
            //
            // Since that behavior is not provided by the IList.Remove exposed
            // via the NonGenericList property, this method can't just defer
            // like the rest of the IList explicit implementations do.
            //
            // The List<T> which backs this collection will guard against any
            // objects which are not X509Certificiate-or-derived, and we've
            // already checked whether value itself was null.  Therefore we
            // know that any (value as X509Certificate) which becomes null
            // could not have been in our collection, and when not null we
            // have a rich object reference and can defer to the other Remove
            // method on this class.

            X509Certificate cert = value as X509Certificate;

            if (cert == null)
            {
                throw new ArgumentException(SR.Arg_RemoveArgNotFound);
            }

            Remove(cert);
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
