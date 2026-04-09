// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates
{
    public partial class X509CertificateCollection : System.Collections.CollectionBase
    {
        public X509CertificateCollection()
        {
        }

        public X509CertificateCollection(X509Certificate[] value)
        {
            AddRange(value);
        }

        public X509CertificateCollection(X509CertificateCollection value)
        {
            AddRange(value);
        }

        public X509Certificate this[int index]
        {
            get
            {
                return ((X509Certificate)(List[index])!);
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                List[index] = value;
            }
        }

        public int Add(X509Certificate value)
        {
            return List.Add(value);
        }

        public void AddRange(X509Certificate[] value)
        {
            ArgumentNullException.ThrowIfNull(value);

            for (int i = 0; i < value.Length; i++)
            {
                Add(value[i]);
            }
        }

        public void AddRange(X509CertificateCollection value)
        {
            ArgumentNullException.ThrowIfNull(value);

            for (int i = 0; i < value.Count; i++)
            {
                Add(value[i]);
            }
        }

        public bool Contains(X509Certificate value)
        {
            return List.Contains(value);
        }

        public void CopyTo(X509Certificate[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public new X509CertificateEnumerator GetEnumerator()
        {
            return new X509CertificateEnumerator(this);
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            foreach (X509Certificate cert in List)
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

        // Although this class for compatibility must derive from CollectionBase
        // we can change the behavior a bit to verify the type of object before
        // we add it to the collection
        protected override void OnValidate(object value)
        {
            base.OnValidate(value);

            if (!(value is X509Certificate))
                throw new ArgumentException(SR.Arg_InvalidType, nameof(value));
        }
    }
}
