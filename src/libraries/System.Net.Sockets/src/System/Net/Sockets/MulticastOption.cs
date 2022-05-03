// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    // Contains option values for IP multicast packets.
    public class MulticastOption
    {
        private IPAddress _group;
        private IPAddress? _localAddress;
        private int _ifIndex;

        // Creates a new instance of the MulticastOption class with the specified IP address
        // group and local address.
        public MulticastOption(IPAddress group, IPAddress mcint)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(mcint);

            _group = group;
            LocalAddress = mcint;
        }

        public MulticastOption(IPAddress group, int interfaceIndex)
        {
            ArgumentNullException.ThrowIfNull(group);

            if (interfaceIndex < 0 || interfaceIndex > 0x00FFFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(interfaceIndex));
            }

            _group = group;
            _ifIndex = interfaceIndex;
        }

        // Creates a new version of the MulticastOption class for the specified group.
        public MulticastOption(IPAddress group)
        {
            ArgumentNullException.ThrowIfNull(group);

            _group = group;

            LocalAddress = IPAddress.Any;
        }

        // Sets the IP address of a multicast group.
        public IPAddress Group
        {
            get => _group;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _group = value;
            }
        }

        // Sets the local address of a multicast group.
        public IPAddress? LocalAddress
        {
            get => _localAddress;
            set
            {
                _ifIndex = 0;
                _localAddress = value;
            }
        }

        public int InterfaceIndex
        {
            get => _ifIndex;
            set
            {
                if (value < 0 || value > 0x00FFFFFF)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
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
            ArgumentNullException.ThrowIfNull(group);

            if (ifindex < 0 || ifindex > 0x00000000FFFFFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(ifindex));
            }

            _group = group;
            InterfaceIndex = ifindex;
        }

        // Creates a new version of the MulticastOption class for the specified
        // group.
        public IPv6MulticastOption(IPAddress group)
        {
            ArgumentNullException.ThrowIfNull(group);

            _group = group;
            InterfaceIndex = 0;
        }

        // Sets the IP address of a multicast group.
        public IPAddress Group
        {
            get => _group;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _group = value;
            }
        }

        // Sets the interface index.
        public long InterfaceIndex
        {
            get => _interface;
            set
            {
                if (value < 0 || value > 0x00000000FFFFFFFF)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _interface = value;
            }
        }
    }
}
