// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Test.Cryptography;
using Xunit;

#pragma warning disable SYSLIB5006

namespace System.Security.Cryptography.Cose.Tests
{
    public sealed class CoseTestKeyManager : IDisposable
    {
        private Dictionary<string, CoseTestKey> _coseKeys = new();

        // Ideally public keys should be used with verify, i.e.:
        // - CoseTestKeyManager.ctor() => private keys
        // - CoseTestKeyManager CreateTestPublicKeys(CoseTestKeyManager privateKeys)
        // this is ok for testing purposes.
        private CoseTestKeyManager() { }

        public static CoseTestKeyManager CreateTestKeys()
        {
            CoseTestKeyManager keyManager = new();

            WithTestKeysInfo((keyId, keyType, hashAlgorithm) =>
            {
                keyManager.AddKey(keyId, keyType, hashAlgorithm);
            });

            return keyManager;
        }

        public static string[] GetAllKeyIds()
        {
            List<string> keyIds = new();
            WithTestKeysInfo((keyId, _, _) =>
            {
                keyIds.Add(keyId);
            });

            return keyIds.ToArray();
        }

        private static void WithTestKeysInfo(Action<string, CoseTestKeyType, HashAlgorithmName?> action)
        {
            action("ECDsa", CoseTestKeyType.ECDsa, HashAlgorithmName.SHA256);

            action("RSA-PKCS1", CoseTestKeyType.RSAPkcs1, HashAlgorithmName.SHA256);

            if (PlatformSupport.IsRsaPssSupported)
            {
                action("RSA-PSS", CoseTestKeyType.RSAPSS, HashAlgorithmName.SHA256);
            }
            else
            {
                // we use 3 suffix because if PSS is not supported then ML-DSA will not be as well
                action("RSA-PKCS1-3", CoseTestKeyType.RSAPkcs1, HashAlgorithmName.SHA256);
            }

            if (MLDsa.IsSupported)
            {
                action("ML-DSA-44", CoseTestKeyType.MLDsa44, null);
                action("ML-DSA-65", CoseTestKeyType.MLDsa65, null);
                action("ML-DSA-87", CoseTestKeyType.MLDsa87, null);
            }
            else
            {
                // we currently need 5 keys for the tests
                action("ECDsa-2", CoseTestKeyType.ECDsa, HashAlgorithmName.SHA256);
                action("RSA-PKCS1-2", CoseTestKeyType.RSAPkcs1, HashAlgorithmName.SHA256);
            }
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

            _coseKeys.Add(keyId, CoseTestKey.GenerateKey(keyId, keyType, hashAlgorithm));
        }

        public override string ToString() => nameof(CoseTestKeyManager);

        public class TestFixture : IDisposable
        {
            // Those are the keys we use for the tests
            public CoseTestKeyManager KeyManager { get; }

            // Those are the keys we use for testing bad keys - they mimick the structure but are different
            public CoseTestKeyManager BadKeyManager { get; }

            public TestFixture()
            {
                KeyManager = CreateTestKeys();
                BadKeyManager = CreateTestKeys();
            }

            public void Dispose()
            {
                KeyManager.Dispose();
                BadKeyManager.Dispose();
            }
        }
    }
}
