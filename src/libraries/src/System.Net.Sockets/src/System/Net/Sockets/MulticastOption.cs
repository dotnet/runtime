// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace System.Net.Sockets
{
    // Contains option values for IP multicast packets.
    public class MulticastOption
    {
        private IPAddress _group;
        private IPAddress _localAddress;
        private int _ifIndex;

        // Creates a new instance of the MulticastOption class with the specified IP address
        // group and local address.
        public MulticastOption(IPAddress group, IPAddress mcint)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }

            if (mcint == null)
            {
                throw new ArgumentNullException("mcint");
            }

            Group = group;
            LocalAddress = mcint;
        }

        public MulticastOption(IPAddress group, int interfaceIndex)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }

            if (interfaceIndex < 0 || interfaceIndex > 0x00FFFFFF)
            {
                throw new ArgumentOutOfRangeException("interfaceIndex");
            }

            Group = group;
            _ifIndex = interfaceIndex;
        }

        // Creates a new version of the MulticastOption class for the specified group.
        public MulticastOption(IPAddress group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }

            Group = group;

            LocalAddress = IPAddress.Any;
        }

        // Sets the IP address of a multicast group.
        public IPAddress Group
        {
            get
            {
                return _group;
            }
            set
            {
                _group = value;
            }
        }

        // Sets the local address of a multicast group.
        public IPAddress LocalAddress
        {
            get
            {
                return _localAddress;
            }
            set
            {
                _ifIndex = 0;
                _localAddress = value;
            }
        }

        public int InterfaceIndex
        {
            get
            {
                return _ifIndex;
            }
            set
            {
                if (value < 0 || value > 0x00FFFFFF)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _localAddress = null;
                _ifIndex = value;
            }
        }
    }

    // Contains option values for joining an IPv6 multicast group.
    public class IPv6MulticastOption
    {
        private IPAddress _group;
        private long _interface;

        // Creates a new instance of the MulticaseOption class with the specified IP
        // address group and local address.
        public IPv6MulticastOption(IPAddress group, long ifindex)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }

            if (ifindex < 0 || ifindex > 0x00000000FFFFFFFF)
            {
                throw new ArgumentOutOfRangeException("ifindex");
            }

            Group = group;
            InterfaceIndex = ifindex;
        }

        // Creates a new version of the MulticastOption class for the specified
        // group.
        public IPv6MulticastOption(IPAddress group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }

            Group = group;
            InterfaceIndex = 0;
        }

        // Sets the IP address of a multicast group.
        public IPAddress Group
        {
            get
            {
                return _group;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _group = value;
            }
        }

        // Sets the interface index.
        public long InterfaceIndex
        {
            get
            {
                return _interface;
            }
            set
            {
                if (value < 0 || value > 0x00000000FFFFFFFF)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _interface = value;
            }
        }
    }
}
