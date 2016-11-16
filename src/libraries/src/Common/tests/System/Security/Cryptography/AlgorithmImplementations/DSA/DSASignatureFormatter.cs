// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    public partial class DSASignatureFormatterTests : AsymmetricSignatureFormatterTests
    {
        [Fact]
        public static void VerifySignature_SHA1()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA1024Params());

                var formatter = new DSASignatureFormatter(dsa);
                var deformatter = new DSASignatureDeformatter(dsa);

                using (SHA1 alg = SHA1.Create())
                {
                    VerifySignature(formatter, deformatter, alg, "SHA1");
                    VerifySignature(formatter, deformatter, alg, "sha1");
                }
            }
        }

        [Fact]
        public static void InvalidHashAlgorithm()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                var formatter = new DSASignatureFormatter(dsa);
                var deformatter = new DSASignatureDeformatter(dsa);

                // Unlike RSA, DSA will throw during SetHashAlgorithm
                Assert.Throws<CryptographicUnexpectedOperationException>(() =>
                    formatter.SetHashAlgorithm("INVALIDVALUE"));
                Assert.Throws<CryptographicUnexpectedOperationException>(() =>
                    deformatter.SetHashAlgorithm("INVALIDVALUE"));

                // Currently anything other than SHA1 fails
                Assert.Throws<CryptographicUnexpectedOperationException>(() =>
                    formatter.SetHashAlgorithm("SHA256"));
                Assert.Throws<CryptographicUnexpectedOperationException>(() =>
                    deformatter.SetHashAlgorithm("SHA256"));
            }
        }

        [Fact]
        public static void VerifyKnownSignature()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                byte[] data;
                byte[] signature;
                DSAParameters dsaParameters;
                DSATestData.GetDSA1024_186_2(out dsaParameters, out signature, out data);

                byte[] hash;
                using (SHA1 alg = SHA1.Create())
                {
                    hash = alg.ComputeHash(data);
                }

                dsa.ImportParameters(dsaParameters);
                var deformatter = new DSASignatureDeformatter(dsa);
                deformatter.VerifySignature(hash, signature);

                // Negative case
                signature[signature.Length - 1] ^= 0xff;
                Assert.False(deformatter.VerifySignature(hash, signature));
            }
        }

        public static bool SupportsFips186_3
        {
            get
            {
                return DSAFactory.SupportsFips186_3;
            }
        }
    }
}
