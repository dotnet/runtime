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

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509ChainElementCollection : ICollection, IEnumerable
    {
        public int Count
        {
            get { return _elements.Length; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public Object SyncRoot
        {
            get { return this; }
        }

        public X509ChainElement this[int index]
        {
            get
            {
                if (index < 0)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                if (index >= _elements.Length)
                    throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);

                return _elements[index];
            }
        }

        public void CopyTo(X509ChainElement[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
            return;
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
            if (index + this.Count > array.Length)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            for (int i = 0; i < this.Count; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }

            return;
        }

        public X509ChainElementEnumerator GetEnumerator()
        {
            return new X509ChainElementEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new X509ChainElementEnumerator(this);
        }

        internal X509ChainElementCollection()
        {
            _elements = new X509ChainElement[0];
            return;
        }

        internal X509ChainElementCollection(IEnumerable<X509ChainElement> chainElements)
        {
            _elements = new LowLevelList<X509ChainElement>(chainElements).ToArray();
            return;
        }


        private X509ChainElement[] _elements;
    }
}

