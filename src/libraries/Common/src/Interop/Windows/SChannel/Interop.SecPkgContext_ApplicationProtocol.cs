// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal enum ApplicationProtocolNegotiationStatus
    {
        None = 0,
        Success,
        SelectedClientOnly
    }

    internal enum ApplicationProtocolNegotiationExt
    {
        None = 0,
        NPN,
        ALPN
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SecPkgContext_ApplicationProtocol
    {
        private const int MaxProtocolIdSize = 0xFF;

        public ApplicationProtocolNegotiationStatus ProtoNegoStatus;
        public ApplicationProtocolNegotiationExt ProtoNegoExt;
        public byte ProtocolIdSize;
        public fixed byte ProtocolId[MaxProtocolIdSize];
        public ReadOnlySpan<byte> Protocol =>
            MemoryMarshal.CreateReadOnlySpan(ref ProtocolId[0], ProtocolIdSize);
    }
}
