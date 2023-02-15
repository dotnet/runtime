// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

#if SYSTEM_NET_PRIMITIVES_DLL
namespace System.Net
#else
namespace System.Net.Internals
#endif
{
    // This class is used when subclassing EndPoint, and provides indication
    // on how to format the memory buffers that the platform uses for network addresses.
#if SYSTEM_NET_PRIMITIVES_DLL
    public
#else
    internal sealed
#endif
    class SocketAddress
    {
#pragma warning disable CA1802 // these could be const on Windows but need to be static readonly for Unix
        internal static readonly int IPv6AddressSize = SocketAddressPal.IPv6AddressSize;
        internal static readonly int IPv4AddressSize = SocketAddressPal.IPv4AddressSize;
#pragma warning restore CA1802

        internal int InternalSize;
        internal byte[] Buffer;

        private const int MinSize = 2;
        private const int MaxSize = 32; // IrDA requires 32 bytes
        private const int DataOffset = 2;
        private bool _changed = true;
        private int _hash;

        public AddressFamily Family
        {
            get
            {
                return SocketAddressPal.GetAddressFamily(Buffer);
            }
        }

        public int Size
        {
            get
            {
                return InternalSize;
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
                if (offset < 0 || offset >= Size)
                {
                    throw new IndexOutOfRangeException();
                }
                return Buffer[offset];
            }
            set
            {
                if (offset < 0 || offset >= Size)
                {
                    throw new IndexOutOfRangeException();
                }
                if (Buffer[offset] != value)
                {
                    _changed = true;
                }
                Buffer[offset] = value;
            }
        }

        public SocketAddress(AddressFamily family) : this(family, MaxSize)
        {
        }

        public SocketAddress(AddressFamily family, int size)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(size, MinSize);

            InternalSize = size;
#if !SYSTEM_NET_PRIMITIVES_DLL && WINDOWS
            // WSARecvFrom needs a pinned pointer to the 32bit socket address size: https://learn.microsoft.com/en-us/windows/win32/api/winsock2/nf-winsock2-wsarecvfrom
            // Allocate IntPtr.Size extra bytes at the end of Buffer ensuring IntPtr.Size alignment, so we don't need to pin anything else.
            // The following formula will extend 'size' to the alignment boundary then add IntPtr.Size more bytes.
            size = (size + IntPtr.Size -  1) / IntPtr.Size * IntPtr.Size + IntPtr.Size;
#endif
            Buffer = new byte[size];

            SocketAddressPal.SetAddressFamily(Buffer, family);
        }

        internal SocketAddress(IPAddress ipAddress)
            : this(ipAddress.AddressFamily,
                ((ipAddress.AddressFamily == AddressFamily.InterNetwork) ? IPv4AddressSize : IPv6AddressSize))
        {
            // No Port.
            SocketAddressPal.SetPort(Buffer, 0);

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                Span<byte> addressBytes = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                ipAddress.TryWriteBytes(addressBytes, out int bytesWritten);
                Debug.Assert(bytesWritten == IPAddressParserStatics.IPv6AddressBytes);

                SocketAddressPal.SetIPv6Address(Buffer, addressBytes, (uint)ipAddress.ScopeId);
            }
            else
            {
#pragma warning disable CS0618 // using Obsolete Address API because it's the more efficient option in this case
                uint address = unchecked((uint)ipAddress.Address);
#pragma warning restore CS0618

                Debug.Assert(ipAddress.AddressFamily == AddressFamily.InterNetwork);
                SocketAddressPal.SetIPv4Address(Buffer, address);
            }
        }

        internal SocketAddress(IPAddress ipaddress, int port)
            : this(ipaddress)
        {
            SocketAddressPal.SetPort(Buffer, unchecked((ushort)port));
        }

        internal SocketAddress(AddressFamily addressFamily, ReadOnlySpan<byte> buffer)
        {
            Buffer = buffer.ToArray();
            InternalSize = Buffer.Length;
            SocketAddressPal.SetAddressFamily(Buffer, addressFamily);
        }

        internal IPAddress GetIPAddress()
        {
            if (Family == AddressFamily.InterNetworkV6)
            {
                Debug.Assert(Size >= IPv6AddressSize);

                Span<byte> address = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                uint scope;
                SocketAddressPal.GetIPv6Address(Buffer, address, out scope);

                return new IPAddress(address, (long)scope);
            }
            else if (Family == AddressFamily.InterNetwork)
            {
                Debug.Assert(Size >= IPv4AddressSize);
                long address = (long)SocketAddressPal.GetIPv4Address(Buffer) & 0x0FFFFFFFF;
                return new IPAddress(address);
            }
            else
            {
#if SYSTEM_NET_PRIMITIVES_DLL
                throw new SocketException(SocketError.AddressFamilyNotSupported);
#else
                throw new SocketException((int)SocketError.AddressFamilyNotSupported);
#endif
            }
        }

        internal int GetPort() => (int)SocketAddressPal.GetPort(Buffer);

        internal IPEndPoint GetIPEndPoint()
        {
            return new IPEndPoint(GetIPAddress(), GetPort());
        }

#if !SYSTEM_NET_PRIMITIVES_DLL && WINDOWS
        // For ReceiveFrom we need to pin address size, using reserved Buffer space.
        internal void CopyAddressSizeIntoBuffer()
        {
            int addressSizeOffset = GetAddressSizeOffset();
            Buffer[addressSizeOffset] = unchecked((byte)(InternalSize));
            Buffer[addressSizeOffset + 1] = unchecked((byte)(InternalSize >> 8));
            Buffer[addressSizeOffset + 2] = unchecked((byte)(InternalSize >> 16));
            Buffer[addressSizeOffset + 3] = unchecked((byte)(InternalSize >> 24));
        }

        // Can be called after the above method did work.
        internal int GetAddressSizeOffset()
        {
            return Buffer.Length - IntPtr.Size;
        }
#endif

        public override bool Equals(object? comparand) =>
            comparand is SocketAddress other &&
            Buffer.AsSpan(0, Size).SequenceEqual(other.Buffer.AsSpan(0, other.Size));

        public override int GetHashCode()
        {
            if (_changed)
            {
                _changed = false;
                _hash = 0;

                int i;
                int size = Size & ~3;

                for (i = 0; i < size; i += 4)
                {
                    _hash ^= BinaryPrimitives.ReadInt32LittleEndian(Buffer.AsSpan(i));
                }
                if ((Size & 3) != 0)
                {
                    int remnant = 0;
                    int shift = 0;

                    for (; i < Size; ++i)
                    {
                        remnant |= ((int)Buffer[i]) << shift;
                        shift += 8;
                    }
                    _hash ^= remnant;
                }
            }
            return _hash;
        }

        public override string ToString()
        {
            // Get the address family string.  In almost all cases, this should be a cached string
            // from the enum and won't actually allocate.
            string familyString = Family.ToString();

            // Determine the maximum length needed to format.
            int maxLength =
                familyString.Length + // AddressFamily
                1 + // :
                10 + // Size (max length for a positive Int32)
                2 + // :{
                (Size - DataOffset) * 4 + // at most ','+3digits per byte
                1; // }

            Span<char> result = maxLength <= 256 ? // arbitrary limit that should be large enough for the vast majority of cases
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

            byte[] buffer = Buffer;
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
