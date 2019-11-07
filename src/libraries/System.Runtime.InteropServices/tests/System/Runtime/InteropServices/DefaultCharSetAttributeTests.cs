// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class DefaultCharSetAttributeTests
    {
        [Theory]
        [InlineData((CharSet)(-1))]
        [InlineData(CharSet.Unicode)]
        [InlineData((CharSet)5)]
        public void Ctor_CharSet(CharSet charSet)
        {
            var attribute = new DefaultCharSetAttribute(charSet);
            Assert.Equal(charSet, attribute.CharSet);
        }
    }
}
