// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509Certificate2Enumerator : IEnumerator
    {
        private readonly IEnumerator _enumerator;

        internal X509Certificate2Enumerator(X509Certificate2Collection collection)
        {
            Debug.Assert(collection != null);

            _enumerator = ((IEnumerable)collection).GetEnumerator();
        }

        public X509Certificate2 Current
        {
            get { return (X509Certificate2)_enumerator.Current; }
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
