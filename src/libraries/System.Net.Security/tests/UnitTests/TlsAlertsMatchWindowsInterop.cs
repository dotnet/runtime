// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Security.Tests
{
    public class TlsAlertsMatchWindowsInterop
    {
        [Fact]
        public void TlsAlertEnums_MatchWindowsInterop_Ok()
        {
            Assert.Equal((int)TlsAlertType.Warning, (int)Interop.SChannel.TLS1_ALERT_WARNING);
            Assert.Equal((int)TlsAlertType.Fatal, (int)Interop.SChannel.TLS1_ALERT_FATAL);

            Assert.Equal((int)TlsAlertMessage.CloseNotify, Interop.SChannel.TLS1_ALERT_CLOSE_NOTIFY); 
            Assert.Equal((int)TlsAlertMessage.UnexpectedMessage, Interop.SChannel.TLS1_ALERT_UNEXPECTED_MESSAGE); 
            Assert.Equal((int)TlsAlertMessage.BadRecordMac, Interop.SChannel.TLS1_ALERT_BAD_RECORD_MAC); 
            Assert.Equal((int)TlsAlertMessage.DecryptionFailed, Interop.SChannel.TLS1_ALERT_DECRYPTION_FAILED); 
            Assert.Equal((int)TlsAlertMessage.RecordOverflow, Interop.SChannel.TLS1_ALERT_RECORD_OVERFLOW); 
            Assert.Equal((int)TlsAlertMessage.DecompressionFail, Interop.SChannel.TLS1_ALERT_DECOMPRESSION_FAIL); 
            Assert.Equal((int)TlsAlertMessage.HandshakeFailure, Interop.SChannel.TLS1_ALERT_HANDSHAKE_FAILURE); 
            Assert.Equal((int)TlsAlertMessage.BadCertificate, Interop.SChannel.TLS1_ALERT_BAD_CERTIFICATE); 
            Assert.Equal((int)TlsAlertMessage.UnsupportedCert, Interop.SChannel.TLS1_ALERT_UNSUPPORTED_CERT); 
            Assert.Equal((int)TlsAlertMessage.CertificateRevoked, Interop.SChannel.TLS1_ALERT_CERTIFICATE_REVOKED); 
            Assert.Equal((int)TlsAlertMessage.CertificateExpired, Interop.SChannel.TLS1_ALERT_CERTIFICATE_EXPIRED); 
            Assert.Equal((int)TlsAlertMessage.CertificateUnknown, Interop.SChannel.TLS1_ALERT_CERTIFICATE_UNKNOWN); 
            Assert.Equal((int)TlsAlertMessage.IllegalParameter, Interop.SChannel.TLS1_ALERT_ILLEGAL_PARAMETER); 
            Assert.Equal((int)TlsAlertMessage.UnknownCA, Interop.SChannel.TLS1_ALERT_UNKNOWN_CA); 
            Assert.Equal((int)TlsAlertMessage.AccessDenied, Interop.SChannel.TLS1_ALERT_ACCESS_DENIED); 
            Assert.Equal((int)TlsAlertMessage.DecodeError, Interop.SChannel.TLS1_ALERT_DECODE_ERROR); 
            Assert.Equal((int)TlsAlertMessage.DecryptError, Interop.SChannel.TLS1_ALERT_DECRYPT_ERROR); 
            Assert.Equal((int)TlsAlertMessage.ExportRestriction, Interop.SChannel.TLS1_ALERT_EXPORT_RESTRICTION); 
            Assert.Equal((int)TlsAlertMessage.ProtocolVersion, Interop.SChannel.TLS1_ALERT_PROTOCOL_VERSION); 
            Assert.Equal((int)TlsAlertMessage.InsuffientSecurity, Interop.SChannel.TLS1_ALERT_INSUFFIENT_SECURITY); 
            Assert.Equal((int)TlsAlertMessage.InternalError, Interop.SChannel.TLS1_ALERT_INTERNAL_ERROR); 
            Assert.Equal((int)TlsAlertMessage.UserCanceled, Interop.SChannel.TLS1_ALERT_USER_CANCELED); 
            Assert.Equal((int)TlsAlertMessage.NoRenegotiation, Interop.SChannel.TLS1_ALERT_NO_RENEGOTIATION); 
            Assert.Equal((int)TlsAlertMessage.UnsupportedExt, Interop.SChannel.TLS1_ALERT_UNSUPPORTED_EXT); 
        }
    }
}
