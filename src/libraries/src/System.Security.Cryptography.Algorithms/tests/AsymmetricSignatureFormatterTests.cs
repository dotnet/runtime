// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    /// <summary>
    /// Helper methods for DSASignatureFormatterTests and RSASignatureFormatterTests
    /// </summary>
    public partial class AsymmetricSignatureFormatterTests
    {
        protected static void InvalidFormatterArguments(AsymmetricSignatureFormatter formatter)
        {
            Assert.Throws<ArgumentNullException>(() => formatter.SetKey(null));
            Assert.Throws<ArgumentNullException>(() => formatter.CreateSignature((byte[])null));
            Assert.Throws<CryptographicUnexpectedOperationException>(() => formatter.CreateSignature(new byte[] { 0, 1, 2, 3 }));
        }

        protected static void InvalidDeformatterArguments(AsymmetricSignatureDeformatter deformatter)
        {
            Assert.Throws<ArgumentNullException>(() => deformatter.SetKey(null));
            Assert.Throws<ArgumentNullException>(() => deformatter.VerifySignature((byte[])null, new byte[] { 0, 1, 2 }));
            Assert.Throws<CryptographicUnexpectedOperationException>(() => deformatter.VerifySignature(new byte[] { 0, 1, 2 }, new byte[] { 0, 1, 2 }));
        }
    }
}
