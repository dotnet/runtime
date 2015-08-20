// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509Certificate2Enumerator : IEnumerator
    {
        // This is a mutable struct enumerator, so don't mark it as readonly.
        private List<X509Certificate>.Enumerator _enumerator;

        internal X509Certificate2Enumerator(X509Certificate2Collection collection)
        {
            Debug.Assert(collection != null);

            collection.GetEnumerator(out _enumerator);
        }

        public X509Certificate2 Current
        {
            // Call the struct enumerator's IEnumerator.Current implementation, which has the
            // behavior we want of throwing InvalidOperationException when the enumerator
            // hasn't been started or has ended, without boxing.
            get { return (X509Certificate2)(EnumeratorHelper.GetCurrent(ref _enumerator)); }
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
            // Call Reset on the struct enumerator without boxing.
            EnumeratorHelper.Reset(ref _enumerator);
        }

        void IEnumerator.Reset()
        {
            Reset();
        }
    }
}
