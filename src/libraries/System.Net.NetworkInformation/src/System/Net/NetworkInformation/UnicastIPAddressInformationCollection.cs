// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Net.NetworkInformation
{
    public class UnicastIPAddressInformationCollection : ICollection<UnicastIPAddressInformation>
    {
        private readonly List<UnicastIPAddressInformation> _addresses =
            new List<UnicastIPAddressInformation>();

        protected internal UnicastIPAddressInformationCollection()
        {
        }

        public virtual void CopyTo(UnicastIPAddressInformation[] array, int offset)
        {
            _addresses.CopyTo(array, offset);
        }

        public virtual int Count
        {
            get
            {
                return _addresses.Count;
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public virtual void Add(UnicastIPAddressInformation address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }

        internal void InternalAdd(UnicastIPAddressInformation address)
        {
            _addresses.Add(address);
        }

        public virtual bool Contains(UnicastIPAddressInformation address)
        {
            return _addresses.Contains(address);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public virtual IEnumerator<UnicastIPAddressInformation> GetEnumerator()
        {
            return _addresses.GetEnumerator();
        }

        public virtual UnicastIPAddressInformation this[int index]
        {
            get
            {
                return _addresses[index];
            }
        }

        public virtual bool Remove(UnicastIPAddressInformation address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }

        public virtual void Clear()
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }
    }
}
