// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class CmsRecipientCollection : ICollection
    {
        public CmsRecipientCollection()
        {
            _recipients = new List<CmsRecipient>();
        }

        public CmsRecipientCollection(CmsRecipient recipient)
        {
            _recipients = new List<CmsRecipient>(1);
            _recipients.Add(recipient);
        }

        public CmsRecipientCollection(SubjectIdentifierType recipientIdentifierType, X509Certificate2Collection certificates)
        {
            if (certificates == null)
                throw new NullReferenceException(); //Desktop compat: this is the wrong exception to throw but it is the compatible one. 

            _recipients = new List<CmsRecipient>(certificates.Count);
            for (int index = 0; index < certificates.Count; index++)
            {
                _recipients.Add(new CmsRecipient(recipientIdentifierType, certificates[index]));
            }
        }

        public CmsRecipient this[int index]
        {
            get
            {
                if (index < 0 || index >= _recipients.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);

                return _recipients[index];
            }
        }

        public int Count
        {
            get
            {
                return _recipients.Count;
            }
        }

        public int Add(CmsRecipient recipient)
        {
            if (recipient == null)
                throw new ArgumentNullException(nameof(recipient));

            int indexOfNewItem = _recipients.Count;
            _recipients.Add(recipient);
            return indexOfNewItem;
        }

        public void Remove(CmsRecipient recipient)
        {
            if (recipient == null)
                throw new ArgumentNullException(nameof(recipient));

            _recipients.Remove(recipient);
        }

        public CmsRecipientEnumerator GetEnumerator()
        {
            return new CmsRecipientEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new CmsRecipientEnumerator(this);
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

        public void CopyTo(CmsRecipient[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            if (index > array.Length - Count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            _recipients.CopyTo(array, index);
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }

        private readonly List<CmsRecipient> _recipients;
    }
}


