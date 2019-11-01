// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Net.NetworkInformation
{
    public class GatewayIPAddressInformationCollection : ICollection<GatewayIPAddressInformation>
    {
        private readonly List<GatewayIPAddressInformation> _addresses;

        protected internal GatewayIPAddressInformationCollection()
        {
            _addresses = new List<GatewayIPAddressInformation>();
        }

        internal GatewayIPAddressInformationCollection(List<GatewayIPAddressInformation> addresses)
        {
            _addresses = addresses;
        }

        public virtual void CopyTo(GatewayIPAddressInformation[] array, int offset)
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

        public virtual GatewayIPAddressInformation this[int index]
        {
            get
            {
                return _addresses[index];
            }
        }

        public virtual void Add(GatewayIPAddressInformation address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }

        internal void InternalAdd(GatewayIPAddressInformation address)
        {
            _addresses.Add(address);
        }

        public virtual bool Contains(GatewayIPAddressInformation address)
        {
            return _addresses.Contains(address);
        }

        public virtual IEnumerator<GatewayIPAddressInformation> GetEnumerator()
        {
            return _addresses.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public virtual bool Remove(GatewayIPAddressInformation address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }

        public virtual void Clear()
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }
    }
}
