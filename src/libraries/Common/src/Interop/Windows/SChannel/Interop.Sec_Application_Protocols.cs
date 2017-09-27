// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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

        public static unsafe byte[] ToByteArray(IList<SslApplicationProtocol> applicationProtocols)
        {
            long protocolListSize = 0;
            foreach (SslApplicationProtocol protocol in applicationProtocols)
            {
                if (protocol.Protocol.Length == 0 || protocol.Protocol.Length > byte.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                }

                protocolListSize += protocol.Protocol.Length + 1;

                if (protocolListSize > short.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                }
            }

            Sec_Application_Protocols protocols = new Sec_Application_Protocols();
            protocols.ProtocolListsSize = (uint)(ProtocolListConstSize + protocolListSize);
            protocols.ProtocolExtenstionType = ApplicationProtocolNegotiationExt.ALPN;
            protocols.ProtocolListSize = (short)protocolListSize;

            byte[] buffer = new byte[ProtocolListOffset + protocolListSize];
            fixed (byte* bufferPtr = buffer)
            {
                Marshal.StructureToPtr(protocols, new IntPtr(bufferPtr), false);
                byte* protocolList = bufferPtr + ProtocolListOffset;
                foreach (SslApplicationProtocol applicationProtocol in applicationProtocols)
                {
                    *(protocolList++) = (byte)applicationProtocol.Protocol.Length;
                    fixed (byte* p = applicationProtocol.Protocol.ToArray())
                    {
                        Buffer.MemoryCopy(p, protocolList, buffer.Length, applicationProtocol.Protocol.Length);
                    }

                    protocolList += applicationProtocol.Protocol.Length;
                }
            }

            return buffer;
        }
    }
}
