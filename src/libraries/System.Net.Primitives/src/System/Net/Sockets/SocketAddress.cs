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
    ///       on how to format the memory buffers that the platform uses for network addresses.
    ///    </para>
    /// </devdoc>
    public class SocketAddress
    {
        internal const int IPv6AddressSize = SocketAddressPal.IPv6AddressSize;
        internal const int IPv4AddressSize = SocketAddressPal.IPv4AddressSize;

        internal int InternalSize;
        internal byte[] Buffer;

        private const int MinSize = 2;
        private const int MaxSize = 32; // IrDA requires 32 bytes
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
            if (size < MinSize)
            {
                throw new ArgumentOutOfRangeException("size");
            }

            InternalSize = size;
            Buffer = new byte[(size / IntPtr.Size + 2) * IntPtr.Size];

            SocketAddressPal.SetAddressFamily(Buffer, family);
        }

        internal SocketAddress(IPAddress ipAddress)
            : this(ipAddress.AddressFamily,
                ((ipAddress.AddressFamily == AddressFamily.InterNetwork) ? IPv4AddressSize : IPv6AddressSize))
        {
            // No Port
            SocketAddressPal.SetPort(Buffer, 0);

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                SocketAddressPal.SetIPv6Address(Buffer, ipAddress.GetAddressBytes(), (uint)ipAddress.ScopeId);
            }
            else
            {
                Debug.Assert(ipAddress.AddressFamily == AddressFamily.InterNetwork);
                SocketAddressPal.SetIPv4Address(Buffer, (uint)ipAddress.Address);
            }
        }

        internal SocketAddress(IPAddress ipaddress, int port)
            : this(ipaddress)
        {
            SocketAddressPal.SetPort(Buffer, unchecked((ushort)port));
        }

        internal IPAddress GetIPAddress()
        {
            if (Family == AddressFamily.InterNetworkV6)
            {
                Debug.Assert(Size >= IPv6AddressSize);

                byte[] address = new byte[IPAddressParser.IPv6AddressBytes];
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
                throw new SocketException(SocketError.AddressFamilyNotSupported);
            }
        }

        internal IPEndPoint GetIPEndPoint()
        {
            IPAddress address = GetIPAddress();
            int port = (int)SocketAddressPal.GetPort(Buffer);
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
            for (int i = SocketAddressPal.DataOffset; i < this.Size; i++)
            {
                if (i > SocketAddressPal.DataOffset)
                {
                    bytes.Append(",");
                }
                bytes.Append(this[i].ToString(NumberFormatInfo.InvariantInfo));
            }
            return Family.ToString() + ":" + Size.ToString(NumberFormatInfo.InvariantInfo) + ":{" + bytes.ToString() + "}";
        }
    }
}
