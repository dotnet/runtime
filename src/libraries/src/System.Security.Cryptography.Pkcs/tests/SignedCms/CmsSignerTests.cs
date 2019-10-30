// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static partial class CmsSignerTests
    {
        [Theory]
        [InlineData((SubjectIdentifierType)0)]
        [InlineData((SubjectIdentifierType)4)]
        [InlineData((SubjectIdentifierType)(-1))]
        public static void SignerIdentifierType_InvalidValues(SubjectIdentifierType invalidType)
        {
            CmsSigner signer = new CmsSigner();

            AssertExtensions.Throws<ArgumentException>(
                expectedParamName: null,
                () => signer.SignerIdentifierType = invalidType);
        }
    }
}
