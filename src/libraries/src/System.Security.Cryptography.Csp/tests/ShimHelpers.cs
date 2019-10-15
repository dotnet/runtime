// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public static class ShimHelpers
    {
        public static void TestSymmetricAlgorithmProperties(SymmetricAlgorithm alg, int blockSize, int keySize, byte[] key = null)
        {
            alg.BlockSize = blockSize;
            Assert.Equal(blockSize, alg.BlockSize);

            var emptyIV = new byte[alg.BlockSize / 8];
            alg.IV = emptyIV;
            Assert.Equal(emptyIV, alg.IV);
            alg.GenerateIV();
            Assert.NotEqual(emptyIV, alg.IV);

            if (key == null)
            {
                key = new byte[alg.KeySize / 8];
            }
            alg.Key = key;
            Assert.Equal(key, alg.Key);
            Assert.NotSame(key, alg.Key);
            alg.GenerateKey();
            Assert.NotEqual(key, alg.Key);

            alg.KeySize = keySize;
            Assert.Equal(keySize, alg.KeySize);

            alg.Mode = CipherMode.ECB;
            Assert.Equal(CipherMode.ECB, alg.Mode);

            alg.Padding = PaddingMode.PKCS7;
            Assert.Equal(PaddingMode.PKCS7, alg.Padding);
        }

        // Shims should override all virtual members and forward to their _impl.
        public static void VerifyAllBaseMembersOverloaded(Type shimType)
        {
            string[] namesToNotVerify =
            {
                "Dispose",
                // DecryptValue and EncryptValue throw PNSE in base class, so they don't need to be checked.
                "DecryptValue",
                "EncryptValue",
                // Key Import/Export defers to ImportParameters/ExportParameters (covered by *KeyFileTests)
                "ImportRSAPrivateKey",
                "ImportRSAPublicKey",
                "ImportSubjectPublicKeyInfo",
                "ImportPkcs8PrivateKey",
                "ImportEncryptedPkcs8PrivateKey",
                "ExportRSAPrivateKey",
                "ExportRSAPublicKey",
                "ExportSubjectPublicKeyInfo",
                "ExportPkcs8PrivateKey",
                "ExportEncryptedPkcs8PrivateKey",
                "ExportRSAPrivateKey",
                "TryExportRSAPrivateKey",
                "TryExportRSAPublicKey",
                "TryExportSubjectPublicKeyInfo",
                "TryExportPkcs8PrivateKey",
                "TryExportEncryptedPkcs8PrivateKey",
            };

            IEnumerable<MethodInfo> baseMethods = shimType.
                GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).
                Where(m => m.IsVirtual && !namesToNotVerify.Any(n=> n.Equals(m.Name)));

            foreach (MethodInfo info in baseMethods)
            {
                // Ensure the override is on the shim; ignore virtual methods on System.Object
                bool methodOverriden = info.DeclaringType == shimType || info.DeclaringType == typeof(object);

                Assert.True(methodOverriden, $"Member overriden: {info.Name}");
            }
        }
    }
}
