// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;

namespace System.Net
{
    // This class is used when subclassing EndPoint, and provides indication
    // on how to format the memory buffers that the platform uses for network addresses.
    public class SocketAddress : IEquatable<SocketAddress>
    {
#pragma warning disable CA1802 // these could be const on Windows but need to be static readonly for Unix
        internal static readonly int IPv6AddressSize = SocketAddressPal.IPv6AddressSize;
        internal static readonly int IPv4AddressSize = SocketAddressPal.IPv4AddressSize;
        internal static readonly int UdsAddressSize = SocketAddressPal.UdsAddressSize;
        internal static readonly int MaxAddressSize = SocketAddressPal.MaxAddressSize;
#pragma warning restore CA1802

        private int _size;
        private byte[] _buffer;

        private const int MinSize = 2;
        private const int DataOffset = 2;

        public AddressFamily Family
        {
            get
            {
                return SocketAddressPal.GetAddressFamily(_buffer);
            }
        }

        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _buffer.Length);
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
                _size = value;
            }
        }

        // Access to unmanaged serialized data. This doesn't
        // allow access to the first 2 bytes of unmanaged memory
        // that are supposed to contain the address family which
        // is readonly.
        public byte this[int offset]
        {
            get
            {
                if ((uint)offset >= (uint)Size)
                {
                    throw new IndexOutOfRangeException();
                }
                return _buffer[offset];
            }
            set
            {
                if ((uint)offset >= (uint)Size)
                {
                    throw new IndexOutOfRangeException();
                }
                _buffer[offset] = value;
            }
        }

        public static int GetMaximumAddressSize(AddressFamily addressFamily) => addressFamily switch
        {
            AddressFamily.InterNetwork => IPv4AddressSize,
            AddressFamily.InterNetworkV6 => IPv6AddressSize,
            AddressFamily.Unix => UdsAddressSize,
            _ => MaxAddressSize
        };

        public SocketAddress(AddressFamily family) : this(family, GetMaximumAddressSize(family))
        {
        }

        public SocketAddress(AddressFamily family, int size)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(size, MinSize);

            _size = size;
            _buffer = new byte[size];
            _buffer[0] = (byte)_size;

            SocketAddressPal.SetAddressFamily(_buffer, family);
        }

        internal SocketAddress(IPAddress ipAddress)
            : this(ipAddress.AddressFamily,
                ((ipAddress.AddressFamily == AddressFamily.InterNetwork) ? IPv4AddressSize : IPv6AddressSize))
        {

            // No Port.
            SocketAddressPal.SetPort(_buffer, 0);

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                Span<byte> addressBytes = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                ipAddress.TryWriteBytes(addressBytes, out int bytesWritten);
                Debug.Assert(bytesWritten == IPAddressParserStatics.IPv6AddressBytes);

                SocketAddressPal.SetIPv6Address(_buffer, addressBytes, (uint)ipAddress.ScopeId);
            }
            else
            {
#pragma warning disable CS0618 // using Obsolete Address API because it's the more efficient option in this case
                uint address = unchecked((uint)ipAddress.Address);
#pragma warning restore CS0618

                Debug.Assert(ipAddress.AddressFamily == AddressFamily.InterNetwork);
                SocketAddressPal.SetIPv4Address(_buffer, address);
            }
        }

        internal SocketAddress(IPAddress ipaddress, int port)
            : this(ipaddress)
        {
            SocketAddressPal.SetPort(_buffer, unchecked((ushort)port));
        }

        /// <summary>This represents underlying memory that can be passed to native OS calls.</summary>
        /// <remarks>
        /// Content of the memory can be invalidated if <see cref="Size"/> is changed or if the SocketAddress is used in another receive call.
        /// </remarks>
        public Memory<byte> Buffer
        {
            get
            {
                return new Memory<byte>(_buffer);
            }
        }


        public override bool Equals(object? comparand) =>
            comparand is SocketAddress other && Equals(other);

        public bool Equals(SocketAddress? comparand) => comparand != null && Buffer.Span.SequenceEqual(comparand.Buffer.Span);

        public override int GetHashCode()
        {
            HashCode hash = default;
            hash.AddBytes(new ReadOnlySpan<byte>(_buffer, 0, _size));
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            // Get the address family string.  In almost all cases, this should be a cached string
            // from the enum and won't actually allocate.
            string familyString = Family.ToString();

            // Determine the maximum length needed to format.
            int maxLength =
                checked(
                    familyString.Length + // AddressFamily
                    1 + // :
                    10 + // Size (max length for a positive Int32)
                    2 + // :{
                    (Size - DataOffset) * 4 + // at most ','+3digits per byte
                    1 // }
                );

            Span<char> result = (uint)maxLength <= 256 ? // arbitrary limit that should be large enough for the vast majority of cases
                stackalloc char[256] :
                new char[maxLength];

            familyString.CopyTo(result);
            int length = familyString.Length;

            result[length++] = ':';

            bool formatted = Size.TryFormat(result.Slice(length), out int charsWritten);
            Debug.Assert(formatted);
            length += charsWritten;

            result[length++] = ':';
            result[length++] = '{';

            byte[] buffer = _buffer;
            for (int i = DataOffset; i < Size; i++)
            {
                if (i > DataOffset)
                {
                    result[length++] = ',';
                }

                formatted = buffer[i].TryFormat(result.Slice(length), out charsWritten);
                Debug.Assert(formatted);
                length += charsWritten;
            }

            result[length++] = '}';
            return result.Slice(0, length).ToString();
        }
    }
}
