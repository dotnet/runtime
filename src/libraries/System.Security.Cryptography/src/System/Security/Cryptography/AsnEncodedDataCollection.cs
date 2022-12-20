// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public sealed class AsnEncodedDataCollection : ICollection
    {
        public AsnEncodedDataCollection()
        {
            _list = new List<AsnEncodedData>();
        }

        public AsnEncodedDataCollection(AsnEncodedData asnEncodedData)
            : this()
        {
            _list.Add(asnEncodedData);
        }

        public int Add(AsnEncodedData asnEncodedData)
        {
            ArgumentNullException.ThrowIfNull(asnEncodedData);

            int indexOfNewItem = _list.Count;
            _list.Add(asnEncodedData);
            return indexOfNewItem;
        }

        public void Remove(AsnEncodedData asnEncodedData)
        {
            ArgumentNullException.ThrowIfNull(asnEncodedData);

            _list.Remove(asnEncodedData);
        }

        public AsnEncodedData this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public AsnEncodedDataEnumerator GetEnumerator()
        {
            return new AsnEncodedDataEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);
            if (Count > array.Length - index)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }

        public void CopyTo(AsnEncodedData[] array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            // Need to do part of the argument validation ourselves as AsnEncodedDataCollection throws
            // ArgumentOutOfRangeException where List<>.CopyTo() throws ArgumentException.

            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);

            _list.CopyTo(array, index);
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        private readonly List<AsnEncodedData> _list;
    }
}
