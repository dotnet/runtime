// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Net.NetworkInformation
{
    internal class InternalIPAddressCollection : IPAddressCollection
    {
        private readonly Collection<IPAddress> _addresses = new Collection<IPAddress>();

        protected internal InternalIPAddressCollection()
        {
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
    }
}
