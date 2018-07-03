// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public class DSACngPkcs8Tests : CngPkcs8Tests<DSACng>
    {
        protected override DSACng CreateKey(out CngKey cngKey)
        {
            DSACng key = new DSACng();
            cngKey = key.Key;
            return key;
        }

        protected override void VerifyMatch(DSACng exported, DSACng imported)
        {
            byte[] data = { 8, 4, 1, 2, 11 };
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;

            byte[] signature = imported.SignData(data, hashAlgorithm);
            Assert.True(exported.VerifyData(data, signature, hashAlgorithm));
        }
    }
}
