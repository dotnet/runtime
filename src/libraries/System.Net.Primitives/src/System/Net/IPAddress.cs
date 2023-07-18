// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#pragma warning disable SA1648 // TODO: https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3595

namespace System.Net
{
    /// <devdoc>
    ///   <para>
    ///     Provides an Internet Protocol (IP) address.
    ///   </para>
    /// </devdoc>
    public class IPAddress : ISpanFormattable, ISpanParsable<IPAddress>, IUtf8SpanFormattable
    {
        public static readonly IPAddress Any = new ReadOnlyIPAddress(new byte[] { 0, 0, 0, 0 });
        public static readonly IPAddress Loopback = new ReadOnlyIPAddress(new byte[] { 127, 0, 0, 1 });
        public static readonly IPAddress Broadcast = new ReadOnlyIPAddress(new byte[] { 255, 255, 255, 255 });
        public static readonly IPAddress None = Broadcast;

        internal const uint LoopbackMaskHostOrder = 0xFF000000;

        public static readonly IPAddress IPv6Any = new IPAddress((ReadOnlySpan<byte>)new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0);
        public static readonly IPAddress IPv6Loopback = new IPAddress((ReadOnlySpan<byte>)new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, 0);
        public static readonly IPAddress IPv6None = IPv6Any;

        private static readonly IPAddress s_loopbackMappedToIPv6 = new IPAddress((ReadOnlySpan<byte>)new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 127, 0, 0, 1 }, 0);

        /// <summary>
        /// For IPv4 addresses, this field stores the Address.
        /// For IPv6 addresses, this field stores the ScopeId.
        /// Instead of accessing this field directly, use the <see cref="PrivateAddress"/> or <see cref="PrivateScopeId"/> properties.
        /// </summary>
        private uint _addressOrScopeId;

        /// <summary>
        /// This field is only used for IPv6 addresses. A null value indicates that this instance is an IPv4 address.
        /// </summary>
        private readonly ushort[]? _numbers;

        /// <summary>
        /// A lazily initialized cache of the result of calling <see cref="ToString"/>.
        /// </summary>
        private string? _toString;

        /// <summary>
        /// A lazily initialized cache of the <see cref="GetHashCode"/> value.
        /// </summary>
        private int _hashCode;

        internal const int NumberOfLabels = IPAddressParserStatics.IPv6AddressBytes / 2;

        [MemberNotNullWhen(false, nameof(_numbers))]
        private bool IsIPv4
        {
            get { return _numbers == null; }
        }

        [MemberNotNullWhen(true, nameof(_numbers))]
        private bool IsIPv6
        {
            get { return _numbers != null; }
        }

        internal uint PrivateAddress
        {
            get
            {
                Debug.Assert(IsIPv4);
                return _addressOrScopeId;
            }
            private set
            {
                Debug.Assert(IsIPv4);
                _toString = null;
                _hashCode = 0;
                _addressOrScopeId = value;
            }
        }

        private uint PrivateScopeId
        {
            get
            {
                Debug.Assert(IsIPv6);
                return _addressOrScopeId;
            }
            set
            {
                Debug.Assert(IsIPv6);
                _toString = null;
                _hashCode = 0;
                _addressOrScopeId = value;
            }
        }

        /// <devdoc>
        ///   <para>
        ///     Initializes a new instance of the <see cref='System.Net.IPAddress'/>
        ///     class with the specified address.
        ///   </para>
        /// </devdoc>
        public IPAddress(long newAddress)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)newAddress, 0x00000000FFFFFFFF, nameof(newAddress));

            PrivateAddress = (uint)newAddress;
        }

        /// <devdoc>
        ///   <para>
        ///     Constructor for an IPv6 Address with a specified Scope.
        ///   </para>
        /// </devdoc>
        public IPAddress(byte[] address, long scopeid) :
            this(new ReadOnlySpan<byte>(address ?? ThrowAddressNullException()), scopeid)
        {
        }

        public IPAddress(ReadOnlySpan<byte> address, long scopeid)
        {
            if (address.Length != IPAddressParserStatics.IPv6AddressBytes)
            {
                throw new ArgumentException(SR.dns_bad_ip_address, nameof(address));
            }

            // Consider: Since scope is only valid for link-local and site-local
            //           addresses we could implement some more robust checking here
            ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)scopeid, 0x00000000FFFFFFFF, nameof(scopeid));

            _numbers = ReadUInt16NumbersFromBytes(address);
            PrivateScopeId = (uint)scopeid;
        }

        internal IPAddress(ReadOnlySpan<ushort> numbers, uint scopeid)
        {
            Debug.Assert(numbers != null);
            Debug.Assert(numbers.Length == NumberOfLabels);

            _numbers = numbers.ToArray();
            PrivateScopeId = scopeid;
        }

        private IPAddress(ushort[] numbers, uint scopeid)
        {
            Debug.Assert(numbers != null);
            Debug.Assert(numbers.Length == NumberOfLabels);

            _numbers = numbers;
            PrivateScopeId = scopeid;
        }

        /// <devdoc>
        ///   <para>
        ///     Constructor for IPv4 and IPv6 Address.
        ///   </para>
        /// </devdoc>
        public IPAddress(byte[] address) :
            this(new ReadOnlySpan<byte>(address ?? ThrowAddressNullException()))
        {
        }

        public IPAddress(ReadOnlySpan<byte> address)
        {
            if (address.Length == IPAddressParserStatics.IPv4AddressBytes)
            {
                PrivateAddress = MemoryMarshal.Read<uint>(address);
            }
            else if (address.Length == IPAddressParserStatics.IPv6AddressBytes)
            {
                _numbers = ReadUInt16NumbersFromBytes(address);
            }
            else
            {
                throw new ArgumentException(SR.dns_bad_ip_address, nameof(address));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort[] ReadUInt16NumbersFromBytes(ReadOnlySpan<byte> address)
        {
            ushort[] numbers = new ushort[NumberOfLabels];
            if (Vector128.IsHardwareAccelerated)
            {
                Vector128<ushort> ushorts = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(address)).AsUInt16();
                if (BitConverter.IsLittleEndian)
                {
                    // Reverse endianness of each ushort
                    ushorts = Vector128.ShiftLeft(ushorts, 8) | Vector128.ShiftRightLogical(ushorts, 8);
                }
                ushorts.StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(numbers));
            }
            else
            {
                for (int i = 0; i < numbers.Length; i++)
                {
                    numbers[i] = BinaryPrimitives.ReadUInt16BigEndian(address.Slice(i * 2));
                }
            }

            return numbers;
        }

        // We need this internally since we need to interface with winsock,
        // and winsock only understands Int32.
        internal IPAddress(int newAddress)
        {
            PrivateAddress = (uint)newAddress;
        }

        /// <devdoc>
        ///   <para>
        ///     Converts an IP address string to an <see cref='System.Net.IPAddress'/> instance.
        ///   </para>
        /// </devdoc>
        public static bool TryParse([NotNullWhen(true)] string? ipString, [NotNullWhen(true)] out IPAddress? address)
        {
            if (ipString == null)
            {
                address = null;
                return false;
            }

            address = IPAddressParser.Parse(ipString.AsSpan(), tryParse: true);
            return (address != null);
        }

        public static bool TryParse(ReadOnlySpan<char> ipSpan, [NotNullWhen(true)] out IPAddress? address)
        {
            address = IPAddressParser.Parse(ipSpan, tryParse: true);
            return (address != null);
        }

        /// <inheritdoc/>
        static bool IParsable<IPAddress>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [NotNullWhen(true)] out IPAddress? result) =>
            // provider is explicitly ignored
            TryParse(s, out result);

        /// <inheritdoc/>
        static bool ISpanParsable<IPAddress>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [NotNullWhen(true)] out IPAddress? result) =>
            // provider is explicitly ignored
            TryParse(s, out result);

        public static IPAddress Parse(string ipString)
        {
            ArgumentNullException.ThrowIfNull(ipString);

            return IPAddressParser.Parse(ipString.AsSpan(), tryParse: false)!;
        }

        public static IPAddress Parse(ReadOnlySpan<char> ipSpan)
        {
            return IPAddressParser.Parse(ipSpan, tryParse: false)!;
        }

        /// <inheritdoc/>
        static IPAddress ISpanParsable<IPAddress>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
            // provider is explicitly ignored
            Parse(s);

        /// <inheritdoc/>
        static IPAddress IParsable<IPAddress>.Parse(string s, IFormatProvider? provider) =>
            // provider is explicitly ignored
            Parse(s);

        public bool TryWriteBytes(Span<byte> destination, out int bytesWritten)
        {
            if (IsIPv6)
            {
                if (destination.Length < IPAddressParserStatics.IPv6AddressBytes)
                {
                    bytesWritten = 0;
                    return false;
                }

                WriteIPv6Bytes(destination);
                bytesWritten = IPAddressParserStatics.IPv6AddressBytes;
            }
            else
            {
                if (destination.Length < IPAddressParserStatics.IPv4AddressBytes)
                {
                    bytesWritten = 0;
                    return false;
                }

                WriteIPv4Bytes(destination);
                bytesWritten = IPAddressParserStatics.IPv4AddressBytes;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteIPv6Bytes(Span<byte> destination)
        {
            ushort[]? numbers = _numbers;
            Debug.Assert(numbers != null && numbers.Length == NumberOfLabels);

            if (BitConverter.IsLittleEndian)
            {
                if (Vector128.IsHardwareAccelerated)
                {
                    Vector128<ushort> ushorts = Vector128.LoadUnsafe(ref MemoryMarshal.GetArrayDataReference(numbers));
                    ushorts = Vector128.ShiftLeft(ushorts, 8) | Vector128.ShiftRightLogical(ushorts, 8);
                    ushorts.AsByte().StoreUnsafe(ref MemoryMarshal.GetReference(destination));
                }
                else
                {
                    for (int i = 0; i < numbers.Length; i++)
                    {
                        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(i * 2), numbers[i]);
                    }
                }
            }
            else
            {
                MemoryMarshal.AsBytes<ushort>(numbers).CopyTo(destination);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteIPv4Bytes(Span<byte> destination)
        {
            uint address = PrivateAddress;
            MemoryMarshal.Write(destination, ref address);
        }

        /// <devdoc>
        ///   <para>
        ///     Provides a copy of the IPAddress internals as an array of bytes.
        ///   </para>
        /// </devdoc>
        public byte[] GetAddressBytes()
        {
            if (IsIPv6)
            {
                Debug.Assert(_numbers != null && _numbers.Length == NumberOfLabels);
                byte[] bytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
                WriteIPv6Bytes(bytes);
                return bytes;
            }
            else
            {
                byte[] bytes = new byte[IPAddressParserStatics.IPv4AddressBytes];
                WriteIPv4Bytes(bytes);
                return bytes;
            }
        }

        public AddressFamily AddressFamily
        {
            get
            {
                return IsIPv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
            }
        }

        /// <devdoc>
        ///   <para>
        ///     IPv6 Scope identifier. This is really a uint32, but that isn't CLS compliant
        ///   </para>
        /// </devdoc>
        public long ScopeId
        {
            get
            {
                // Not valid for IPv4 addresses
                if (IsIPv4)
                {
                    ThrowSocketOperationNotSupported();
                }

                return PrivateScopeId;
            }
            set
            {
                // Not valid for IPv4 addresses
                if (IsIPv4)
                {
                    ThrowSocketOperationNotSupported();
                }

                // Consider: Since scope is only valid for link-local and site-local
                //           addresses we could implement some more robust checking here
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 0x00000000FFFFFFFF);

                PrivateScopeId = (uint)value;
            }
        }

        /// <devdoc>
        ///   <para>
        ///     Converts the Internet address to either standard dotted quad format
        ///     or standard IPv6 representation.
        ///   </para>
        /// </devdoc>
        public override string ToString()
        {
            string? toString = _toString;
            if (toString is null)
            {
                Span<char> span = stackalloc char[IPAddressParser.MaxIPv6StringLength];
                int length = IsIPv4 ?
                    IPAddressParser.FormatIPv4Address(_addressOrScopeId, span) :
                    IPAddressParser.FormatIPv6Address(_numbers, _addressOrScopeId, span);
                _toString = toString = new string(span.Slice(0, length));
            }

            return toString;
        }

        /// <inheritdoc/>
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider) =>
            // format and provider are explicitly ignored
            ToString();

        public bool TryFormat(Span<char> destination, out int charsWritten) =>
            TryFormatCore(destination, out charsWritten);

        /// <summary>Tries to format the current IP address into the provided span.</summary>
        /// <param name="utf8Destination">When this method returns, the IP address as a span of UTF8 bytes.</param>
        /// <param name="bytesWritten">When this method returns, the number of bytes written into the <paramref name="utf8Destination"/>.</param>
        /// <returns><see langword="true" /> if the formatting was successful; otherwise, <see langword="false" />.</returns>
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) =>
            TryFormatCore(utf8Destination, out bytesWritten);

        /// <inheritdoc/>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            // format and provider are explicitly ignored
            TryFormatCore(destination, out charsWritten);

        /// <inheritdoc/>
        bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            // format and provider are explicitly ignored
            TryFormatCore(utf8Destination, out bytesWritten);

        private bool TryFormatCore<TChar>(Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            if (IsIPv4)
            {
                if (destination.Length >= IPAddressParser.MaxIPv4StringLength)
                {
                    charsWritten = IPAddressParser.FormatIPv4Address(_addressOrScopeId, destination);
                    return true;
                }
            }
            else
            {
                if (destination.Length >= IPAddressParser.MaxIPv6StringLength)
                {
                    charsWritten = IPAddressParser.FormatIPv6Address(_numbers, _addressOrScopeId, destination);
                    return true;
                }
            }

            Span<TChar> tmpDestination = stackalloc TChar[IPAddressParser.MaxIPv6StringLength];
            Debug.Assert(tmpDestination.Length >= IPAddressParser.MaxIPv4StringLength);

            int written = IsIPv4 ?
                IPAddressParser.FormatIPv4Address(PrivateAddress, tmpDestination) :
                IPAddressParser.FormatIPv6Address(_numbers, PrivateScopeId, tmpDestination);

            if (tmpDestination.Slice(0, written).TryCopyTo(destination))
            {
                charsWritten = written;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        public static long HostToNetworkOrder(long host)
        {
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(host) : host;
        }

        public static int HostToNetworkOrder(int host)
        {
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(host) : host;
        }

        public static short HostToNetworkOrder(short host)
        {
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(host) : host;
        }

        public static long NetworkToHostOrder(long network)
        {
            return HostToNetworkOrder(network);
        }

        public static int NetworkToHostOrder(int network)
        {
            return HostToNetworkOrder(network);
        }

        public static short NetworkToHostOrder(short network)
        {
            return HostToNetworkOrder(network);
        }

        public static bool IsLoopback(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            if (address.IsIPv6)
            {
                // Do Equals test for IPv6 addresses
                return address.Equals(IPv6Loopback) || address.Equals(s_loopbackMappedToIPv6);
            }
            else
            {
                long LoopbackMask = (uint)HostToNetworkOrder(unchecked((int)LoopbackMaskHostOrder));
                return ((address.PrivateAddress & LoopbackMask) == (Loopback.PrivateAddress & LoopbackMask));
            }
        }

        /// <devdoc>
        ///   <para>
        ///     Determines if an address is an IPv6 Multicast address
        ///   </para>
        /// </devdoc>
        public bool IsIPv6Multicast
        {
            get
            {
                return IsIPv6 && ((_numbers[0] & 0xFF00) == 0xFF00);
            }
        }

        /// <devdoc>
        ///   <para>
        ///     Determines if an address is an IPv6 Link Local address
        ///   </para>
        /// </devdoc>
        public bool IsIPv6LinkLocal
        {
            get
            {
                return IsIPv6 && ((_numbers[0] & 0xFFC0) == 0xFE80);
            }
        }

        /// <devdoc>
        ///   <para>
        ///     Determines if an address is an IPv6 Site Local address
        ///   </para>
        /// </devdoc>
        public bool IsIPv6SiteLocal
        {
            get
            {
                return IsIPv6 && ((_numbers[0] & 0xFFC0) == 0xFEC0);
            }
        }

        public bool IsIPv6Teredo
        {
            get
            {
                return IsIPv6 &&
                       (_numbers[0] == 0x2001) &&
                       (_numbers[1] == 0);
            }
        }

        /// <summary>Gets whether the address is an IPv6 Unique Local address.</summary>
        public bool IsIPv6UniqueLocal
        {
            get
            {
                return IsIPv6 && ((_numbers[0] & 0xFE00) == 0xFC00);
            }
        }

        // 0:0:0:0:0:FFFF:x.x.x.x
        public bool IsIPv4MappedToIPv6
        {
            get
            {
                if (IsIPv4)
                {
                    return false;
                }

                ReadOnlySpan<byte> numbers = MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(_numbers));
                return
                    MemoryMarshal.Read<ulong>(numbers) == 0 &&
                    BinaryPrimitives.ReadUInt32LittleEndian(numbers.Slice(8)) == 0xFFFF0000;
            }
        }

        [Obsolete("IPAddress.Address is address family dependent and has been deprecated. Use IPAddress.Equals to perform comparisons instead.")]
        public long Address
        {
            get
            {
                if (AddressFamily == AddressFamily.InterNetworkV6)
                {
                    ThrowSocketOperationNotSupported();
                }

                return PrivateAddress;
            }
            set
            {
                if (AddressFamily == AddressFamily.InterNetworkV6)
                {
                    ThrowSocketOperationNotSupported();
                }

                if (PrivateAddress != value)
                {
                    if (this is ReadOnlyIPAddress)
                    {
                        ThrowSocketOperationNotSupported();
                    }

                    PrivateAddress = unchecked((uint)value);
                }
            }
        }

        /// <summary>Compares two IP addresses.</summary>
        public override bool Equals([NotNullWhen(true)] object? comparand)
        {
            return comparand is IPAddress address && Equals(address);
        }

        internal bool Equals(IPAddress comparand)
        {
            Debug.Assert(comparand != null);

            // Compare families before address representations
            if (AddressFamily != comparand.AddressFamily)
            {
                return false;
            }

            if (IsIPv6)
            {
                // For IPv6 addresses, we must compare the full 128-bit representation and the scope IDs.
                ReadOnlySpan<byte> thisNumbers = MemoryMarshal.AsBytes<ushort>(_numbers);
                ReadOnlySpan<byte> comparandNumbers = MemoryMarshal.AsBytes<ushort>(comparand._numbers);
                return
                    MemoryMarshal.Read<ulong>(thisNumbers) == MemoryMarshal.Read<ulong>(comparandNumbers) &&
                    MemoryMarshal.Read<ulong>(thisNumbers.Slice(sizeof(ulong))) == MemoryMarshal.Read<ulong>(comparandNumbers.Slice(sizeof(ulong))) &&
                    PrivateScopeId == comparand.PrivateScopeId;
            }
            else
            {
                // For IPv4 addresses, compare the integer representation.
                return comparand.PrivateAddress == PrivateAddress;
            }
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                // For IPv4 addresses, we calculate the hashcode based on address bytes.
                // For IPv6 addresses, we also factor in scope ID.
                if (IsIPv6)
                {
                    ReadOnlySpan<byte> numbers = MemoryMarshal.AsBytes<ushort>(_numbers);
                    _hashCode = HashCode.Combine(
                        MemoryMarshal.Read<uint>(numbers),
                        MemoryMarshal.Read<uint>(numbers.Slice(4)),
                        MemoryMarshal.Read<uint>(numbers.Slice(8)),
                        MemoryMarshal.Read<uint>(numbers.Slice(12)),
                        _addressOrScopeId);
                }
                else
                {
                    _hashCode = HashCode.Combine(_addressOrScopeId);
                }
            }

            return _hashCode;
        }

        // IPv4 192.168.1.1 maps as ::FFFF:192.168.1.1
        public IPAddress MapToIPv6()
        {
            if (IsIPv6)
            {
                return this;
            }

            uint address = (uint)NetworkToHostOrder(unchecked((int)PrivateAddress));
            ushort[] labels = new ushort[NumberOfLabels];
            labels[5] = 0xFFFF;
            labels[6] = (ushort)(address >> 16);
            labels[7] = (ushort)address;
            return new IPAddress(labels, 0);
        }

        // Takes the last 4 bytes of an IPv6 address and converts it to an IPv4 address.
        // This does not restrict to address with the ::FFFF: prefix because other types of
        // addresses display the tail segments as IPv4 like Terado.
        public IPAddress MapToIPv4()
        {
            if (IsIPv4)
            {
                return this;
            }

            uint address = (uint)_numbers[6] << 16 | (uint)_numbers[7];
            return new IPAddress((uint)HostToNetworkOrder(unchecked((int)address)));
        }

        [DoesNotReturn]
        private static byte[] ThrowAddressNullException() => throw new ArgumentNullException("address");

        [DoesNotReturn]
        private static void ThrowSocketOperationNotSupported() => throw new SocketException(SocketError.OperationNotSupported);

        private sealed class ReadOnlyIPAddress : IPAddress
        {
            public ReadOnlyIPAddress(ReadOnlySpan<byte> newAddress) : base(newAddress)
            { }
        }
    }
}
