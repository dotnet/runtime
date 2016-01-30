// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal struct IPPacketInformation
        {
            public IPAddress Address;  // Destination IP Address
            public int InterfaceIndex; // Interface index
            private int Padding;       // Pad out to 8-byte alignment
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetControlMessageBufferSize")]
        internal static extern int GetControlMessageBufferSize(bool isIPv4, bool isIPv6);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_TryGetIPPacketInformation")]
        internal static extern unsafe bool TryGetIPPacketInformation(MessageHeader* messageHeader, bool isIPv4, IPPacketInformation* packetInfo);
    }
}
