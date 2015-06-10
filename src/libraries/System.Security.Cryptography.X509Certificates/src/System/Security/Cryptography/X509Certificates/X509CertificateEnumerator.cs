// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public partial class X509CertificateCollection : ICollection, IEnumerable, IList
    {
        public class X509CertificateEnumerator : IEnumerator
        {
            public X509CertificateEnumerator(X509CertificateCollection mappings)
            {
                _baseEnumerator = mappings.GetEnumerator();
            }

            public X509Certificate Current
            {
                get { return (X509Certificate)(_baseEnumerator.Current); }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return _baseEnumerator.Current; }
            }

            public bool MoveNext()
            {
                return _baseEnumerator.MoveNext();
            }

            bool IEnumerator.MoveNext()
            {
                return _baseEnumerator.MoveNext();
            }

            public void Reset()
            {
                _baseEnumerator.Reset();
            }

            void IEnumerator.Reset()
            {
                _baseEnumerator.Reset();
            }

            internal X509CertificateEnumerator(IEnumerator baseEnumerator)
            {
                _baseEnumerator = baseEnumerator;
            }

            private IEnumerator _baseEnumerator;
        }
    }
}

