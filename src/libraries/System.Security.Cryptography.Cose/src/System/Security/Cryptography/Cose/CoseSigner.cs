// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Cose
{
    public sealed class CoseSigner
    {
        public AsymmetricAlgorithm Key { get; }
        public HashAlgorithmName HashAlgorithm { get; }
        internal CoseHeaderMap? _protectedHeaders;
        public CoseHeaderMap ProtectedHeaders => _protectedHeaders ??= new CoseHeaderMap();
        internal CoseHeaderMap? _unprotectedHeaders;
        public CoseHeaderMap UnprotectedHeaders => _unprotectedHeaders ??= new CoseHeaderMap();
        public RSASignaturePadding? RSASignaturePadding { get; }
        internal readonly KeyType _keyType;

        public CoseSigner(AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (key is RSA)
                throw new CryptographicException(SR.CoseSignerRSAKeyNeedsPadding);

            Key = key;
            HashAlgorithm = hashAlgorithm;
            _protectedHeaders = protectedHeaders;
            _unprotectedHeaders = unprotectedHeaders;
            _keyType = CoseHelpers.GetKeyType(key);
        }

        public CoseSigner(RSA key, RSASignaturePadding signaturePadding, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (signaturePadding is null)
                throw new ArgumentNullException(nameof(signaturePadding));

            Key = key;
            HashAlgorithm = hashAlgorithm;
            _protectedHeaders = protectedHeaders;
            _unprotectedHeaders = unprotectedHeaders;
            _keyType = CoseHelpers.GetKeyType(key);
            RSASignaturePadding = signaturePadding;
        }
    }
}
