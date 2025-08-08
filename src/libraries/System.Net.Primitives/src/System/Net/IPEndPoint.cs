// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System.Net
{
    /// <summary>
    /// Provides an IP address.
    /// </summary>
    public class IPEndPoint : EndPoint, ISpanFormattable, ISpanParsable<IPEndPoint>, IUtf8SpanFormattable, IUtf8SpanParsable<IPEndPoint>
    {
        /// <summary>
        /// Specifies the minimum acceptable value for the <see cref='System.Net.IPEndPoint.Port'/> property.
        /// </summary>
        public const int MinPort = 0x00000000;

        /// <summary>
        /// Specifies the maximum acceptable value for the <see cref='System.Net.IPEndPoint.Port'/> property.
        /// </summary>
        public const int MaxPort = 0x0000FFFF;

        private IPAddress _address;
        private int _port;

        public override AddressFamily AddressFamily => _address.AddressFamily;

        /// <summary>
        /// Creates a new instance of the IPEndPoint class with the specified address and port.
        /// </summary>
        public IPEndPoint(long address, int port)
        {
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            _port = port;
            _address = new IPAddress(address);
        }

        /// <summary>
        /// Creates a new instance of the IPEndPoint class with the specified address and port.
        /// </summary>
        public IPEndPoint(IPAddress address, int port)
        {
            ArgumentNullException.ThrowIfNull(address);

            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            _port = port;
            _address = address;
        }

        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public IPAddress Address
        {
            get => _address;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _address = value;
            }
        }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        public int Port
        {
            get => _port;
            set
            {
                if (!TcpValidationHelpers.ValidatePortNumber(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _port = value;
            }
        }

        public static bool TryParse(string s, [NotNullWhen(true)] out IPEndPoint? result)
        {
            return TryParse(s.AsSpan(), out result);
        }

        internal static bool InternalTryParse<TChar>(ReadOnlySpan<TChar> s, [NotNullWhen(true)] out IPEndPoint? result)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));

            int addressLength = s.Length;  // If there's no port then send the entire string to the address parser
            int lastColonPos = s.LastIndexOf(TChar.CreateTruncating(':'));

            // Look to see if this is an IPv6 address with a port.
            if (lastColonPos > 0)
            {
                if (s[lastColonPos - 1] == TChar.CreateTruncating(']'))
                {
                    addressLength = lastColonPos;
                }
                // Look to see if this is IPv4 with a port (IPv6 will have another colon)
                else if (s.Slice(0, lastColonPos).LastIndexOf(TChar.CreateTruncating(':')) == -1)
                {
                    addressLength = lastColonPos;
                }
            }

            IPAddress? address = IPAddressParser.Parse(s.Slice(0, addressLength), true);
            if (address is not null)
            {
                if (addressLength == s.Length)
                {
                    result = new IPEndPoint(address, 0);
                    return true;
                }
                else
                {
                    uint port;
                    ReadOnlySpan<TChar> portSpan = s.Slice(addressLength + 1);
                    bool isConvertedToInt;

                    if (typeof(TChar) == typeof(byte))
                    {
                        isConvertedToInt = uint.TryParse(MemoryMarshal.Cast<TChar, byte>(portSpan), NumberStyles.None, CultureInfo.InvariantCulture, out port);
                    }
                    else
                    {
                        isConvertedToInt = uint.TryParse(MemoryMarshal.Cast<TChar, char>(portSpan), NumberStyles.None, CultureInfo.InvariantCulture, out port);
                    }

                    if (isConvertedToInt && port <= MaxPort)
                    {
                        result = new IPEndPoint(address, (int)port);
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        public static bool TryParse(ReadOnlySpan<char> s, [NotNullWhen(true)] out IPEndPoint? result) => InternalTryParse(s, out result);

        public static IPEndPoint Parse(string s)
        {
            ArgumentNullException.ThrowIfNull(s);

            return Parse(s.AsSpan());
        }

        public static IPEndPoint Parse(ReadOnlySpan<char> s)
        {
            if (TryParse(s, out IPEndPoint? result))
            {
                return result;
            }

            throw new FormatException(SR.bad_endpoint_string);
        }

        public override string ToString() =>
            _address.AddressFamily == AddressFamily.InterNetworkV6 ?
                string.Create(NumberFormatInfo.InvariantInfo, $"[{_address}]:{_port}") :
                string.Create(NumberFormatInfo.InvariantInfo, $"{_address}:{_port}");

        public override SocketAddress Serialize() => new SocketAddress(Address, Port);

        public override EndPoint Create(SocketAddress socketAddress)
        {
            ArgumentNullException.ThrowIfNull(socketAddress);

            if (socketAddress.Family is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidAddressFamily, socketAddress.Family.ToString(), GetType().FullName), nameof(socketAddress));
            }

            int minSize = AddressFamily == AddressFamily.InterNetworkV6 ? SocketAddress.IPv6AddressSize : SocketAddress.IPv4AddressSize;
            if (socketAddress.Size < minSize)
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidSocketAddressSize, socketAddress.GetType().FullName, GetType().FullName), nameof(socketAddress));
            }

            return socketAddress.GetIPEndPoint();
        }

        public override bool Equals([NotNullWhen(true)] object? comparand)
        {
            return comparand is IPEndPoint other && other._address.Equals(_address) && other._port == _port;
        }

        public override int GetHashCode()
        {
            return _address.GetHashCode() ^ _port;
        }

        public static IPEndPoint Parse(ReadOnlySpan<byte> utf8Text)
        {
            if (TryParse(utf8Text, out IPEndPoint? result))
            {
                return result;
            }

            throw new FormatException(SR.bad_endpoint_string);
        }

        static IPEndPoint ISpanParsable<IPEndPoint>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

        static IPEndPoint IParsable<IPEndPoint>.Parse(string s, IFormatProvider? provider) => Parse(s);

        static IPEndPoint IUtf8SpanParsable<IPEndPoint>.Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text);

        public static bool TryParse(ReadOnlySpan<byte> utf8Text, [NotNullWhen(true)] out IPEndPoint? result) => InternalTryParse(utf8Text, out result);

        static bool ISpanParsable<IPEndPoint>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [NotNullWhen(true)] out IPEndPoint? result) => TryParse(s, out result);

        static bool IParsable<IPEndPoint>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [NotNullWhen(true)] out IPEndPoint? result)
        {
            if (s is null)
            {
                result = default;
                return false;
            }

            return TryParse(s, out result);
        }

        static bool IUtf8SpanParsable<IPEndPoint>.TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [NotNullWhen(true)] out IPEndPoint? result) => TryParse(utf8Text, out result);

        string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();

        public bool TryFormat(Span<char> destination, out int charsWritten) =>
            _address.AddressFamily == AddressFamily.InterNetworkV6 ?
                destination.TryWrite(CultureInfo.InvariantCulture, $"[{_address}]:{_port}", out charsWritten) :
                destination.TryWrite(CultureInfo.InvariantCulture, $"{_address}:{_port}", out charsWritten);

        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) =>
            _address.AddressFamily == AddressFamily.InterNetworkV6 ?
                Utf8.TryWrite(utf8Destination, CultureInfo.InvariantCulture, $"[{_address}]:{_port}", out bytesWritten) :
                Utf8.TryWrite(utf8Destination, CultureInfo.InvariantCulture, $"{_address}:{_port}", out bytesWritten);

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            TryFormat(destination, out charsWritten);

        bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            TryFormat(utf8Destination, out bytesWritten);
    }
}
