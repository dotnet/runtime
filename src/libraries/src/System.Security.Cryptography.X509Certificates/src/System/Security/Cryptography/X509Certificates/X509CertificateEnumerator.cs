// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates
{
    public partial class X509CertificateCollection : ICollection, IEnumerable, IList
    {
        public class X509CertificateEnumerator : IEnumerator
        {
            // This is a mutable struct enumerator, so don't mark it as readonly.
            private List<X509Certificate>.Enumerator _enumerator;

            public X509CertificateEnumerator(X509CertificateCollection mappings)
            {
                if (mappings == null)
                    throw new ArgumentNullException(nameof(mappings));

                mappings.GetEnumerator(out _enumerator);
            }

            public X509Certificate Current
            {
                // Call the struct enumerator's IEnumerator.Current implementation, which has the
                // behavior we want of throwing InvalidOperationException when the enumerator
                // hasn't been started or has ended, without boxing.
                get { return (X509Certificate)(EnumeratorHelper.GetCurrent(ref _enumerator)); }
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

    internal static class EnumeratorHelper
    {
        /// <summary>
        /// Allows calling List<T>.Enumerator's explicit implementation of IEnumerator.Current without boxing.
        /// We call the non-generic IEnumerator.Current implementation because it handles throwing InvalidOperationException
        /// when the enumerator hasn't been started or has ended, which is the behavior we want.
        /// </summary>
        internal static object GetCurrent<TEnumerator>(ref TEnumerator enumerator) where TEnumerator : IEnumerator
        {
            return enumerator.Current;
        }

        /// <summary>
        /// Allows calling List<T>.Enumerator's explicit implementation of IEnumerator.Reset without boxing.
        /// </summary>
        internal static void Reset<TEnumerator>(ref TEnumerator enumerator) where TEnumerator : IEnumerator
        {
            enumerator.Reset();
        }
    }
}
