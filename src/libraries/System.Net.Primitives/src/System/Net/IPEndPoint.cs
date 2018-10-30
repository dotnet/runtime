// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Globalization;

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///       Provides an IP address.
    ///    </para>
    /// </devdoc>
    public class IPEndPoint : EndPoint
    {
        /// <devdoc>
        ///    <para>
        ///       Specifies the minimum acceptable value for the <see cref='System.Net.IPEndPoint.Port'/>
        ///       property.
        ///    </para>
        /// </devdoc>
        public const int MinPort = 0x00000000;

        /// <devdoc>
        ///    <para>
        ///       Specifies the maximum acceptable value for the <see cref='System.Net.IPEndPoint.Port'/>
        ///       property.
        ///    </para>
        /// </devdoc>
        public const int MaxPort = 0x0000FFFF;

        private IPAddress _address;
        private int _port;

        internal const int AnyPort = MinPort;

        internal static IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        internal static IPEndPoint IPv6Any = new IPEndPoint(IPAddress.IPv6Any, AnyPort);

        public override AddressFamily AddressFamily
        {
            get
            {
                // IPv6 Changes: Always delegate this to the address we are wrapping.
                return _address.AddressFamily;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the IPEndPoint class with the specified address and port.
        ///    </para>
        /// </devdoc>
        public IPEndPoint(long address, int port)
        {
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            _port = port;
            _address = new IPAddress(address);
        }

        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the IPEndPoint class with the specified address and port.
        ///    </para>
        /// </devdoc>
        public IPEndPoint(IPAddress address, int port)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            _port = port;
            _address = address;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the IP address.
        ///    </para>
        /// </devdoc>
        public IPAddress Address
        {
            get
            {
                return _address;
            }
            set
            {
                _address = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the port.
        ///    </para>
        /// </devdoc>
        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (!TcpValidationHelpers.ValidatePortNumber(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _port = value;
            }
        }

        public static bool TryParse(string s, out IPEndPoint result)
        {
            return TryParse(s.AsSpan(), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out IPEndPoint result)
        {   
            int addressLength = s.Length;  // If there's no port then send the entire string to the address parser
            int lastColonPos = s.LastIndexOf(':');

            // Look to see if this is an IPv6 address with a port.
            if (lastColonPos > 0)
            {
                if (s[lastColonPos - 1] == ']')
                {
                    addressLength = lastColonPos;
                }
                // Look to see if this is IPv4 with a port (IPv6 will have another colon)
                else if (s.Slice(0, lastColonPos).LastIndexOf(':') == -1)
                {
                    addressLength = lastColonPos;
                }
            }

            if (IPAddress.TryParse(s.Slice(0, addressLength), out IPAddress address))
            {
                uint port = 0;
                if (addressLength == s.Length ||
                    (uint.TryParse(s.Slice(addressLength + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port) && port <= MaxPort))
                    
                {
                    result = new IPEndPoint(address, (int)port);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static IPEndPoint Parse(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return Parse(s.AsSpan());
        }

        public static IPEndPoint Parse(ReadOnlySpan<char> s)
        {
            if (TryParse(s, out IPEndPoint result))
            {
                return result;
            }

            throw new FormatException(SR.bad_endpoint_string);
        }

        public override string ToString()
        {
            string format = (_address.AddressFamily == AddressFamily.InterNetworkV6) ? "[{0}]:{1}" : "{0}:{1}";
            return string.Format(format, _address.ToString(), Port.ToString(NumberFormatInfo.InvariantInfo));
        }

        public override SocketAddress Serialize()
        {
            // Let SocketAddress do the bulk of the work
            return new SocketAddress(Address, Port);
        }

        public override EndPoint Create(SocketAddress socketAddress)
        {
            // Validate SocketAddress
            if (socketAddress.Family != this.AddressFamily)
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidAddressFamily, socketAddress.Family.ToString(), this.GetType().FullName, this.AddressFamily.ToString()), nameof(socketAddress));
            }
            if (socketAddress.Size < 8)
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidSocketAddressSize, socketAddress.GetType().FullName, this.GetType().FullName), nameof(socketAddress));
            }

            return socketAddress.GetIPEndPoint();
        }

        public override bool Equals(object comparand)
        {
            return comparand is IPEndPoint other && other._address.Equals(_address) && other._port == _port;
        }

        public override int GetHashCode()
        {
            return _address.GetHashCode() ^ _port;
        }
    }
}
