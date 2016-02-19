// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed class OidCollection : ICollection
    {
        public OidCollection()
        {
            _list = new LowLevelListWithIList<Oid>();
        }

        public int Add(Oid oid)
        {
            int count = _list.Count;
            _list.Add(oid);
            return count;
        }

        public Oid this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        // Indexer using an OID friendly name or value.
        public Oid this[String oid]
        {
            get
            {
                // If we were passed the friendly name, retrieve the value String.
                String oidValue = OidLookup.ToOid(oid, OidGroup.All, fallBackToAllGroups: false);
                if (oidValue == null)
                {
                    oidValue = oid;
                }
                foreach (Oid entry in _list)
                {
                    if (entry.Value == oidValue)
                        return entry;
                }
                return null;
            }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public OidEnumerator GetEnumerator()
        {
            return new OidEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new OidEnumerator(this);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            if (index + this.Count > array.Length)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            for (int i = 0; i < this.Count; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }

        public void CopyTo(Oid[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        Object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }

        private LowLevelListWithIList<Oid> _list;
    }
}
