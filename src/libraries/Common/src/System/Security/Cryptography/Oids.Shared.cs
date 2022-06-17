// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class Oids
    {
        private static volatile Oid? _rsaOid;
        private static volatile Oid? _ecPublicKeyOid;
        private static volatile Oid? _tripleDesCbcOid;
        private static volatile Oid? _aes256CbcOid;
        private static volatile Oid? _secp256r1Oid;
        private static volatile Oid? _secp384r1Oid;
        private static volatile Oid? _secp521r1Oid;
        private static volatile Oid? _sha256Oid;
        private static volatile Oid? _pkcs7DataOid;
        private static volatile Oid? _contentTypeOid;
        private static volatile Oid? _documentDescriptionOid;
        private static volatile Oid? _documentNameOid;
        private static volatile Oid? _localKeyIdOid;
        private static volatile Oid? _messageDigestOid;
        private static volatile Oid? _signingTimeOid;
        private static volatile Oid? _pkcs9ExtensionRequestOid;
        private static volatile Oid? _basicConstraints2Oid;
        private static volatile Oid? _enhancedKeyUsageOid;
        private static volatile Oid? _keyUsageOid;
        private static volatile Oid? _subjectKeyIdentifierOid;
        private static volatile Oid? _authorityInformationAccessOid;

        internal static Oid RsaOid => _rsaOid ??= InitializeOid(Rsa);
        internal static Oid EcPublicKeyOid => _ecPublicKeyOid ??= InitializeOid(EcPublicKey);
        internal static Oid TripleDesCbcOid => _tripleDesCbcOid ??= InitializeOid(TripleDesCbc);
        internal static Oid Aes256CbcOid => _aes256CbcOid ??= InitializeOid(Aes256Cbc);
        internal static Oid secp256r1Oid => _secp256r1Oid ??= new Oid(secp256r1, nameof(ECCurve.NamedCurves.nistP256));
        internal static Oid secp384r1Oid => _secp384r1Oid ??= new Oid(secp384r1, nameof(ECCurve.NamedCurves.nistP384));
        internal static Oid secp521r1Oid => _secp521r1Oid ??= new Oid(secp521r1, nameof(ECCurve.NamedCurves.nistP521));
        internal static Oid Sha256Oid => _sha256Oid ??= InitializeOid(Sha256);

        internal static Oid Pkcs7DataOid => _pkcs7DataOid ??= InitializeOid(Pkcs7Data);
        internal static Oid ContentTypeOid => _contentTypeOid ??= InitializeOid(ContentType);
        internal static Oid DocumentDescriptionOid => _documentDescriptionOid ??= InitializeOid(DocumentDescription);
        internal static Oid DocumentNameOid => _documentNameOid ??= InitializeOid(DocumentName);
        internal static Oid LocalKeyIdOid => _localKeyIdOid ??= InitializeOid(LocalKeyId);
        internal static Oid MessageDigestOid => _messageDigestOid ??= InitializeOid(MessageDigest);
        internal static Oid SigningTimeOid => _signingTimeOid ??= InitializeOid(SigningTime);
        internal static Oid Pkcs9ExtensionRequestOid => _pkcs9ExtensionRequestOid ??= InitializeOid(Pkcs9ExtensionRequest);

        internal static Oid BasicConstraints2Oid => _basicConstraints2Oid ??= InitializeOid(BasicConstraints2);
        internal static Oid EnhancedKeyUsageOid => _enhancedKeyUsageOid ??= InitializeOid(EnhancedKeyUsage);
        internal static Oid KeyUsageOid => _keyUsageOid ??= InitializeOid(KeyUsage);
        internal static Oid SubjectKeyIdentifierOid => _subjectKeyIdentifierOid ??= InitializeOid(SubjectKeyIdentifier);
        internal static Oid AuthorityInformationAccessOid => _authorityInformationAccessOid ??= InitializeOid(AuthorityInformationAccess);

        private static Oid InitializeOid(string oidValue)
        {
            Debug.Assert(oidValue != null);
            Oid oid = new Oid(oidValue, null);

            // Do not remove - the FriendlyName property get has side effects.
            // On read, it initializes the friendly name based on the value and
            // locks it to prevent any further changes.
            _ = oid.FriendlyName;

            return oid;
        }

    }
}
