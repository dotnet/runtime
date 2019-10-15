// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public class ECDHCngPkcs8Tests : CngPkcs8Tests<ECDiffieHellmanCng>
    {
        protected override ECDiffieHellmanCng CreateKey(out CngKey cngKey)
        {
            ECDiffieHellmanCng key = new ECDiffieHellmanCng(ECCurve.NamedCurves.nistP384);
            cngKey = key.Key;
            return key;
        }

        protected override void VerifyMatch(ECDiffieHellmanCng exported, ECDiffieHellmanCng imported)
        {
            using (ECDiffieHellmanCng other = new ECDiffieHellmanCng(exported.ExportParameters(false).Curve))
            using (ECDiffieHellmanPublicKey otherPub = other.PublicKey)
            {
                byte[] a = imported.DeriveKeyFromHash(otherPub, HashAlgorithmName.SHA256);
                byte[] b = exported.DeriveKeyFromHash(otherPub, HashAlgorithmName.SHA256);

                Assert.Equal(a.ByteArrayToHex(), b.ByteArrayToHex());
            }
        }
    }
}
