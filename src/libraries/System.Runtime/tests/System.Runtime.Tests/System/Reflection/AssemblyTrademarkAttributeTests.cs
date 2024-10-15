// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyTrademarkAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("trademark")]
        public void Ctor_String(string trademark)
        {
            var attribute = new AssemblyTrademarkAttribute(trademark);
            Assert.Equal(trademark, attribute.Trademark);
        }
    }
}
