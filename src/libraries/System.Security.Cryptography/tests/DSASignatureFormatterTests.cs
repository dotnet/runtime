// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public partial class DSASignatureFormatterTests : AsymmetricSignatureFormatterTests
    {
        [Fact]
        public static void InvalidFormatterArguments_DSA()
        {
            InvalidFormatterArguments(new DSASignatureFormatter());
        }

        [Fact]
        public static void InvalidDeformatterArguments_DSA()
        {
            InvalidDeformatterArguments(new DSASignatureDeformatter());
        }
    }
}
