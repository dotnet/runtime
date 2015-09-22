// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            return packetInformation1.Equals(packetInformation2);
        }

        public static bool operator !=(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2)
        {
            return !packetInformation1.Equals(packetInformation2);
        }

        public override bool Equals(object comparand)
        {
            if ((object)comparand == null || !(comparand is IPPacketInformation))
            {
                return false;
            }

            IPPacketInformation obj = (IPPacketInformation)comparand;
            return _address.Equals(obj._address) && _networkInterface == obj._networkInterface;
        }

        public override int GetHashCode()
        {
            return _address.GetHashCode() + _networkInterface.GetHashCode();
        }
    }
}
