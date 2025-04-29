// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

#pragma warning disable SYSLIB5006

namespace System.Security.Cryptography.Cose.Tests
{
    public sealed class CoseTestKeyManager : IDisposable
    {
        private Dictionary<string, CoseTestKey> _coseKeys = new();

        public static CoseTestKeyManager TestKeys { get; } = CreateTestKeys();

        private CoseTestKeyManager()
        {
        }

        // Ideally public keys should be used with verify, i.e.:
        // - CoseTestKeyManager CreateTestPrivateKeys()
        // - CoseTestKeyManager CreateTestPublicKeys(CoseTestKeyManager privateKeys)
        // this is ok for testing purposes.
        private static CoseTestKeyManager CreateTestKeys()
        {
            var ret = new CoseTestKeyManager();

            ret.AddKey("ECDsa", CoseTestKeyType.ECDsa, HashAlgorithmName.SHA256);
            ret.AddKey("RSA-PSS", CoseTestKeyType.RSAPSS, HashAlgorithmName.SHA256);
            ret.AddKey("RSA-PKCS1", CoseTestKeyType.RSAPkcs1, HashAlgorithmName.SHA256);

            if (MLDsa.IsSupported)
            {
                ret.AddKey("ML-DSA-44", CoseTestKeyType.MLDsa44);
                ret.AddKey("ML-DSA-65", CoseTestKeyType.MLDsa65);
                ret.AddKey("ML-DSA-87", CoseTestKeyType.MLDsa87);
            }
            else
            {
                // we currently need 5 keys for the tests
                ret.AddKey("ECDsa-2", CoseTestKeyType.ECDsa, HashAlgorithmName.SHA256);
                ret.AddKey("RSA-PKCS1-2", CoseTestKeyType.RSAPkcs1, HashAlgorithmName.SHA256);
            }

            return ret;
        }

        public IEnumerable<CoseTestKey> AllKeys()
        {
            foreach (var key in _coseKeys.Values)
            {
                yield return key;
            }
        }

        public CoseTestKey GetKey(string keyId)
        {
            Assert.True(_coseKeys.TryGetValue(keyId, out var key), $"Key {keyId} not found");
            return key;
        }

        public CoseTestKey GetDifferentKey(string notWantedKeyId)
        {
            foreach (var key in _coseKeys.Values)
            {
                if (key.Id != notWantedKeyId)
                {
                    return key;
                }
            }

            Assert.Fail("Not enough keys in the manager");
            throw null;
        }

        public void Dispose()
        {
            if (_coseKeys != null)
            {
                foreach (var key in _coseKeys.Values)
                {
                    key.Dispose();
                }

                _coseKeys = null!;
            }
        }

        private void AddKey(string keyId, CoseTestKeyType keyType, HashAlgorithmName? hashAlgorithm = null)
        {
            if (_coseKeys.ContainsKey(keyId))
            {
                throw new ArgumentException($"Key with ID {keyId} already exists.");
            }

            _coseKeys[keyId] = CoseTestKey.GenerateKey(keyId, keyType, hashAlgorithm);
        }

        public override string ToString() => nameof(CoseTestKeyManager);
    }
}
