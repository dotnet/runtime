// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System;

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///       This class is used when subclassing EndPoint, and provides indication
    ///       on how to format the memory buffers that winsock uses for network addresses.
    ///    </para>
    /// </devdoc>
    public class SocketAddress
    {
        internal const int IPv6AddressSize = 28;
        internal const int IPv4AddressSize = 16;

        internal int InternalSize;
        internal byte[] Buffer;

        private const int WriteableOffset = 2;
        private const int MaxSize = 32; // IrDA requires 32 bytes
        private bool _changed = true;
        private int _hash;

        public AddressFamily Family
        {
            get
            {
                int family;
#if BIGENDIAN
                family = ((int)Buffer[0]<<8) | Buffer[1];
#else
                family = Buffer[0] | ((int)Buffer[1] << 8);
#endif
                return (AddressFamily)family;
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
            if (size < WriteableOffset)
            {
                // It doesn't make sense to create a socket address with less than
                // 2 bytes: that's where we store the address family.
                throw new ArgumentOutOfRangeException("size");
            }
            InternalSize = size;
            Buffer = new byte[(size / IntPtr.Size + 2) * IntPtr.Size];

#if BIGENDIAN
            Buffer[0] = unchecked((byte)((int)family>>8));
            Buffer[1] = unchecked((byte)((int)family   ));
#else
            Buffer[0] = unchecked((byte)((int)family));
            Buffer[1] = unchecked((byte)((int)family >> 8));
#endif
        }

        internal SocketAddress(IPAddress ipAddress)
            : this(ipAddress.AddressFamily,
                ((ipAddress.AddressFamily == AddressFamily.InterNetwork) ? IPv4AddressSize : IPv6AddressSize))
        {
            // No Port
            Buffer[2] = (byte)0;
            Buffer[3] = (byte)0;

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // No handling for Flow Information
                Buffer[4] = (byte)0;
                Buffer[5] = (byte)0;
                Buffer[6] = (byte)0;
                Buffer[7] = (byte)0;

                // Scope serialization
                long scope = ipAddress.ScopeId;
                Buffer[24] = (byte)scope;
                Buffer[25] = (byte)(scope >> 8);
                Buffer[26] = (byte)(scope >> 16);
                Buffer[27] = (byte)(scope >> 24);

                // Address serialization
                byte[] addressBytes = ipAddress.GetAddressBytes();
                for (int i = 0; i < addressBytes.Length; i++)
                {
                    Buffer[8 + i] = addressBytes[i];
                }
            }
            else
            {
                // IPv4 Address serialization
                Buffer[4] = unchecked((byte)(ipAddress.Address));
                Buffer[5] = unchecked((byte)(ipAddress.Address >> 8));
                Buffer[6] = unchecked((byte)(ipAddress.Address >> 16));
                Buffer[7] = unchecked((byte)(ipAddress.Address >> 24));
            }
        }

        internal SocketAddress(IPAddress ipaddress, int port)
            : this(ipaddress)
        {
            Buffer[2] = (byte)(port >> 8);
            Buffer[3] = (byte)port;
        }

        internal IPAddress GetIPAddress()
        {
            if (Family == AddressFamily.InterNetworkV6)
            {
                Debug.Assert(Size >= IPv6AddressSize);

                byte[] address = new byte[IPAddressParser.IPv6AddressBytes];
                for (int i = 0; i < address.Length; i++)
                {
                    address[i] = Buffer[i + 8];
                }

                long scope = (long)((Buffer[27] << 24) +
                                    (Buffer[26] << 16) +
                                    (Buffer[25] << 8) +
                                    (Buffer[24]));

                return new IPAddress(address, scope);
            }
            else if (Family == AddressFamily.InterNetwork)
            {
                Debug.Assert(Size >= IPv4AddressSize);

                long address = (long)(
                        (Buffer[4] & 0x000000FF) |
                        (Buffer[5] << 8 & 0x0000FF00) |
                        (Buffer[6] << 16 & 0x00FF0000) |
                        (Buffer[7] << 24)
                        ) & 0x00000000FFFFFFFF;

                return new IPAddress(address);
            }
            else
            {
                throw new SocketException(SocketError.AddressFamilyNotSupported);
            }
        }

        internal IPEndPoint GetIPEndPoint()
        {
            IPAddress address = GetIPAddress();
            int port = (int)((Buffer[2] << 8 & 0xFF00) | (Buffer[3]));
            return new IPEndPoint(address, port);
        }

        // For ReceiveFrom we need to pin address size, using reserved Buffer space
        internal void CopyAddressSizeIntoBuffer()
        {
            Buffer[Buffer.Length - IntPtr.Size] = unchecked((byte)(InternalSize));
            Buffer[Buffer.Length - IntPtr.Size + 1] = unchecked((byte)(InternalSize >> 8));
            Buffer[Buffer.Length - IntPtr.Size + 2] = unchecked((byte)(InternalSize >> 16));
            Buffer[Buffer.Length - IntPtr.Size + 3] = unchecked((byte)(InternalSize >> 24));
        }

        // Can be called after the above method did work
        internal int GetAddressSizeOffset()
        {
            return Buffer.Length - IntPtr.Size;
        }

        // For ReceiveFrom we need to update the address size upon IO return
        internal unsafe void SetSize(IntPtr ptr)
        {
            // Apparently it must be less or equal the original value since ReceiveFrom cannot reallocate the address buffer
            InternalSize = *(int*)ptr;
        }
        public override bool Equals(object comparand)
        {
            SocketAddress castedComparand = comparand as SocketAddress;
            if (castedComparand == null || this.Size != castedComparand.Size)
            {
                return false;
            }
            for (int i = 0; i < this.Size; i++)
            {
                if (this[i] != castedComparand[i])
                {
                    return false;
                }
            }
            return true;
        }

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
                    _hash ^= (int)Buffer[i]
                            | ((int)Buffer[i + 1] << 8)
                            | ((int)Buffer[i + 2] << 16)
                            | ((int)Buffer[i + 3] << 24);
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
            StringBuilder bytes = new StringBuilder();
            for (int i = WriteableOffset; i < this.Size; i++)
            {
                if (i > WriteableOffset)
                {
                    bytes.Append(",");
                }
                bytes.Append(this[i].ToString(NumberFormatInfo.InvariantInfo));
            }
            return Family.ToString() + ":" + Size.ToString(NumberFormatInfo.InvariantInfo) + ":{" + bytes.ToString() + "}";
        }
    }
}
