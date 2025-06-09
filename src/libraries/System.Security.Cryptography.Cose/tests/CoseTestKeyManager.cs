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
        public const string RSAPkcs1Identifier = "RSA-PKCS1";
        public const string RSAPssIdentifier = "RSA-PSS";
        public const string ECDsaIdentifier = "ECDsa";
        public const string MLDSA44Identifier = "ML-DSA-44";
        public const string MLDSA65Identifier = "ML-DSA-65";
        public const string MLDSA87Identifier = "ML-DSA-87";

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

        private static void WithTestKeysInfo(Action<string, IDisposable, CoseKey> action)
        {
            Let(ECDsa.Create(), (key) => action(ECDsaIdentifier, key, CoseKey.FromKey(key, HashAlgorithmName.SHA256)));
            Let(RSA.Create(), (key) => action(RSAPkcs1Identifier, key, CoseKey.FromKey(key, RSASignaturePadding.Pkcs1, HashAlgorithmName.SHA256)));

            if (PlatformSupport.IsRsaPssSupported)
            {
                Let(RSA.Create(), (key) => action(RSAPssIdentifier, key, CoseKey.FromKey(key, RSASignaturePadding.Pss, HashAlgorithmName.SHA256)));
            }

            if (MLDsa.IsSupported)
            {
                Let(MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa44), (key) => action(MLDSA44Identifier, key, CoseKey.FromKey(key)));
                Let(MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa65), (key) => action(MLDSA65Identifier, key, CoseKey.FromKey(key)));
                Let(MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa87), (key) => action(MLDSA87Identifier, key, CoseKey.FromKey(key)));
            }

            static void Let<KeyType>(KeyType key, Action<KeyType> action) => action(key);
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

        private void AddKey(string keyId, IDisposable key, CoseKey coseKey)
        {
            if (_coseKeys.ContainsKey(keyId))
            {
                throw new ArgumentException($"Key with ID {keyId} already exists.");
            }

            _coseKeys.Add(keyId, new CoseTestKey(keyId, key, coseKey));
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
