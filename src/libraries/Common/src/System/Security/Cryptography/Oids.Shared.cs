// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class Oids
    {
        private static volatile Oid? _rsaOid = null;
        private static volatile Oid? _ecPublicKeyOid = null;
        private static volatile Oid? _tripleDesCbcOid = null;
        private static volatile Oid? _aes256CbcOid = null;
        private static volatile Oid? _secp256r1Oid = null;
        private static volatile Oid? _secp384r1Oid = null;
        private static volatile Oid? _secp521r1Oid = null;
        private static volatile Oid? _sha256Oid = null;
        private static volatile Oid? _pkcs7DataOid = null;
        private static volatile Oid? _contentTypeOid = null;
        private static volatile Oid? _documentDescriptionOid = null;

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

        private static Oid InitializeOid(string oidValue)
        {
            Debug.Assert(oidValue != null);
            Oid oid = new Oid(oidValue, null);

            // Do not remove - the FriendlyName property has side effects.
            // On read, it initializes the friendly name based on the value. On write,
            // it locks the friendly name so that it can not be set again, including
            // if it is null and being set to null.
            string? friendlyName = oid.FriendlyName;
            oid.FriendlyName = friendlyName;

            return oid;
        }

    }
}
