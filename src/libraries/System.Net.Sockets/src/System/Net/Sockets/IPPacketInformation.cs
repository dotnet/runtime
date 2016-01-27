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

        public static bool operator ==(IPPacketInformation left, IPPacketInformation right)
        {
            return left._networkInterface == right._networkInterface &&
                (left._address == null && right._address == null || left._address.Equals(right._address));
        }

        public static bool operator !=(IPPacketInformation left, IPPacketInformation right)
        {
            return !(left == right);
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
