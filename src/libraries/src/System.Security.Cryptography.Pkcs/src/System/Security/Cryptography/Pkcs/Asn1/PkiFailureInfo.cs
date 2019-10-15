// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc4210#section-5.2.3
    [Flags]
    internal enum PkiFailureInfo
    {
        None = 0,
        BadAlg = 1 << 0,
        BadMessageCheck = 1 << 1,
        BadRequest = 1 << 2,
        BadTime = 1 << 3,
        BadCertId = 1 << 4,
        BadDataFormat = 1 << 5,
        WrongAuthority = 1 << 6,
        IncorrectData = 1 << 7,
        MissingTimeStamp = 1 << 8,
        BadPop = 1 << 9,
        CertRevoked = 1 << 10,
        CertConfirmed = 1 << 11,
        WrongIntegrity = 1 << 12,
        BadRecipientNonce = 1 << 13,
        TimeNotAvailable = 1 << 14,
        UnacceptedPolicy = 1 << 15,
        UnacceptedExtension = 1 << 16,
        AddInfoNotAvailable = 1 << 17,
        BadSenderNonce = 1 << 18,
        BadCertTemplate = 1 << 19,
        SignerNotTrusted = 1 << 20,
        TransactionIdInUse = 1 << 21,
        UnsupportedVersion = 1 << 22,
        NotAuthorized = 1 << 23,
        SystemUnavail = 1 << 24,
        SystemFailure = 1 << 25,
        DuplicateCertReq = 1 << 26,
    }
}
