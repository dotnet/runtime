// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides information about a network interface's unicast address.
    /// </summary>
    public abstract class UnicastIPAddressInformation : IPAddressInformation
    {
        /// <summary>
        /// Gets the number of seconds remaining during which this address is the preferred address.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public abstract long AddressPreferredLifetime { get; }

        /// <summary>
        /// Gets the number of seconds remaining during which this address is valid.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public abstract long AddressValidLifetime { get; }

        /// <summary>
        /// Specifies the amount of time remaining on the Dynamic Host Configuration Protocol (DHCP) lease for this IP address.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public abstract long DhcpLeaseLifetime { get; }

        /// <summary>
        /// Gets a value that indicates the state of the duplicate address detection algorithm.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public abstract DuplicateAddressDetectionState DuplicateAddressDetectionState { get; }

        /// <summary>
        /// Gets a value that identifies the source of a unicast IP address prefix.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public abstract PrefixOrigin PrefixOrigin { get; }

        /// <summary>
        /// Gets a value that identifies the source of a unicast IP address suffix.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public abstract SuffixOrigin SuffixOrigin { get; }

        public abstract IPAddress IPv4Mask { get; }

        /// <summary>
        /// The CIDR representation of the subnet mask.
        /// </summary>
        public virtual int PrefixLength
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
        }

        /// <summary>
        /// Convert a CIDR prefix length to a subnet mask "255.255.255.0" format.
        /// </summary>
        /// <param name="prefixLength">Length of the prefix.</param>
        /// <param name="family">AddressFamily for the mask.</param>
        /// <returns>netmask corresponding to prefix length.</returns>
        internal static IPAddress PrefixLengthToSubnetMask(byte prefixLength, AddressFamily family)
        {
            Debug.Assert((0 <= prefixLength) && (prefixLength <= 126));
            Debug.Assert((family == AddressFamily.InterNetwork) || (family == AddressFamily.InterNetworkV6));

            Span<byte> addressBytes = (family == AddressFamily.InterNetwork) ?
                stackalloc byte[4] :
                stackalloc byte[16];
            addressBytes.Clear();

            Debug.Assert(prefixLength <= (addressBytes.Length * 8));

            // Enable bits one at a time from left/high to right/low.
            for (int bit = 0; bit < prefixLength; bit++)
            {
                addressBytes[bit / 8] |= (byte)(0x80 >> (bit % 8));
            }

            return new IPAddress(addressBytes);
        }
    }
}
