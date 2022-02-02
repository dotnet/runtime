// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.Sockets
{
    public struct IPPacketInformation : IEquatable<IPPacketInformation>
    {
        private readonly IPAddress _address;
        private readonly int _networkInterface;

        internal IPPacketInformation(IPAddress address, int networkInterface)
        {
            _address = address;
            _networkInterface = networkInterface;
        }

        public IPAddress Address => _address;

        public int Interface => _networkInterface;

        public static bool operator ==(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2) =>
            packetInformation1.Equals(packetInformation2);

        public static bool operator !=(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2) =>
            !packetInformation1.Equals(packetInformation2);

        public override bool Equals([NotNullWhen(true)] object? comparand) =>
            comparand is IPPacketInformation other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(IPPacketInformation other) =>
            _networkInterface == other._networkInterface &&
            (_address is null ? other._address is null : _address.Equals(other._address));

        public override int GetHashCode() =>
            unchecked(_networkInterface.GetHashCode() * (int)0xA5555529) + (_address?.GetHashCode() ?? 0);
    }
}
