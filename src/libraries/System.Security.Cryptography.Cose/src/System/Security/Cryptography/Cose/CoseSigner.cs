// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Cose
{
    public sealed class CoseSigner
    {
        internal CoseHeaderMap? _protectedHeaders;
        internal CoseHeaderMap? _unprotectedHeaders;
        internal readonly KeyType _keyType;
        public AsymmetricAlgorithm Key { get; }
        public HashAlgorithmName HashAlgorithm { get; }
        public RSASignaturePadding? RSASignaturePadding { get; }
        public CoseHeaderMap ProtectedHeaders => _protectedHeaders ??= new CoseHeaderMap();
        public CoseHeaderMap UnprotectedHeaders => _unprotectedHeaders ??= new CoseHeaderMap();

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
