// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public partial class RSASignatureFormatterTests : AsymmetricSignatureFormatterTests
    {
        [Fact]
        public static void InvalidFormatterArguments()
        {
            InvalidFormatterArguments(new RSAPKCS1SignatureFormatter());
        }

        [Fact]
        public static void InvalidDeformatterArguments()
        {
            InvalidDeformatterArguments(new RSAPKCS1SignatureDeformatter());
        }
    }
}
