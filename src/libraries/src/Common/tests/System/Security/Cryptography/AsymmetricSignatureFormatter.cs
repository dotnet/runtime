// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    /// <summary>
    /// Helper methods for DSASignatureFormatterTests and RSASignatureFormatterTests
    /// </summary>
    public partial class AsymmetricSignatureFormatterTests
    {
        private static readonly byte[] HelloBytes = new ASCIIEncoding().GetBytes("Hello");

        protected static void VerifySignature(AsymmetricSignatureFormatter formatter, AsymmetricSignatureDeformatter deformatter, HashAlgorithm hashAlgorithm, string hashAlgorithmName)
        {
            formatter.SetHashAlgorithm(hashAlgorithmName);
            deformatter.SetHashAlgorithm(hashAlgorithmName);

            byte[] hash = hashAlgorithm.ComputeHash(HelloBytes);

            VerifySignatureWithHashBytes(formatter, deformatter, hash);
            VerifySignatureWithHashAlgorithm(formatter, deformatter, hashAlgorithm);
        }

        private static void VerifySignatureWithHashBytes(AsymmetricSignatureFormatter formatter, AsymmetricSignatureDeformatter deformatter, byte[] hash)
        {
            byte[] signature = formatter.CreateSignature(hash);
            Assert.True(deformatter.VerifySignature(hash, signature));

            signature[signature.Length - 1] ^= 0xff;
            Assert.False(deformatter.VerifySignature(hash, signature));
        }

        private static void VerifySignatureWithHashAlgorithm(AsymmetricSignatureFormatter formatter, AsymmetricSignatureDeformatter deformatter, HashAlgorithm hashAlgorithm)
        {
            byte[] signature = formatter.CreateSignature(hashAlgorithm);
            Assert.True(deformatter.VerifySignature(hashAlgorithm, signature));

            signature[signature.Length - 1] ^= 0xff;
            Assert.False(deformatter.VerifySignature(hashAlgorithm, signature));
        }
    }
}
