// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public class RSACngPkcs8Tests : CngPkcs8Tests<RSACng>
    {
        protected override RSACng CreateKey(out CngKey cngKey)
        {
            RSACng rsa = new RSACng();
            cngKey = rsa.Key;
            return rsa;
        }

        protected override void VerifyMatch(RSACng exported, RSACng imported)
        {
            byte[] data = { 8, 4, 1, 2, 11 };
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
            RSASignaturePadding padding = RSASignaturePadding.Pss;

            byte[] signature = imported.SignData(data, hashAlgorithm, padding);
            Assert.True(exported.VerifyData(data, signature, hashAlgorithm, padding));
        }
    }
}
