// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Security.Cryptography.X509Certificates
{
    public partial class X509CertificateCollection : System.Collections.CollectionBase
    {
        public class X509CertificateEnumerator : IEnumerator
        {
            private readonly IEnumerator _enumerator;

            public X509CertificateEnumerator(X509CertificateCollection mappings)
            {
                ArgumentNullException.ThrowIfNull(mappings);

                _enumerator = ((IEnumerable)mappings).GetEnumerator();
            }

            public X509Certificate Current
            {
                get { return (X509Certificate)_enumerator.Current!; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            bool IEnumerator.MoveNext()
            {
                return MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            void IEnumerator.Reset()
            {
                Reset();
            }
        }
    }
}
