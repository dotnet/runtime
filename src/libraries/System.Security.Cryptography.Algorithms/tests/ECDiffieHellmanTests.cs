// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.Tests;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanTests
    {
        [Fact]
        public static void ECCurve_ctor()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            {
                Assert.Equal(256, ecdh.KeySize);
                ecdh.Exercise();
            }

            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP384))
            {
                Assert.Equal(384, ecdh.KeySize);
                ecdh.Exercise();
            }

            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP521))
            {
                Assert.Equal(521, ecdh.KeySize);
                ecdh.Exercise();
            }
        }

        [Fact]
        public static void Equivalence_Hash()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                byte[] newWay = ecdh.DeriveKeyFromHash(publicKey, HashAlgorithmName.SHA256, null, null);
                byte[] oldWay = ecdh.DeriveKeyMaterial(publicKey);
                Assert.Equal(newWay, oldWay);
            }
        }
    }
}