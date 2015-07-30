// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Net
{
    internal static class ByteOrder
    {
        public static ushort HostToNetwork(this ushort host)
        {
#if BIGENDIAN
            return host;
#else
            return unchecked((ushort)((host << 8) | (host >> 8)));
#endif
        }

        public static uint HostToNetwork(this uint host)
        {
#if BIGENDIAN
            return host;
#else
            return unchecked((uint)((host << 24) | ((host & 0xff00) << 8) | ((host & 0xff0000) >> 8) | ((host & 0xff000000) >> 24)));
#endif
        }

        public static void HostToNetworkBytes(this ushort host, byte[] bytes, int index)
        {
            bytes[index] = (byte)(host >> 8);
            bytes[index + 1] = (byte)host;
        }

        public static ushort NetworkToHost(this ushort network)
        {
            return HostToNetwork(network);
        }

        public static uint NetworkToHost(this uint network)
        {
            return HostToNetwork(network);
        }

        public static ushort NetworkBytesToHostUInt16(this byte[] bytes, int index)
        {
            return (ushort)(((ushort)bytes[index] << 8) | (ushort)bytes[index + 1]);
        }

        public static uint NetworkBytesToNetworkUInt32(this byte[] bytes, int index)
        {
#if BIGENDIAN
            return unchecked((uint)(bytes[index + 3] | (bytes[index + 2] << 8) | (bytes[index + 1] << 16) | (bytes[index] << 24)));
#else
            return unchecked((uint)(bytes[index] | (bytes[index + 1] << 8) | (bytes[index + 2] << 16) | (bytes[index + 3] << 24)));
#endif
        }

        public static void NetworkToNetworkBytes(this uint host, byte[] bytes, int index)
        {
#if BIGENDIAN
            bytes[index] = (byte)(host >> 24);
            bytes[index + 1] = (byte)(host >> 16);
            bytes[index + 2] = (byte)(host >> 8);
            bytes[index + 3] = (byte)host;
#else
            bytes[index] = (byte)host;
            bytes[index + 1] = (byte)(host >> 8);
            bytes[index + 2] = (byte)(host >> 16);
            bytes[index + 3] = (byte)(host >> 24);
#endif
        }
    }
}
