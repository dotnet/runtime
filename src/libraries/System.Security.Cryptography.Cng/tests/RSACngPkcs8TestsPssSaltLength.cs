// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public abstract class RSACngPkcs8TestsPssSaltLength : CngPkcs8Tests<RSACng>
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
            RSASignaturePadding padding = RSASignaturePadding.CreatePss(SaltLength);

            byte[] signature = imported.SignData(data, hashAlgorithm, padding);
            Assert.True(exported.VerifyData(data, signature, hashAlgorithm, padding));
        }

        protected abstract int SaltLength { get; }
    }
}
