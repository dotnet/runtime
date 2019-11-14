// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Security
{
    internal enum TlsAlertMessage
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
}
