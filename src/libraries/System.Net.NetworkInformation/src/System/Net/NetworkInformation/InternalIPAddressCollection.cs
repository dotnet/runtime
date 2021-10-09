// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net.NetworkInformation
{
    internal sealed class InternalIPAddressCollection : IPAddressCollection
    {
        private readonly List<IPAddress> _addresses;

        internal InternalIPAddressCollection()
        {
            _addresses = new List<IPAddress>();
        }

        internal InternalIPAddressCollection(List<IPAddress> addresses)
        {
            _addresses = addresses;
        }

        public override void CopyTo(IPAddress[] array, int offset)
        {
            _addresses.CopyTo(array, offset);
        }

        public override int Count
        {
            get
            {
                return _addresses.Count;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public override void Add(IPAddress address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }

        internal void InternalAdd(IPAddress address)
        {
            _addresses.Add(address);
        }

        public override bool Contains(IPAddress address)
        {
            return _addresses.Contains(address);
        }

        public override IPAddress this[int index]
        {
            get
            {
                return _addresses[index];
            }
        }

        public override IEnumerator<IPAddress> GetEnumerator()
        {
            return _addresses.GetEnumerator();
        }
    }
}
