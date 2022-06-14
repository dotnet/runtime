// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509ChainElementCollection : ICollection, IEnumerable<X509ChainElement>
    {
        private readonly X509ChainElement[] _elements;

        internal X509ChainElementCollection()
        {
            _elements = Array.Empty<X509ChainElement>();
        }

        internal X509ChainElementCollection(X509ChainElement[] chainElements)
        {
            Debug.Assert(chainElements != null, "chainElements != null");
            _elements = chainElements;
        }

        public int Count
        {
            get { return _elements.Length; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        public X509ChainElement this[int index]
        {
            get
            {
                if (index < 0)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _elements.Length);

                return _elements[index];
            }
        }

        public void CopyTo(X509ChainElement[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            ArgumentOutOfRangeException.ThrowIf(index < 0 || index >= array.Length);
            if (index + Count > array.Length)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }

        public X509ChainElementEnumerator GetEnumerator()
        {
            return new X509ChainElementEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new X509ChainElementEnumerator(this);
        }

        IEnumerator<X509ChainElement> IEnumerable<X509ChainElement>.GetEnumerator() => GetEnumerator();
    }
}
