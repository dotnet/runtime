// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        NoCertificate = 41, // reserved - Used in SSLv3 but not in TLS
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
        TooManyCidsRequested = 52, // error
        ExportRestriction = 60, // reserved
        ProtocolVersion = 70, // error
        InsufficientSecurity = 71, // error
        InternalError = 80, // error
        InappropriateFallback = 86, // error
        UserCanceled = 90, // warning or error
        NoRenegotiation = 100, // warning
        MissingExtension = 109, // error
        UnsupportedExtension = 110, // error
        CertificateUnobtainable = 111, // reserved - Used in TLS versions prior to 1.3
        UnrecognizedName = 112, // error
        BadCertificateStatusResponse = 113, // error
        BadCertificateHashValue = 114, // reserved - Used in TLS versions prior to 1.3
        UnknownPskIdentity = 115, // error
        CertificateRequired = 116, // error
        GeneralError = 117, // error
        NoApplicationProtocol = 120, // error
        EchRequired = 121, // error
    }
}
