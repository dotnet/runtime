// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal class SecPkgContext_ApplicationProtocol
    {
        private const int MaxProtocolIdSize = 0xFF;

        public ApplicationProtocolNegotiationStatus ProtoNegoStatus;
        public ApplicationProtocolNegotiationExt ProtoNegoExt;
        public byte ProtocolIdSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxProtocolIdSize)]
        public byte[] ProtocolId;
        public byte[] Protocol
        {
            get
            {
                return new Span<byte>(ProtocolId, 0, ProtocolIdSize).ToArray();
            }
        }
    }
}
