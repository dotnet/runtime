// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class RSASignatureFormatterTests_NoKey
    {
        [Fact]
        public static void InvalidFormatterArguments_RSA()
        {
            AsymmetricSignatureFormatterTests.InvalidFormatterArguments(new RSAPKCS1SignatureFormatter());
        }

        [Fact]
        public static void InvalidDeformatterArguments_RSA()
        {
            AsymmetricSignatureFormatterTests.InvalidDeformatterArguments(new RSAPKCS1SignatureDeformatter());
        }
    }
}
