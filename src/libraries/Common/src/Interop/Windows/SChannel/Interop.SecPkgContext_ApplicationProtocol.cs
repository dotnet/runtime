// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal class SecPkgContext_ApplicationProtocol
    {
        private const int MAX_PROTOCOL_ID_SIZE = 0xFF;
        public ApplicationProtocolNegotiationStatus NegotiationStatus;
        public ApplicationProtocolNegotiationExt NegotiationExtension;
        public byte ProtocolIdSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_PROTOCOL_ID_SIZE)]
        public char[] ProtocolId;

        public string GetProtocolId()
        {
            if (ProtocolId == null)
                return null;

            return new string(ProtocolId, 0, ProtocolIdSize);
        }
    }
}
