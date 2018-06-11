// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class RecipientInfoCollection : ICollection
    {
        internal RecipientInfoCollection()
        {
            _recipientInfos = Array.Empty<RecipientInfo>();
        }

        internal RecipientInfoCollection(RecipientInfo recipientInfo)
        {
            _recipientInfos = new RecipientInfo[] { recipientInfo };
        }

        internal RecipientInfoCollection(ICollection<RecipientInfo> recipientInfos)
        {
            _recipientInfos = new RecipientInfo[recipientInfos.Count];
            recipientInfos.CopyTo(_recipientInfos, 0);
        }

        public RecipientInfo this[int index]
        {
            get
            {
                if (index < 0 || index >= _recipientInfos.Length)
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
                return _recipientInfos[index];
            }
        }

        public int Count
        {
            get
            {
                return _recipientInfos.Length;
            }
        }

        public RecipientInfoEnumerator GetEnumerator()
        {
            return new RecipientInfoEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((RecipientInfoCollection)this).GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            if (index > array.Length - Count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }

        public void CopyTo(RecipientInfo[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            _recipientInfos.CopyTo(array, index);
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

        private readonly RecipientInfo[] _recipientInfos;
    }
}


