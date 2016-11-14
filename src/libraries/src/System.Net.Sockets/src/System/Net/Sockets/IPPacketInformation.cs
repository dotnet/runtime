// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace System.Net.Sockets
{
    public struct IPPacketInformation
    {
        private IPAddress _address;
        private int _networkInterface;

        internal IPPacketInformation(IPAddress address, int networkInterface)
        {
            _address = address;
            _networkInterface = networkInterface;
        }

        public IPAddress Address
        {
            get
            {
                return _address;
            }
        }

        public int Interface
        {
            get
            {
                return _networkInterface;
            }
        }

        public static bool operator ==(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2)
        {
            return packetInformation1._networkInterface == packetInformation2._networkInterface &&
                (packetInformation1._address == null && packetInformation2._address == null || packetInformation1._address.Equals(packetInformation2._address));
        }

        public static bool operator !=(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2)
        {
            return !(packetInformation1 == packetInformation2);
        }

        public override bool Equals(object comparand)
        {
            return comparand is IPPacketInformation && this == (IPPacketInformation)comparand;
        }

        public override int GetHashCode()
        {
            return unchecked(_networkInterface.GetHashCode() * (int)0xA5555529) +
                (_address == null ? 0 : _address.GetHashCode());
        }
    }
}
