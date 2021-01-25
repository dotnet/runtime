// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblySignatureKeyAttributeTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("publicKey", "countersignature")]
        public void Ctor_String_String(string publicKey, string countersignature)
        {
            var attribute = new AssemblySignatureKeyAttribute(publicKey, countersignature);
            Assert.Equal(publicKey, attribute.PublicKey);
            Assert.Equal(countersignature, attribute.Countersignature);
        }
    }
}
