// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Sec_Application_Protocols
    {
        private static readonly int ProtocolListOffset = Marshal.SizeOf<Sec_Application_Protocols>();
        private static readonly int ProtocolListConstSize = ProtocolListOffset - (int)Marshal.OffsetOf<Sec_Application_Protocols>(nameof(ProtocolExtenstionType));
        public uint ProtocolListsSize;
        public ApplicationProtocolNegotiationExt ProtocolExtenstionType;
        public short ProtocolListSize;

        public static unsafe byte[] ToByteArray(List<SslApplicationProtocol> applicationProtocols)
        {
            long protocolListSize = 0;
            for (int i = 0; i < applicationProtocols.Count; i++)
            {
                if (applicationProtocols[i].Protocol.Length == 0 || applicationProtocols[i].Protocol.Length > byte.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                }

                protocolListSize += applicationProtocols[i].Protocol.Length + 1;

                if (protocolListSize > short.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                }
            }

            Sec_Application_Protocols protocols = new Sec_Application_Protocols();
            protocols.ProtocolListsSize = (uint)(ProtocolListConstSize + protocolListSize);
            protocols.ProtocolExtenstionType = ApplicationProtocolNegotiationExt.ALPN;
            protocols.ProtocolListSize = (short)protocolListSize;

            Span<byte> pBuffer = new byte[protocolListSize];
            int index = 0;
            for (int i = 0; i < applicationProtocols.Count; i++)
            {
                pBuffer[index++] = (byte)applicationProtocols[i].Protocol.Length;
                applicationProtocols[i].Protocol.Span.CopyTo(pBuffer.Slice(index));
                index += applicationProtocols[i].Protocol.Length;
            }

            byte[] buffer = new byte[ProtocolListOffset + protocolListSize];
            fixed (byte* bufferPtr = buffer)
            {
                Marshal.StructureToPtr(protocols, new IntPtr(bufferPtr), false);
                byte* pList = bufferPtr + ProtocolListOffset;
                pBuffer.CopyTo(new Span<byte>(pList, index));
            }

            return buffer;
        }
    }
}
