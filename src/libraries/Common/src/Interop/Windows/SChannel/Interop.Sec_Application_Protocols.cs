// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public uint ProtocolListsSize;
        public ApplicationProtocolNegotiationExt ProtocolExtensionType;
        public short ProtocolListSize;

        public static int GetProtocolLength(List<SslApplicationProtocol> applicationProtocols)
        {
            int protocolListSize = 0;
            for (int i = 0; i < applicationProtocols.Count; i++)
            {
                int protocolLength = applicationProtocols[i].Protocol.Length;

                if (protocolLength == 0 || protocolLength > byte.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                }

                protocolListSize += protocolLength + 1;

                if (protocolListSize > short.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                }
            }

            return protocolListSize;
        }

        public static unsafe byte[] ToByteArray(List<SslApplicationProtocol> applicationProtocols)
        {
            int protocolListSize = GetProtocolLength(applicationProtocols);

            Sec_Application_Protocols protocols = default;

            int protocolListConstSize = sizeof(Sec_Application_Protocols) - sizeof(uint) /* offsetof(Sec_Application_Protocols, ProtocolExtensionType) */;
            protocols.ProtocolListsSize = (uint)(protocolListConstSize + protocolListSize);

            protocols.ProtocolExtensionType = ApplicationProtocolNegotiationExt.ALPN;
            protocols.ProtocolListSize = (short)protocolListSize;

            byte[] buffer = new byte[sizeof(Sec_Application_Protocols) + protocolListSize];
            int index = 0;

            MemoryMarshal.Write(buffer.AsSpan(index), ref protocols);
            index += sizeof(Sec_Application_Protocols);

            for (int i = 0; i < applicationProtocols.Count; i++)
            {
                ReadOnlySpan<byte> protocol = applicationProtocols[i].Protocol.Span;
                buffer[index++] = (byte)protocol.Length;
                protocol.CopyTo(buffer.AsSpan(index));
                index += protocol.Length;
            }

            return buffer;
        }

        public static unsafe void SetProtocols(Span<byte> buffer, List<SslApplicationProtocol> applicationProtocols, int protocolLength)
        {
            Span<Sec_Application_Protocols> alpn = MemoryMarshal.Cast<byte, Sec_Application_Protocols>(buffer);
            alpn[0].ProtocolListsSize = (uint)(sizeof(Sec_Application_Protocols) - sizeof(uint) + protocolLength);
            alpn[0].ProtocolExtensionType = ApplicationProtocolNegotiationExt.ALPN;
            alpn[0].ProtocolListSize = (short)protocolLength;

            Span<byte> data = buffer.Slice(sizeof(Sec_Application_Protocols));
            for (int i = 0; i < applicationProtocols.Count; i++)
            {
                ReadOnlySpan<byte> protocol = applicationProtocols[i].Protocol.Span;

                data[0] = (byte)protocol.Length;
                data = data.Slice(1);
                protocol.CopyTo(data);
                data = data.Slice(protocol.Length);
            }
        }
    }
}
