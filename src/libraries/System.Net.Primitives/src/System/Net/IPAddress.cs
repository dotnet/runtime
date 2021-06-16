// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net
{
    /// <devdoc>
    ///   <para>
    ///     Provides an Internet Protocol (IP) address.
    ///   </para>
    /// </devdoc>
    public class IPAddress
    {
        public static readonly IPAddress Any = new ReadOnlyIPAddress(new byte[] { 0, 0, 0, 0 });
        public static readonly IPAddress Loopback = new ReadOnlyIPAddress(new byte[] { 127, 0, 0, 1 });
        public static readonly IPAddress Broadcast = new ReadOnlyIPAddress(new byte[] { 255, 255, 255, 255 });
        public static readonly IPAddress None = Broadcast;

        internal const uint LoopbackMaskHostOrder = 0xFF000000;

        public static readonly IPAddress IPv6Any = new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0);
        public static readonly IPAddress IPv6Loopback = new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, 0);
        public static readonly IPAddress IPv6None = new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0);

        private static readonly IPAddress s_loopbackMappedToIPv6 = new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 127, 0, 0, 1 }, 0);

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

        private bool IsIPv4
        {
            get { return _numbers == null; }
        }

        private bool IsIPv6
        {
            get { return _numbers != null; }
        }

        private uint PrivateAddress
        {
            get
            {
                Debug.Assert(IsIPv4);
                return _addressOrScopeId;
            }
            set
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
            if (newAddress < 0 || newAddress > 0x00000000FFFFFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(newAddress));
            }

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
            if (scopeid < 0 || scopeid > 0x00000000FFFFFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(scopeid));
            }

            _numbers = new ushort[NumberOfLabels];

            for (int i = 0; i < NumberOfLabels; i++)
            {
                _numbers[i] = (ushort)(address[i * 2] * 256 + address[i * 2 + 1]);
            }

            PrivateScopeId = (uint)scopeid;
        }

        internal IPAddress(ReadOnlySpan<ushort> numbers, uint scopeid)
        {
            Debug.Assert(numbers != null);
            Debug.Assert(numbers.Length == NumberOfLabels);

            var arr = new ushort[NumberOfLabels];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = numbers[i];
            }

            _numbers = arr;
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
                _numbers = new ushort[NumberOfLabels];

                for (int i = 0; i < NumberOfLabels; i++)
                {
                    _numbers[i] = (ushort)(address[i * 2] * 256 + address[i * 2 + 1]);
                }
            }
            else
            {
                throw new ArgumentException(SR.dns_bad_ip_address, nameof(address));
            }
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

        public static IPAddress Parse(string ipString)
        {
            if (ipString == null)
            {
                throw new ArgumentNullException(nameof(ipString));
            }

            return IPAddressParser.Parse(ipString.AsSpan(), tryParse: false)!;
        }

        public static IPAddress Parse(ReadOnlySpan<char> ipSpan)
        {
            return IPAddressParser.Parse(ipSpan, tryParse: false)!;
        }

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
            Debug.Assert(_numbers != null && _numbers.Length == NumberOfLabels);
            int j = 0;
            for (int i = 0; i < NumberOfLabels; i++)
            {
                destination[j++] = (byte)((_numbers[i] >> 8) & 0xFF);
                destination[j++] = (byte)((_numbers[i]) & 0xFF);
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
                    throw new SocketException(SocketError.OperationNotSupported);
                }

                return PrivateScopeId;
            }
            set
            {
                // Not valid for IPv4 addresses
                if (IsIPv4)
                {
                    throw new SocketException(SocketError.OperationNotSupported);
                }

                // Consider: Since scope is only valid for link-local and site-local
                //           addresses we could implement some more robust checking here
                if (value < 0 || value > 0x00000000FFFFFFFF)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

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
            if (_toString == null)
            {
                _toString = IsIPv4 ?
                    IPAddressParser.IPv4AddressToString(PrivateAddress) :
                    IPAddressParser.IPv6AddressToString(_numbers!, PrivateScopeId);
            }

            return _toString;
        }

        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            return IsIPv4 ?
                IPAddressParser.IPv4AddressToString(PrivateAddress, destination, out charsWritten) :
                IPAddressParser.IPv6AddressToString(_numbers!, PrivateScopeId, destination, out charsWritten);
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
            if (address == null)
            {
                ThrowAddressNullException();
            }

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
                return IsIPv6 && ((_numbers![0] & 0xFF00) == 0xFF00);
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
                return IsIPv6 && ((_numbers![0] & 0xFFC0) == 0xFE80);
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
                return IsIPv6 && ((_numbers![0] & 0xFFC0) == 0xFEC0);
            }
        }

        public bool IsIPv6Teredo
        {
            get
            {
                return IsIPv6 &&
                       (_numbers![0] == 0x2001) &&
                       (_numbers![1] == 0);
            }
        }

        /// <summary>Gets whether the address is an IPv6 Unique Local address.</summary>
        public bool IsIPv6UniqueLocal
        {
            get
            {
                return IsIPv6 && ((_numbers![0] & 0xFE00) == 0xFC00);
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
                for (int i = 0; i < 5; i++)
                {
                    if (_numbers![i] != 0)
                    {
                        return false;
                    }
                }
                return (_numbers![5] == 0xFFFF);
            }
        }

        [Obsolete("This property has been deprecated. It is address family dependent. Please use IPAddress.Equals method to perform comparisons. https://go.microsoft.com/fwlink/?linkid=14202")]
        public long Address
        {
            get
            {
                //
                // IPv6 Changes: Can't do this for IPv6, so throw an exception.
                //
                //
                if (AddressFamily == AddressFamily.InterNetworkV6)
                {
                    throw new SocketException(SocketError.OperationNotSupported);
                }
                else
                {
                    return PrivateAddress;
                }
            }
            set
            {
                //
                // IPv6 Changes: Can't do this for IPv6 addresses
                if (AddressFamily == AddressFamily.InterNetworkV6)
                {
                    throw new SocketException(SocketError.OperationNotSupported);
                }
                else
                {
                    if (PrivateAddress != value)
                    {
                        if (this is ReadOnlyIPAddress)
                        {
                            throw new SocketException(SocketError.OperationNotSupported);
                        }
                        PrivateAddress = unchecked((uint)value);
                    }
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

            uint address = (uint)_numbers![6] << 16 | (uint)_numbers[7];
            return new IPAddress((uint)HostToNetworkOrder(unchecked((int)address)));
        }

        [DoesNotReturn]
        private static byte[] ThrowAddressNullException() => throw new ArgumentNullException("address");

        private sealed class ReadOnlyIPAddress : IPAddress
        {
            public ReadOnlyIPAddress(byte[] newAddress) : base(newAddress)
            { }
        }
    }
}
