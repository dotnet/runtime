// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

#pragma warning disable SYSLIB5006

namespace System.Security.Cryptography.Cose.Tests
{
    public sealed class CoseTestKey : IDisposable
    {
        public string Id { get; }
        public CoseTestKeyType Type { get; }
        public HashAlgorithmName? HashAlgorithm { get; }

        public IDisposable Key { get; }
        public AsymmetricAlgorithm KeyAsymmetricAlgorithm { get; }
        public CoseSigner Signer { get; }

        private CoseTestKey(string keyId, CoseTestKeyType keyType, HashAlgorithmName? hashAlgorithm, IDisposable key, AsymmetricAlgorithm keyAsymmetricAlgorithm, CoseSigner signer)
        {
            Id = keyId;
            Type = keyType;
            HashAlgorithm = hashAlgorithm;

            Key = key;
            KeyAsymmetricAlgorithm = keyAsymmetricAlgorithm;
            Signer = signer;
        }
        public void Dispose()
        {
            Key.Dispose();
        }

        public static CoseTestKey GenerateKey(string keyId, CoseTestKeyType keyType, HashAlgorithmName? hashAlgorithm)
        {
            (IDisposable key, AsymmetricAlgorithm keyAsymmetricAlgorithm, CoseSigner signer) = GenerateKeyCreateSignerAndAlgorithm(keyId, keyType, hashAlgorithm);
            return new CoseTestKey(keyId, keyType, hashAlgorithm, key, keyAsymmetricAlgorithm, signer);
        }

        public override string ToString() => $"KeyId={Id}";

        private static AsymmetricAlgorithm GenerateAsymmetricAlgorithm(CoseTestKeyType keyType)
        {
            return keyType switch
            {
                CoseTestKeyType.RSAPkcs1 => RSA.Create(),
                CoseTestKeyType.RSAPSS => RSA.Create(),
                CoseTestKeyType.ECDsa => ECDsa.Create(),
                _ => throw new InvalidOperationException("Unknown key type"),
            };
        }

        private static (IDisposable, AsymmetricAlgorithm, CoseSigner) GenerateKeyCreateSignerAndAlgorithm(string keyId, CoseTestKeyType keyType, HashAlgorithmName? hashAlgorithm)
        {
            if (keyType == CoseTestKeyType.MLDsa44 || keyType == CoseTestKeyType.MLDsa65 || keyType == CoseTestKeyType.MLDsa87)
            {
                MLDsaAlgorithm algorithm = keyType switch
                {
                    CoseTestKeyType.MLDsa44 => MLDsaAlgorithm.MLDsa44,
                    CoseTestKeyType.MLDsa65 => MLDsaAlgorithm.MLDsa65,
                    CoseTestKeyType.MLDsa87 => MLDsaAlgorithm.MLDsa87,
                    _ => throw new NotImplementedException("Unknown key type"),
                };
                MLDsa mldsaKey = MLDsa.GenerateKey(algorithm);
                CoseSigner mldsaSigner = new(mldsaKey, protectedHeaders: new CoseHeaderMap { [CoseHeaderLabel.KeyIdentifier] = CoseHeaderValue.FromBytes(Encoding.UTF8.GetBytes(keyId)) });
                return (mldsaKey, mldsaSigner.Key, mldsaSigner);
            }

            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException(nameof(hashAlgorithm), "Hash algorithm must be specified for non-ML-DSA keys");
            }

            AsymmetricAlgorithm key = GenerateAsymmetricAlgorithm(keyType);
            CoseHeaderMap headerMap = new CoseHeaderMap { [CoseHeaderLabel.KeyIdentifier] = CoseHeaderValue.FromBytes(Encoding.UTF8.GetBytes(keyId)) };

            CoseSigner signer = keyType switch
            {
                CoseTestKeyType.RSAPkcs1 => new CoseSigner(
                    (RSA)key,
                    RSASignaturePadding.Pkcs1,
                    hashAlgorithm.Value,
                    protectedHeaders: headerMap),
                CoseTestKeyType.RSAPSS => new CoseSigner(
                    (RSA)key,
                    RSASignaturePadding.Pss,
                    hashAlgorithm.Value,
                    protectedHeaders: headerMap),
                CoseTestKeyType.ECDsa => new CoseSigner(
                    key,
                    hashAlgorithm.Value,
                    protectedHeaders: headerMap),
                _ => throw new NotImplementedException("Unknown key type"),
            };

            return (key, key, signer);
        }
    }
}
