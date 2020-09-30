// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    internal static class ByteOrder
    {
        public static void HostToNetworkBytes(this ushort host, byte[] bytes, int index)
        {
            bytes[index] = (byte)(host >> 8);
            bytes[index + 1] = unchecked((byte)host);
        }

        public static ushort NetworkBytesToHostUInt16(this ReadOnlySpan<byte> bytes, int index)
        {
            return (ushort)(((ushort)bytes[index] << 8) | (ushort)bytes[index + 1]);
        }
    }
}
