// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Net.NetworkInformation
{
    public class IPAddressInformationCollection : ICollection<IPAddressInformation>
    {
        private readonly List<IPAddressInformation> _addresses = new List<IPAddressInformation>();

        internal IPAddressInformationCollection()
        {
        }

        public virtual void CopyTo(IPAddressInformation[] array, int offset)
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

        public virtual void Add(IPAddressInformation address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }

        internal void InternalAdd(IPAddressInformation address)
        {
            _addresses.Add(address);
        }

        public virtual bool Contains(IPAddressInformation address)
        {
            return _addresses.Contains(address);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public virtual IEnumerator<IPAddressInformation> GetEnumerator()
        {
            return _addresses.GetEnumerator();
        }

        public virtual IPAddressInformation this[int index]
        {
            get
            {
                return _addresses[index];
            }
        }

        public virtual bool Remove(IPAddressInformation address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }

        public virtual void Clear()
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }
    }
}
