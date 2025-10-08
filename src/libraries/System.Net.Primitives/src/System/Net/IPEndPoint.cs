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

        internal static bool InternalTryParse(ReadOnlySpan<char> s, [NotNullWhen(true)] out IPEndPoint? result)
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
                    ReadOnlySpan<char> portSpan = s.Slice(addressLength + 1);
                    bool isConvertedToInt;

                    isConvertedToInt = uint.TryParse(portSpan, NumberStyles.None, CultureInfo.InvariantCulture, out port);

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

        internal static bool InternalTryParse(ReadOnlySpan<byte> s, [NotNullWhen(true)] out IPEndPoint? result)
        {
            int addressLength = s.Length;  // If there's no port then send the entire string to the address parser
            int lastColonPos = s.LastIndexOf((byte)':');

            // Look to see if this is an IPv6 address with a port.
            if (lastColonPos > 0)
            {
                if (s[lastColonPos - 1] == (byte)']')
                {
                    addressLength = lastColonPos;
                }
                // Look to see if this is IPv4 with a port (IPv6 will have another colon)
                else if (s.Slice(0, lastColonPos).LastIndexOf((byte)':') == -1)
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
                    ReadOnlySpan<byte> portSpan = s.Slice(addressLength + 1);
                    bool isConvertedToInt;

                    isConvertedToInt = uint.TryParse(portSpan, NumberStyles.None, CultureInfo.InvariantCulture, out port);

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

        /// <summary>Tries to convert the character span to its <see cref="IPEndPoint"/> equivalent.</summary>
        /// <param name="s">A span container the characters representing the <see cref="IPEndPoint"/> to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="IPEndPoint"/> value equivalent to what is contained in <paramref name="s" /> if the conversion succeeded, or default if the conversion failed. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
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

        /// <summary>Converts the UTF-8 span to its <see cref="IPEndPoint"/> equivalent.</summary>
        /// <param name="utf8Text">A span containing the characters representing the <see cref="IPEndPoint"/> to convert.</param>
        /// <returns>contains the <see cref="IPEndPoint"/> value equivalent to what is contained in <paramref name="utf8Text" /> if the conversion succeeded</returns>
        /// <exception cref="FormatException"><paramref name="utf8Text"/> is invalid</exception>
        public static IPEndPoint Parse(ReadOnlySpan<byte> utf8Text)
        {
            if (TryParse(utf8Text, out IPEndPoint? result))
            {
                return result;
            }

            throw new FormatException(SR.bad_endpoint_string);
        }

        /// <summary>Converts the character span to its <see cref="IPEndPoint"/> equivalent.</summary>
        /// <param name="s">A span containing the characters representing the <see cref="IPEndPoint"/> to convert.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>contains the <see cref="IPEndPoint"/> value equivalent to what is contained in <paramref name="s" /> if the conversion succeeded</returns>
        /// <exception cref="FormatException"><paramref name="s"/> is invalid</exception>
        static IPEndPoint ISpanParsable<IPEndPoint>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

        /// <summary>Converts the string to its <see cref="IPEndPoint"/> equivalent.</summary>
        /// <param name="s">A string containing the characters representing the <see cref="IPEndPoint"/> to convert.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>contains the <see cref="IPEndPoint"/> value equivalent to what is contained in <paramref name="s" /> if the conversion succeeded</returns>
        /// <exception cref="FormatException"><paramref name="s"/> is invalid</exception>
        static IPEndPoint IParsable<IPEndPoint>.Parse(string s, IFormatProvider? provider) => Parse(s);

        /// <summary>Converts the UTF-8 span to its <see cref="IPEndPoint"/> equivalent.</summary>
        /// <param name="utf8Text">A Span containing the UTF-8 characters representing the <see cref="IPEndPoint"/> to convert.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="utf8Text" />.</param>
        /// <returns>contains the <see cref="IPEndPoint"/> value equivalent to what is contained in <paramref name="utf8Text" /> if the conversion succeeded</returns>
        /// <exception cref="FormatException"><paramref name="utf8Text"/> is invalid</exception>
        static IPEndPoint IUtf8SpanParsable<IPEndPoint>.Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text);

        /// <summary>Tries to convert the UTF-8 span to its <see cref="IPEndPoint"/> equivalent.</summary>
        /// <param name="utf8Text">A span containing the UTF-8 characters representing the <see cref="IPEndPoint"/> to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="IPEndPoint"/> value equivalent to what is contained in <paramref name="utf8Text" /> if the conversion succeeded, or default if the conversion failed. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="utf8Text" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, [NotNullWhen(true)] out IPEndPoint? result) => InternalTryParse(utf8Text, out result);

        /// <summary>Tries to convert the character span to its <see cref="IPEndPoint"/> equivalent.</summary>
        /// <param name="s">A span container the characters representing the <see cref="IPEndPoint"/> to convert.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">When this method returns, contains the <see cref="IPEndPoint"/> value equivalent to what is contained in <paramref name="s" /> if the conversion succeeded, or default if the conversion failed. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        static bool ISpanParsable<IPEndPoint>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [NotNullWhen(true)] out IPEndPoint? result) => TryParse(s, out result);

        /// <summary>Tries to convert the string to its <see cref="IPEndPoint"/> equivalent.</summary>
        /// <param name="s">A string representing the <see cref="IPEndPoint"/> to convert.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">When this method returns, contains the <see cref="IPEndPoint"/> value equivalent to what is contained in <paramref name="s" /> if the conversion succeeded, or default if the conversion failed. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        static bool IParsable<IPEndPoint>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [NotNullWhen(true)] out IPEndPoint? result)
        {
            if (s is null)
            {
                result = default;
                return false;
            }

            return TryParse(s, out result);
        }

        /// <summary>Tries to convert the UTF-8 span to its <see cref="IPEndPoint"/> equivalent.</summary>
        /// <param name="utf8Text">A span container the characters representing the <see cref="IPEndPoint"/> to convert.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="utf8Text" />.</param>
        /// <param name="result">When this method returns, contains the <see cref="IPEndPoint"/> value equivalent to what is contained in <paramref name="utf8Text" /> if the conversion succeeded, or default if the conversion failed. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="utf8Text" /> was converted successfully; otherwise, false.</returns>
        static bool IUtf8SpanParsable<IPEndPoint>.TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [NotNullWhen(true)] out IPEndPoint? result) => TryParse(utf8Text, out result);

        /// <summary>Returns the string representation of the current instance using the specified format string to define culture-specific formatting.</summary>
        /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
        /// <param name="formatProvider">A format provider that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the current instance.</returns>
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();

        /// <summary>Tries to format the value of the current instance as characters into the provided span of characters.</summary>
        /// <param name="destination">When this method returns, this parameter is filled with this instance formatted characters.</param>
        /// <param name="charsWritten">When this method returns, the number of bytes that were written in <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        public bool TryFormat(Span<char> destination, out int charsWritten) =>
            _address.AddressFamily == AddressFamily.InterNetworkV6 ?
                destination.TryWrite(CultureInfo.InvariantCulture, $"[{_address}]:{_port}", out charsWritten) :
                destination.TryWrite(CultureInfo.InvariantCulture, $"{_address}:{_port}", out charsWritten);

        /// <summary>Tries to format the value of the current instance as UTF-8 bytes into the provided span.</summary>
        /// <param name="utf8Destination">When this method returns, this parameter is filled with this instance formatted UTF68 bytes.</param>
        /// <param name="bytesWritten">When this method returns, the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) =>
            _address.AddressFamily == AddressFamily.InterNetworkV6 ?
                Utf8.TryWrite(utf8Destination, CultureInfo.InvariantCulture, $"[{_address}]:{_port}", out bytesWritten) :
                Utf8.TryWrite(utf8Destination, CultureInfo.InvariantCulture, $"{_address}:{_port}", out bytesWritten);

        /// <summary>Tries to format the value of the current instance as characters into the provided span of characters.</summary>
        /// <param name="destination">When this method returns, this parameter is filled with this instance formatted characters.</param>
        /// <param name="charsWritten">When this method returns, the number of bytes that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            TryFormat(destination, out charsWritten);

        /// <summary>Tries to format the value of the current instance as UTF-8 bytes into the provided span.</summary>
        /// <param name="utf8Destination">When this method returns, this parameter is filled with this instance formatted UTF68 bytes.</param>
        /// <param name="bytesWritten">When this method returns, the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="utf8Destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="utf8Destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            TryFormat(utf8Destination, out bytesWritten);
    }
}
