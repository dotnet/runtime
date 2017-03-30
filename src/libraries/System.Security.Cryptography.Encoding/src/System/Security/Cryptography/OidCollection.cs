// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed class OidCollection : ICollection
    {
        public OidCollection()
        {
            _list = new List<Oid>();
        }

        public int Add(Oid oid)
        {
            int count = _list.Count;
            _list.Add(oid);
            return count;
        }

        public Oid this[int index] => _list[index];

        // Indexer using an OID friendly name or value.
        public Oid this[string oid]
        {
            get
            {
                // If we were passed the friendly name, retrieve the value String.
                string oidValue = OidLookup.ToOid(oid, OidGroup.All, fallBackToAllGroups: false);
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

        public int Count => _list.Count;

        public OidEnumerator GetEnumerator() => new OidEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            if (index + Count > array.Length)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }

        public void CopyTo(Oid[] array, int index)
        {
            // Need to do part of the argument validation ourselves as OidCollection throws
            // ArgumentOutOfRangeException where List<>.CopyTo() throws ArgumentException.

            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);

            _list.CopyTo(array, index);
        }

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        private readonly List<Oid> _list;
    }
}
