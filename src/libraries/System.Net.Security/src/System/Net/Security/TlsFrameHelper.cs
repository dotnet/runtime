// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Authentication;
using System.Text;

namespace System.Net.Security
{
    // SSL3/TLS protocol frames definitions.
    internal enum TlsContentType : byte
    {
        ChangeCipherSpec = 20,
        Alert = 21,
        Handshake = 22,
        AppData = 23
    }

    internal enum TlsHandshakeType : byte
    {
        HelloRequest = 0,
        ClientHello = 1,
        ServerHello = 2,
        NewSessionTicket = 4,
        EndOfEarlyData = 5,
        EncryptedExtensions = 8,
        Certificate = 11,
        ServerKeyExchange = 12,
        CertificateRequest = 13,
        ServerHelloDone = 14,
        CertificateVerify = 15,
        ClientKeyExchange = 16,
        Finished = 20,
        KeyUpdate = 24,
        MessageHash = 254
    }

    internal enum TlsAlertLevel : byte
    {
        Warning = 1,
        Fatal = 2,
    }

    internal enum TlsAlertDescription : byte
    {
        CloseNotify = 0, // warning
        UnexpectedMessage = 10, // error
        BadRecordMac = 20, // error
        DecryptionFailed = 21, // reserved
        RecordOverflow = 22, // error
        DecompressionFail = 30, // error
        HandshakeFailure = 40, // error
        BadCertificate = 42, // warning or error
        UnsupportedCert = 43, // warning or error
        CertificateRevoked = 44, // warning or error
        CertificateExpired = 45, // warning or error
        CertificateUnknown = 46, // warning or error
        IllegalParameter = 47, // error
        UnknownCA = 48, // error
        AccessDenied = 49, // error
        DecodeError = 50, // error
        DecryptError = 51, // error
        ExportRestriction = 60, // reserved
        ProtocolVersion = 70, // error
        InsuffientSecurity = 71, // error
        InternalError = 80, // error
        UserCanceled = 90, // warning or error
        NoRenegotiation = 100, // warning
        UnsupportedExt = 110, // error
    }

    internal enum ExtensionType : ushort
    {
        ServerName = 0,
        MaximumFagmentLength = 1,
        ClientCertificateUrl = 2,
        TrustedCaKeys = 3,
        TruncatedHmac = 4,
        CertificateStatusRequest = 5,
        ApplicationProtocols = 16,
        SupportedVersions = 43
    }

    internal struct TlsFrameHeader
    {
        public TlsContentType Type;
        public SslProtocols Version;
        public int Length;

        public override string ToString() => $"{Version}:{Type}[{Length}]";
    }

    internal static class TlsFrameHelper
    {
        public const int HeaderSize = 5;

        [Flags]
        public enum ProcessingOptions
        {
            All = 0,
            ServerName = 0x1,
            ApplicationProtocol = 0x2,
            Versions = 0x4,
        }

        [Flags]
        public enum ApplicationProtocolInfo
        {
            None = 0,
            Http11 = 1,
            Http2 = 2,
            Other = 128
        }

        public struct TlsFrameInfo
        {
            public TlsFrameHeader Header;
            public TlsHandshakeType HandshakeType;
            public SslProtocols SupportedVersions;
            public string TargetName;
            public ApplicationProtocolInfo ApplicationProtocols;
            public TlsAlertDescription AlertDescription;

            public override string ToString()
            {
                if (Header.Type == TlsContentType.Handshake)
                {
                    if (HandshakeType == TlsHandshakeType.ClientHello)
                    {
                        return $"{Header.Version}:{HandshakeType}[{Header.Length}] TargetName='{TargetName}' SupportedVersion='{SupportedVersions}' ApplicationProtocols='{ApplicationProtocols}'";
                    }
                    else if (HandshakeType == TlsHandshakeType.ServerHello)
                    {
                        return $"{Header.Version}:{HandshakeType}[{Header.Length}] SupportedVersion='{SupportedVersions}' ApplicationProtocols='{ApplicationProtocols}'";
                    }
                    else
                    {
                        return $"{Header.Version}:{HandshakeType}[{Header.Length}] SupportedVersion='{SupportedVersions}'";
                    }
                }
                else
                {
                    return $"{Header.Version}:{Header.Type}[{Header.Length}]";
                }
            }
        }

        public delegate bool HelloExtensionCallback(ref TlsFrameInfo info, ExtensionType type, ReadOnlySpan<byte> extensionsData);

        private static readonly byte[] s_protocolMismatch13 = new byte[] { (byte)TlsContentType.Alert, 3, 4, 0, 2, 2, 70 };
        private static readonly byte[] s_protocolMismatch12 = new byte[] { (byte)TlsContentType.Alert, 3, 3, 0, 2, 2, 70 };
        private static readonly byte[] s_protocolMismatch11 = new byte[] { (byte)TlsContentType.Alert, 3, 2, 0, 2, 2, 70 };
        private static readonly byte[] s_protocolMismatch10 = new byte[] { (byte)TlsContentType.Alert, 3, 1, 0, 2, 2, 70 };
        private static readonly byte[] s_protocolMismatch30 = new byte[] { (byte)TlsContentType.Alert, 3, 0, 0, 2, 2, 40 };

        private const int UInt24Size = 3;
        private const int RandomSize = 32;
        private const int ProtocolVersionMajorOffset = 0;
        private const int ProtocolVersionMinorOffset = 1;
        private const int ProtocolVersionSize = 2;
        private const int ProtocolVersionTlsMajorValue = 3;

        // Per spec "AllowUnassigned flag MUST be set". See comment above DecodeString() for more details.
        private static readonly IdnMapping s_idnMapping = new IdnMapping() { AllowUnassigned = true };
        private static readonly Encoding s_encoding = Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback());

        public static bool TryGetFrameHeader(ReadOnlySpan<byte> frame, ref TlsFrameHeader header)
        {
            if (frame.Length < HeaderSize)
            {
                header.Length= -1;
                return false;
            }

            header.Type = (TlsContentType)frame[0];

            // SSLv3, TLS or later
            if (frame[1] == 3)
            {
                header.Length = ((frame[3] << 8) | frame[4]) + HeaderSize;
                header.Version = TlsMinorVersionToProtocol(frame[2]);
            }
            else if (frame[2] == (byte)TlsHandshakeType.ClientHello &&
                     frame[3] == 3) // SSL3 or above
            {
                int length;
                if ((frame[0] & 0x80) != 0)
                {
                            // Two bytes
                    length = (((frame[0] & 0x7f) << 8) | frame[1]) + 2;
                }
                else
                {
                            // Three bytes
                    length = (((frame[0] & 0x3f) << 8) | frame[1]) + 3;
                }


                // max frame for SSLv2 is 32767.
                // However, we expect something reasonable for initial HELLO
                // We don't have enough logic to verify full validity,
                // the limits bellow are queses.
#pragma warning disable CS0618 // Ssl2 and Ssl3 are obsolete
                header.Version = SslProtocols.Ssl2;
#pragma warning restore CS0618
                header.Length = length;
                header.Type = TlsContentType.Handshake;
            }
            else
            {
                header.Length = -1;
            }

            return true;
        }

        // This function will try to parse TLS hello frame and fill details in provided info structure.
        // If frame was fully processed without any error, function returns true.
        // Otherwise it returns false and info may have partial data.
        // It is OK to call it again if more data becomes available.
        // It is also possible to limit what information is processed.
        // If callback delegate is provided, it will be called on ALL extensions.
        public static bool TryGetFrameInfo(ReadOnlySpan<byte> frame, ref TlsFrameInfo info, ProcessingOptions options = ProcessingOptions.All, HelloExtensionCallback? callback = null)
        {
            const int HandshakeTypeOffset = 5;
            if (frame.Length < HeaderSize)
            {
                return false;
            }

            // This will not fail since we have enough data.
            bool gotHeader = TryGetFrameHeader(frame, ref info.Header);
            Debug.Assert(gotHeader);

            info.SupportedVersions = info.Header.Version;

            if (info.Header.Type == TlsContentType.Alert)
            {
                TlsAlertLevel level = default;
                TlsAlertDescription description = default;
                if (TryGetAlertInfo(frame, ref level, ref description))
                {
                    info.AlertDescription = description;
                    return true;
                }

                return false;
            }

            if (info.Header.Type != TlsContentType.Handshake || frame.Length <= HandshakeTypeOffset)
            {
                return false;
            }

            info.HandshakeType = (TlsHandshakeType)frame[HandshakeTypeOffset];
#pragma warning disable CS0618 // Ssl2 and Ssl3 are obsolete
            if (info.Header.Version == SslProtocols.Ssl2)
            {
                // This is safe. We would not get here if the length is too small.
                info.SupportedVersions |= TlsMinorVersionToProtocol(frame[4]);
                // We only recognize Unified ClientHello at the moment.
                // This is needed to trigger certificate selection callback in SslStream.
                info.HandshakeType = TlsHandshakeType.ClientHello;
                // There is no more parsing for old protocols.
                return true;
            }
#pragma warning restore CS0618

            // Check if we have full frame.
            bool isComplete = frame.Length >= info.Header.Length;

#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            if (((int)info.Header.Version >= (int)SslProtocols.Tls) &&
#pragma warning restore SYSLIB0039
                (info.HandshakeType == TlsHandshakeType.ClientHello || info.HandshakeType == TlsHandshakeType.ServerHello))
            {
                if (!TryParseHelloFrame(frame.Slice(HeaderSize), ref info, options, callback))
                {
                    isComplete = false;
                }
            }

            return isComplete;
        }

        // This is similar to TryGetFrameInfo but it will only process SNI.
        // It returns TargetName as string or NULL if SNI is missing or parsing error happened.
        public static string? GetServerName(ReadOnlySpan<byte> frame)
        {
            TlsFrameInfo info = default;
            if (!TryGetFrameInfo(frame, ref info, ProcessingOptions.ServerName))
            {
                return null;
            }

            return info.TargetName;
        }

        // This function will parse TLS Alert message and it will return alert level and description.
        public static bool TryGetAlertInfo(ReadOnlySpan<byte> frame, ref TlsAlertLevel level, ref TlsAlertDescription description)
        {
            if (frame.Length < 7 || frame[0] != (byte)TlsContentType.Alert)
            {
                return false;
            }

            level = (TlsAlertLevel)frame[5];
            description = (TlsAlertDescription)frame[6];

            return true;
        }

        private static byte[] CreateProtocolVersionAlert(SslProtocols version) =>
            version switch
            {
                SslProtocols.Tls13 => s_protocolMismatch13,
                SslProtocols.Tls12 => s_protocolMismatch12,
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                SslProtocols.Tls11 => s_protocolMismatch11,
                SslProtocols.Tls => s_protocolMismatch10,
#pragma warning restore SYSLIB0039
#pragma warning disable 0618
                SslProtocols.Ssl3 => s_protocolMismatch30,
#pragma warning restore 0618
                _ => Array.Empty<byte>(),
            };

        public static byte[] CreateAlertFrame(SslProtocols version, TlsAlertDescription reason)
        {
            if (reason == TlsAlertDescription.ProtocolVersion)
            {
                return CreateProtocolVersionAlert(version);
            }
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            else if ((int)version > (int)SslProtocols.Tls)
#pragma warning restore SYSLIB0039
            {
                // Create TLS1.2 alert
                byte[] buffer = new byte[] { (byte)TlsContentType.Alert, 3, 3, 0, 2, 2, (byte)reason };
                switch (version)
                {
                    case SslProtocols.Tls13:
                        buffer[2] = 4;
                        break;
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    case SslProtocols.Tls11:
                        buffer[2] = 2;
                        break;
                    case SslProtocols.Tls:
                        buffer[2] = 1;
                        break;
#pragma warning restore SYSLIB0039
                }

                return buffer;
            }

            return Array.Empty<byte>();
        }

        private static bool TryParseHelloFrame(ReadOnlySpan<byte> sslHandshake, ref TlsFrameInfo info, ProcessingOptions options, HelloExtensionCallback? callback)
        {
            // https://tools.ietf.org/html/rfc6101#section-5.6
            // struct {
            //     HandshakeType msg_type;    /* handshake type */
            //     uint24 length;             /* bytes in message */
            //     select (HandshakeType) {
            //         ...
            //         case client_hello: ClientHello;
            //         case server_hello: ServerHello;
            //         ...
            //     } body;
            // } Handshake;
            const int HandshakeTypeOffset = 0;
            const int HelloLengthOffset = HandshakeTypeOffset + sizeof(TlsHandshakeType);
            const int HelloOffset = HelloLengthOffset + UInt24Size;

            if (sslHandshake.Length < HelloOffset ||
                ((TlsHandshakeType)sslHandshake[HandshakeTypeOffset] != TlsHandshakeType.ClientHello &&
                 (TlsHandshakeType)sslHandshake[HandshakeTypeOffset] != TlsHandshakeType.ServerHello))
            {
                return false;
            }

            int helloLength = ReadUInt24BigEndian(sslHandshake.Slice(HelloLengthOffset));
            ReadOnlySpan<byte> helloData = sslHandshake.Slice(HelloOffset);

            if (helloData.Length < helloLength)
            {
                return false;
            }

            // ProtocolVersion may be different from frame header.
            if (helloData[ProtocolVersionMajorOffset] == ProtocolVersionTlsMajorValue)
            {
                info.SupportedVersions |= TlsMinorVersionToProtocol(helloData[ProtocolVersionMinorOffset]);
            }

            return (TlsHandshakeType)sslHandshake[HandshakeTypeOffset] == TlsHandshakeType.ClientHello ?
                        TryParseClientHello(helloData.Slice(0, helloLength), ref info, options, callback) :
                        TryParseServerHello(helloData.Slice(0, helloLength), ref info, options, callback);
        }

        private static bool TryParseClientHello(ReadOnlySpan<byte> clientHello, ref TlsFrameInfo info, ProcessingOptions options, HelloExtensionCallback? callback)
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
            p = SkipOpaqueType2(p);

            // Skip compression methods (max size 2^8-1 => size fits in 1 byte)
            p = SkipOpaqueType1(p);

            // no extensions
            if (p.IsEmpty)
            {
                return true;
            }

            // client_hello_extension_list (max size 2^16-1 => size fits in 2 bytes)
            int extensionListLength = BinaryPrimitives.ReadUInt16BigEndian(p);
            p = SkipBytes(p, sizeof(ushort));
            if (extensionListLength != p.Length)
            {
                return false;
            }

            return TryParseHelloExtensions(p, ref info, options, callback);
        }

        private static bool TryParseServerHello(ReadOnlySpan<byte> serverHello, ref TlsFrameInfo info, ProcessingOptions options, HelloExtensionCallback? callback)
        {
            // Basic structure: https://tools.ietf.org/html/rfc6101#section-5.6.1.3
            // Extended structure: https://tools.ietf.org/html/rfc3546#section-2.2
            // struct {
            //   ProtocolVersion server_version;
            //   Random random;
            //   SessionID session_id;
            //   CipherSuite cipher_suite;
            //   CompressionMethod compression_method;
            //   Extension server_hello_extension_list<0..2^16-1>;
            // }
            // ServerHello;
            const int CipherSuiteLength = 2;
            const int CompressionMethiodLength = 1;

            ReadOnlySpan<byte> p = SkipBytes(serverHello, ProtocolVersionSize + RandomSize);
            // Skip SessionID (max size 32 => size fits in 1 byte)
            p = SkipOpaqueType1(p);
            p = SkipBytes(p, CipherSuiteLength + CompressionMethiodLength);

            // is invalid structure or no extensions?
            if (p.IsEmpty)
            {
                return false;
            }

            // client_hello_extension_list (max size 2^16-1 => size fits in 2 bytes)
            int extensionListLength = BinaryPrimitives.ReadUInt16BigEndian(p);
            p = SkipBytes(p, sizeof(ushort));
            if (extensionListLength != p.Length)
            {
                return false;
            }

            return TryParseHelloExtensions(p, ref info, options, callback);
        }

        // This is common for ClientHello and ServerHello.
        private static bool TryParseHelloExtensions(ReadOnlySpan<byte> extensions, ref TlsFrameInfo info, ProcessingOptions options, HelloExtensionCallback? callback)
        {
            const int ExtensionHeader = 4;
            bool isComplete = true;

            while (extensions.Length >= ExtensionHeader)
            {
                ExtensionType extensionType = (ExtensionType)BinaryPrimitives.ReadUInt16BigEndian(extensions);
                extensions = SkipBytes(extensions, sizeof(ushort));

                ushort extensionLength = BinaryPrimitives.ReadUInt16BigEndian(extensions);
                extensions = SkipBytes(extensions, sizeof(ushort));
                if (extensions.Length < extensionLength)
                {
                    isComplete = false;
                    break;
                }

                ReadOnlySpan<byte> extensionData = extensions.Slice(0, extensionLength);

                if (extensionType == ExtensionType.ServerName && (options == ProcessingOptions.All ||
                   (options & ProcessingOptions.ServerName) == ProcessingOptions.ServerName))
                {
                    if (!TryGetSniFromServerNameList(extensionData, out string? sni))
                    {
                        return false;
                    }

                    info.TargetName = sni!;
                }
                else if (extensionType == ExtensionType.SupportedVersions && (options == ProcessingOptions.All ||
                          (options & ProcessingOptions.Versions) == ProcessingOptions.Versions))
                {
                    if (!TryGetSupportedVersionsFromExtension(extensionData, out SslProtocols versions))
                    {
                        return false;
                    }

                    info.SupportedVersions |= versions;
                }
                else if (extensionType == ExtensionType.ApplicationProtocols && (options == ProcessingOptions.All ||
                          (options & ProcessingOptions.ApplicationProtocol) == ProcessingOptions.ApplicationProtocol))
                {
                    if (!TryGetApplicationProtocolsFromExtension(extensionData, out ApplicationProtocolInfo alpn))
                    {
                        return false;
                    }

                    info.ApplicationProtocols |= alpn;
                }

                callback?.Invoke(ref info, extensionType, extensionData);
                extensions = extensions.Slice(extensionLength);
            }

            return isComplete;
        }

        private static bool TryGetSniFromServerNameList(ReadOnlySpan<byte> serverNameListExtension, out string? sni)
        {
            // https://tools.ietf.org/html/rfc3546#section-3.1
            // struct {
            //     ServerName server_name_list<1..2^16-1>
            // } ServerNameList;
            // ServerNameList is an opaque type (length of sufficient size for max data length is prepended)
            const int ServerNameListOffset = sizeof(ushort);
            sni = null;

            if (serverNameListExtension.Length < ServerNameListOffset)
            {
                return false;
            }

            int serverNameListLength = BinaryPrimitives.ReadUInt16BigEndian(serverNameListExtension);
            ReadOnlySpan<byte> serverNameList = serverNameListExtension.Slice(ServerNameListOffset);

            if (serverNameListLength != serverNameList.Length)
            {
                return false;
            }

            ReadOnlySpan<byte> serverName = serverNameList.Slice(0, serverNameListLength);

            sni = GetSniFromServerName(serverName, out bool invalid);
            return invalid == false;
        }

        private static string? GetSniFromServerName(ReadOnlySpan<byte> serverName, out bool invalid)
        {
            // https://tools.ietf.org/html/rfc3546#section-3.1
            // struct {
            //     NameType name_type;
            //     select (name_type) {
            //         case host_name: HostName;
            //     } name;
            // } ServerName;
            // ServerName is an opaque type (length of sufficient size for max data length is prepended)
            const int NameTypeOffset = 0;
            const int HostNameStructOffset = NameTypeOffset + sizeof(NameType);
            if (serverName.Length < HostNameStructOffset)
            {
                invalid = true;
                return null;
            }

            // Following can underflow but it is ok due to equality check below
            NameType nameType = (NameType)serverName[NameTypeOffset];
            ReadOnlySpan<byte> hostNameStruct = serverName.Slice(HostNameStructOffset);
            if (nameType != NameType.HostName)
            {
                invalid = true;
                return null;
            }

            return GetSniFromHostNameStruct(hostNameStruct, out invalid);
        }

        private static string? GetSniFromHostNameStruct(ReadOnlySpan<byte> hostNameStruct, out bool invalid)
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

        private static bool TryGetSupportedVersionsFromExtension(ReadOnlySpan<byte> extensionData, out SslProtocols protocols)
        {
            // https://tools.ietf.org/html/rfc8446#section-4.2.1
            // struct {
            // select(Handshake.msg_type) {
            //  case client_hello:
            //    ProtocolVersion versions<2..254 >;
            //
            //  case server_hello: /* and HelloRetryRequest */
            //    ProtocolVersion selected_version;
            // };
            const int VersionListLengthOffset = 0;
            const int VersionListNameOffset = VersionListLengthOffset + sizeof(byte);
            const int VersionLength = 2;

            protocols = SslProtocols.None;

            byte supportedVersionLength = extensionData[VersionListLengthOffset];
            extensionData = extensionData.Slice(VersionListNameOffset);

            if (extensionData.Length != supportedVersionLength)
            {
                return false;
            }

            // Get list of protocols we support.I nore the rest.
            while (extensionData.Length >= VersionLength)
            {
                if (extensionData[ProtocolVersionMajorOffset] == ProtocolVersionTlsMajorValue)
                {
                    protocols |= TlsMinorVersionToProtocol(extensionData[ProtocolVersionMinorOffset]);
                }

                extensionData = extensionData.Slice(VersionLength);
            }

            return true;
        }

        private static bool TryGetApplicationProtocolsFromExtension(ReadOnlySpan<byte> extensionData, out ApplicationProtocolInfo alpn)
        {
            // https://tools.ietf.org/html/rfc7301#section-3.1
            // opaque ProtocolName<1..2 ^ 8 - 1 >;
            //
            // struct {
            //   ProtocolName protocol_name_list<2..2^16-1>
            // }
            // ProtocolNameList;
            const int AlpnListLengthOffset = 0;
            const int AlpnListOffset = AlpnListLengthOffset + sizeof(short);

            alpn = ApplicationProtocolInfo.None;

            if (extensionData.Length < AlpnListOffset)
            {
                return false;
            }

            int AlpnListLength = BinaryPrimitives.ReadUInt16BigEndian(extensionData);
            ReadOnlySpan<byte> alpnList = extensionData.Slice(AlpnListOffset);
            if (AlpnListLength != alpnList.Length)
            {
                return false;
            }

            while (!alpnList.IsEmpty)
            {
                byte protocolLength = alpnList[0];
                if (alpnList.Length < protocolLength + 1)
                {
                    return false;
                }

                ReadOnlySpan<byte> protocol = alpnList.Slice(1, protocolLength);
                if (protocolLength == 2)
                {
                    if (protocol.SequenceEqual(SslApplicationProtocol.Http2.Protocol.Span))
                    {
                        alpn |= ApplicationProtocolInfo.Http2;
                    }
                    else
                    {
                        alpn |= ApplicationProtocolInfo.Other;
                    }
                }
                else if (protocolLength == SslApplicationProtocol.Http11.Protocol.Length &&
                         protocol.SequenceEqual(SslApplicationProtocol.Http11.Protocol.Span))
                {
                    alpn |= ApplicationProtocolInfo.Http11;
                }
                else
                {
                    alpn |= ApplicationProtocolInfo.Other;
                }

                alpnList = alpnList.Slice(protocolLength + 1);
            }

            return true;
        }

        private static SslProtocols TlsMinorVersionToProtocol(byte value)
        {
            return value switch
            {
                4 => SslProtocols.Tls13,
                3 => SslProtocols.Tls12,
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                2 => SslProtocols.Tls11,
                1 => SslProtocols.Tls,
#pragma warning restore SYSLIB0039
#pragma warning disable 0618
                0 => SslProtocols.Ssl3,
#pragma warning restore 0618
                _ => SslProtocols.None,
            };
        }

        private static string? DecodeString(ReadOnlySpan<byte> bytes)
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

        private static ReadOnlySpan<byte> SkipOpaqueType2(ReadOnlySpan<byte> bytes)
        {
            const int OpaqueTypeLengthSize = sizeof(ushort);
            if (bytes.Length < OpaqueTypeLengthSize)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            ushort length = BinaryPrimitives.ReadUInt16BigEndian(bytes);
            int totalBytes = OpaqueTypeLengthSize + length;

            return SkipBytes(bytes, totalBytes);
        }

        private enum NameType : byte
        {
            HostName = 0x00
        }
    }
}
