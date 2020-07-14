// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

#pragma warning disable CA1823 // unused private padding fields in MulticastOption structs

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal struct IPPacketInformation
        {
            public IPAddress Address;  // Destination IP Address
            public int InterfaceIndex; // Interface index
            private int _padding;       // Pad out to 8-byte alignment
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetControlMessageBufferSize")]
        [SuppressGCTransition]
        internal static extern int GetControlMessageBufferSize(int isIPv4, int isIPv6);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_TryGetIPPacketInformation")]
        internal static extern unsafe bool TryGetIPPacketInformation(MessageHeader* messageHeader, bool isIPv4, IPPacketInformation* packetInfo);
    }
}
