// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
    internal struct SecPkgContext_ApplicationProtocol
    {
        private const int MaxProtocolIdSize = 0xFF;

        public ApplicationProtocolNegotiationStatus ProtoNegoStatus;
        public ApplicationProtocolNegotiationExt ProtoNegoExt;
        public byte ProtocolIdSize;
        public ProtocolIdBuffer ProtocolId;
        [UnscopedRef]
        public ReadOnlySpan<byte> Protocol =>
            ((ReadOnlySpan<byte>)ProtocolId).Slice(0, ProtocolIdSize);

        [InlineArray(MaxProtocolIdSize)]
        internal struct ProtocolIdBuffer
        {
            private byte _element0;
        }
    }
}
