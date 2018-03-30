// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace System.Net.Security
{
    internal class SniHelper
    {
        private const int ProtocolVersionSize = 2;
        private const int UInt24Size = 3;
        private const int RandomSize = 32;
        private readonly static IdnMapping s_idnMapping = CreateIdnMapping();
        private readonly static Encoding s_encoding = CreateEncoding();

        public static string GetServerName(byte[] clientHello)
        {
            return GetSniFromSslPlainText(clientHello);
        }

        private static string GetSniFromSslPlainText(ReadOnlySpan<byte> sslPlainText)
        {
            // https://tools.ietf.org/html/rfc6101#section-5.2.1
            // struct {
            //     ContentType type; // enum with max value 255
            //     ProtocolVersion version; // 2x uint8
            //     uint16 length;
            //     opaque fragment[SSLPlaintext.length];
            // } SSLPlaintext;
            const int ContentTypeOffset = 0;
            const int ProtocolVersionOffset = ContentTypeOffset + sizeof(ContentType);
            const int LengthOffset = ProtocolVersionOffset + ProtocolVersionSize;
            const int HandshakeOffset = LengthOffset + sizeof(ushort);

            // SSL v2's ContentType has 0x80 bit set.
            // We do not care about SSL v2 here because it does not support client hello extensions
            if (sslPlainText.Length < HandshakeOffset || (ContentType)sslPlainText[ContentTypeOffset] != ContentType.Handshake)
            {
                return null;
            }

            // Skip ContentType and ProtocolVersion
            int handshakeLength = BinaryPrimitives.ReadUInt16BigEndian(sslPlainText.Slice(LengthOffset));
            ReadOnlySpan<byte> sslHandshake = sslPlainText.Slice(HandshakeOffset);

            if (handshakeLength != sslHandshake.Length)
            {
                return null;
            }

            return GetSniFromSslHandshake(sslHandshake);
        }

        private static string GetSniFromSslHandshake(ReadOnlySpan<byte> sslHandshake)
        {
            // https://tools.ietf.org/html/rfc6101#section-5.6
            // struct {
            //     HandshakeType msg_type;    /* handshake type */
            //     uint24 length;             /* bytes in message */
            //     select (HandshakeType) {
            //         ...
            //         case client_hello: ClientHello;
            //         ...
            //     } body;
            // } Handshake;
            const int HandshakeTypeOffset = 0;
            const int ClientHelloLengthOffset = HandshakeTypeOffset + sizeof(HandshakeType);
            const int ClientHelloOffset = ClientHelloLengthOffset + UInt24Size;

            if (sslHandshake.Length < ClientHelloOffset || (HandshakeType)sslHandshake[HandshakeTypeOffset] != HandshakeType.ClientHello)
            {
                return null;
            }

            int clientHelloLength = ReadUInt24BigEndian(sslHandshake.Slice(ClientHelloLengthOffset));
            ReadOnlySpan<byte> clientHello = sslHandshake.Slice(ClientHelloOffset);

            if (clientHello.Length != clientHelloLength)
            {
                return null;
            }

            return GetSniFromClientHello(clientHello);
        }

        private static string GetSniFromClientHello(ReadOnlySpan<byte> clientHello)
        {
            // Basic structure: https://tools.ietf.org/html/rfc6101#section-5.6.1.2
            // Extended structure: https://tools.ietf.org/html/rfc3546#section-2.1
            // struct {
            //     ProtocolVersion client_version; // 2x uint8
            //     Random random; // 32 bytes
            //     SessionID session_id; // opaque type
            //     CipherSuite cipher_suites<2..2^16-1>; // opaque type
            //     CompressionMethod compression_methods<1..2^8-1>; // opaque type
            //     Extension client_hello_extension_list<0..2^16-1>;
            // } ClientHello;
            ReadOnlySpan<byte> p = SkipBytes(clientHello, ProtocolVersionSize + RandomSize);

            // Skip SessionID (max size 32 => size fits in 1 byte)
            p = SkipOpaqueType1(p);

            // Skip cipher suites (max size 2^16-1 => size fits in 2 bytes)
            p = SkipOpaqueType2(p, out _);

            // Skip compression methods (max size 2^8-1 => size fits in 1 byte)
            p = SkipOpaqueType1(p);

            // is invalid structure or no extensions?
            if (p.IsEmpty)
            {
                return null;
            }

            // client_hello_extension_list (max size 2^16-1 => size fits in 2 bytes)
            int extensionListLength = BinaryPrimitives.ReadUInt16BigEndian(p);
            p = SkipBytes(p, sizeof(ushort));

            if (extensionListLength != p.Length)
            {
                return null;
            }

            string ret = null;
            while (!p.IsEmpty)
            {
                bool invalid;
                string sni = GetSniFromExtension(p, out p, out invalid);
                if (invalid)
                {
                    return null;
                }

                if (ret != null && sni != null)
                {
                    return null;
                }

                if (sni != null)
                {
                    ret = sni;
                }
            }

            return ret;
        }

        private static string GetSniFromExtension(ReadOnlySpan<byte> extension, out ReadOnlySpan<byte> remainingBytes, out bool invalid)
        {
            // https://tools.ietf.org/html/rfc3546#section-2.3
            // struct {
            //     ExtensionType extension_type;
            //     opaque extension_data<0..2^16-1>;
            // } Extension;
            const int ExtensionDataOffset = sizeof(ExtensionType);

            if (extension.Length < ExtensionDataOffset)
            {
                remainingBytes = ReadOnlySpan<byte>.Empty;
                invalid = true;
                return null;
            }

            ExtensionType extensionType = (ExtensionType)BinaryPrimitives.ReadUInt16BigEndian(extension);
            ReadOnlySpan<byte> extensionData = extension.Slice(ExtensionDataOffset);

            if (extensionType == ExtensionType.ServerName)
            {
                return GetSniFromServerNameList(extensionData, out remainingBytes, out invalid);
            }
            else
            {
                remainingBytes = SkipOpaqueType2(extensionData, out invalid);
                return null;
            }
        }

        private static string GetSniFromServerNameList(ReadOnlySpan<byte> serverNameListExtension, out ReadOnlySpan<byte> remainingBytes, out bool invalid)
        {
            // https://tools.ietf.org/html/rfc3546#section-3.1
            // struct {
            //     ServerName server_name_list<1..2^16-1>
            // } ServerNameList;
            // ServerNameList is an opaque type (length of sufficient size for max data length is prepended)
            const int ServerNameListOffset = sizeof(ushort);

            if (serverNameListExtension.Length < ServerNameListOffset)
            {
                remainingBytes = ReadOnlySpan<byte>.Empty;
                invalid = true;
                return null;
            }

            int serverNameListLength = BinaryPrimitives.ReadUInt16BigEndian(serverNameListExtension);
            ReadOnlySpan<byte> serverNameList = serverNameListExtension.Slice(ServerNameListOffset);

            if (serverNameListLength > serverNameList.Length)
            {
                remainingBytes = ReadOnlySpan<byte>.Empty;
                invalid = true;
                return null;
            }

            remainingBytes = serverNameList.Slice(serverNameListLength);
            ReadOnlySpan<byte> serverName = serverNameList.Slice(0, serverNameListLength);

            return GetSniFromServerName(serverName, out invalid);
        }

        private static string GetSniFromServerName(ReadOnlySpan<byte> serverName, out bool invalid)
        {
            // https://tools.ietf.org/html/rfc3546#section-3.1
            // struct {
            //     NameType name_type;
            //     select (name_type) {
            //         case host_name: HostName;
            //     } name;
            // } ServerName;
            // ServerName is an opaque type (length of sufficient size for max data length is prepended)
            const int ServerNameLengthOffset = 0;
            const int NameTypeOffset = ServerNameLengthOffset + sizeof(ushort);
            const int HostNameStructOffset = NameTypeOffset + sizeof(NameType);

            if (serverName.Length < HostNameStructOffset)
            {
                invalid = true;
                return null;
            }

            // Following can underflow but it is ok due to equality check below
            int hostNameStructLength = BinaryPrimitives.ReadUInt16BigEndian(serverName) - sizeof(NameType);
            NameType nameType = (NameType)serverName[NameTypeOffset];
            ReadOnlySpan<byte> hostNameStruct = serverName.Slice(HostNameStructOffset);

            if (hostNameStructLength != hostNameStruct.Length || nameType != NameType.HostName)
            {
                invalid = true;
                return null;
            }

            return GetSniFromHostNameStruct(hostNameStruct, out invalid);
        }

        private static string GetSniFromHostNameStruct(ReadOnlySpan<byte> hostNameStruct, out bool invalid)
        {
            // https://tools.ietf.org/html/rfc3546#section-3.1
            // HostName is an opaque type (length of sufficient size for max data length is prepended)
            const int HostNameLengthOffset = 0;
            const int HostNameOffset = HostNameLengthOffset + sizeof(ushort);

            int hostNameLength = BinaryPrimitives.ReadUInt16BigEndian(hostNameStruct);
            ReadOnlySpan<byte> hostName = hostNameStruct.Slice(HostNameOffset);
            if (hostNameLength != hostName.Length)
            {
                invalid = true;
                return null;
            }

            invalid = false;
            return DecodeString(hostName);
        }

        private static string DecodeString(ReadOnlySpan<byte> bytes)
        {
            // https://tools.ietf.org/html/rfc3546#section-3.1
            // Per spec:
            //   If the hostname labels contain only US-ASCII characters, then the
            //   client MUST ensure that labels are separated only by the byte 0x2E,
            //   representing the dot character U+002E (requirement 1 in section 3.1
            //   of [IDNA] notwithstanding). If the server needs to match the HostName
            //   against names that contain non-US-ASCII characters, it MUST perform
            //   the conversion operation described in section 4 of [IDNA], treating
            //   the HostName as a "query string" (i.e. the AllowUnassigned flag MUST
            //   be set). Note that IDNA allows labels to be separated by any of the
            //   Unicode characters U+002E, U+3002, U+FF0E, and U+FF61, therefore
            //   servers MUST accept any of these characters as a label separator.  If
            //   the server only needs to match the HostName against names containing
            //   exclusively ASCII characters, it MUST compare ASCII names case-
            //   insensitively.

            string idnEncodedString;
            try
            {
                idnEncodedString = s_encoding.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }

            try
            {
                return s_idnMapping.GetUnicode(idnEncodedString);
            }
            catch (ArgumentException)
            {
                // client has not done IDN mapping
                return idnEncodedString;
            }
        }

        private static int ReadUInt24BigEndian(ReadOnlySpan<byte> bytes)
        {
            return (bytes[0] << 16) | (bytes[1] << 8) | bytes[2];
        }

        private static ReadOnlySpan<byte> SkipBytes(ReadOnlySpan<byte> bytes, int numberOfBytesToSkip)
        {
            return (numberOfBytesToSkip < bytes.Length) ? bytes.Slice(numberOfBytesToSkip) : ReadOnlySpan<byte>.Empty;
        }

        // Opaque type is of structure:
        //   - length (minimum number of bytes to hold the max value)
        //   - data (length bytes)
        // We will only use opaque types which are of max size: 255 (length = 1) or 2^16-1 (length = 2).
        // We will call them SkipOpaqueType`length`
        private static ReadOnlySpan<byte> SkipOpaqueType1(ReadOnlySpan<byte> bytes)
        {
            const int OpaqueTypeLengthSize = sizeof(byte);
            if (bytes.Length < OpaqueTypeLengthSize)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            byte length = bytes[0];
            int totalBytes = OpaqueTypeLengthSize + length;

            return SkipBytes(bytes, totalBytes);
        }

        private static ReadOnlySpan<byte> SkipOpaqueType2(ReadOnlySpan<byte> bytes, out bool invalid)
        {
            const int OpaqueTypeLengthSize = sizeof(ushort);
            if (bytes.Length < OpaqueTypeLengthSize)
            {
                invalid = true;
                return ReadOnlySpan<byte>.Empty;
            }

            ushort length = BinaryPrimitives.ReadUInt16BigEndian(bytes);
            int totalBytes = OpaqueTypeLengthSize + length;

            invalid = bytes.Length < totalBytes;
            if (invalid)
            {
                return ReadOnlySpan<byte>.Empty;
            }
            else
            {
                return bytes.Slice(totalBytes);
            }
        }

        private static IdnMapping CreateIdnMapping()
        {
            return new IdnMapping()
            {
                // Per spec "AllowUnassigned flag MUST be set". See comment above GetSniFromServerNameList for more details.
                AllowUnassigned = true
            };
        }

        private static Encoding CreateEncoding()
        {
            return Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback());
        }

        private enum ContentType : byte
        {
            Handshake = 0x16
        }

        private enum HandshakeType : byte
        {
            ClientHello = 0x01
        }

        private enum ExtensionType : ushort
        {
            ServerName = 0x00
        }

        private enum NameType : byte
        {
            HostName = 0x00
        }
    }
}
