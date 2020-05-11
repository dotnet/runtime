// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security.Authentication;

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
        KeyEpdate = 24,
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

    internal struct TlsFrameHeader
    {
        public TlsContentType Type;
        public SslProtocols Version;
        public int Length;
    }

    internal struct TlsFrameHandshakeInfo
    {
        public TlsFrameHeader Header;
        public TlsHandshakeType HandshakeType;
        public SslProtocols SupportedVersions;
        public string? TargetName;
    }

    internal class TlsFrameHelper
    {
        public const int HeaderSize = 5;

        private static byte[] s_protocolMismatch13 = new byte[] { (byte)TlsContentType.Alert, 3, 4, 0, 2, 2, 70 };
        private static byte[] s_protocolMismatch12 = new byte[] { (byte)TlsContentType.Alert, 3, 3, 0, 2, 2, 70 };
        private static byte[] s_protocolMismatch11 = new byte[] { (byte)TlsContentType.Alert, 3, 2, 0, 2, 2, 70 };
        private static byte[] s_protocolMismatch10 = new byte[] { (byte)TlsContentType.Alert, 3, 1, 0, 2, 2, 70 };
        private static byte[] s_protocolMismatch30 = new byte[] { (byte)TlsContentType.Alert, 3, 0, 0, 2, 2, 40 };

        public static bool TryGetFrameHeader(ReadOnlySpan<byte> frame, ref TlsFrameHeader header)
        {
            bool result = frame.Length > 4;

            if (frame.Length >= 1)
            {
                header.Type = (TlsContentType)frame[0];

                if (frame.Length >= 3)
                {
                    // SSLv3, TLS or later
                    if (frame[1] == 3)
                    {
                        if (frame.Length > 4)
                        {
                            header.Length = ((frame[3] << 8) | frame[4]);
                        }

                        switch (frame[2])
                        {
                            case 4:
                                header.Version = SslProtocols.Tls13;
                                break;
                            case 3:
                                header.Version = SslProtocols.Tls12;
                                break;
                            case 2:
                                header.Version = SslProtocols.Tls11;
                                break;
                            case 1:
                                header.Version = SslProtocols.Tls;
                                break;
                            case 0:
#pragma warning disable 0618
                                header.Version = SslProtocols.Ssl3;
#pragma warning restore 0618
                                break;
                            default:
                                header.Version = SslProtocols.None;
                                break;
                        }
                    }
                    else
                    {
                        header.Length = -1;
                        header.Version = SslProtocols.None;
                    }
                }
            }

            return result;
        }

        // Returns frame size e.g. header + content
        public static int GetFrameSize(ReadOnlySpan<byte> frame)
        {
            if (frame.Length < 5 || frame[1] < 3)
            {
                return - 1;
            }

            return ((frame[3] << 8) | frame[4]) + HeaderSize;
        }

        public static bool TryGetHandshakeInfo(ReadOnlySpan<byte> frame, ref TlsFrameHandshakeInfo info)
        {
            if (frame.Length < 6 || frame[0] != (byte)TlsContentType.Handshake)
            {
                return false;
            }

            // This will not fail since we have enough data.
            bool gotHeader = TryGetFrameHeader(frame, ref info.Header);
            Debug.Assert(gotHeader);

            info.SupportedVersions = info.Header.Version;

            info.HandshakeType = (TlsHandshakeType)frame[5];

            if (info.HandshakeType == TlsHandshakeType.ClientHello)
            {
                info.TargetName = SniHelper.GetServerName(frame);
            }

            return true;
        }

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
                SslProtocols.Tls11 => s_protocolMismatch11,
                SslProtocols.Tls => s_protocolMismatch10,
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
            else if ((int)version > (int)SslProtocols.Tls)
            {
                // Create TLS1.2 alert
                byte[] buffer = new byte[] { (byte)TlsContentType.Alert, 3, 3, 0, 2, 2, (byte)reason };
                switch (version)
                {
                    case SslProtocols.Tls13:
                        buffer[2] = 4;
                        break;
                    case SslProtocols.Tls11:
                        buffer[2] = 2;
                        break;
                    case SslProtocols.Tls:
                        buffer[2] = 1;
                        break;
                }

                return buffer;
            }

            return Array.Empty<byte>();
        }
    }
}
