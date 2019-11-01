// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ObsoleteAttributeTests
    {
        [Fact]
        public static void Ctor_Default()
        {
            var attribute = new ObsoleteAttribute();
            Assert.Null(attribute.Message);
            Assert.False(attribute.IsError);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("message")]
        public void Ctor_String(string message)
        {
            var attribute = new ObsoleteAttribute(message);
            Assert.Equal(message, attribute.Message);
            Assert.False(attribute.IsError);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", false)]
        [InlineData("message", true)]
        public void Ctor_String_Bool(string message, bool error)
        {
            var attribute = new ObsoleteAttribute(message, error);
            Assert.Equal(message, attribute.Message);
            Assert.Equal(error, attribute.IsError);
        }
    }
}
