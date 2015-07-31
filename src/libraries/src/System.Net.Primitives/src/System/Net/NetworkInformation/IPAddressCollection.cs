// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Net.NetworkInformation
{
    public class IPAddressCollection : ICollection<IPAddress>
    {
        private Collection<IPAddress> _addresses = new Collection<IPAddress>();

        protected internal IPAddressCollection()
        {
        }

        public virtual void CopyTo(IPAddress[] array, int offset)
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

        public virtual void Add(IPAddress address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }


        internal void InternalAdd(IPAddress address)
        {
            _addresses.Add(address);
        }

        public virtual bool Contains(IPAddress address)
        {
            return _addresses.Contains(address);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public virtual IEnumerator<IPAddress> GetEnumerator()
        {
            return (IEnumerator<IPAddress>)_addresses.GetEnumerator();
        }

        public virtual IPAddress this[int index]
        {
            get
            {
                return (IPAddress)_addresses[index];
            }
        }

        public virtual bool Remove(IPAddress address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }

        public virtual void Clear()
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }
    }
}
