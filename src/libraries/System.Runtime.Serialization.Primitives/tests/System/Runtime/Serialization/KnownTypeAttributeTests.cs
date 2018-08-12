// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.Serialization.Tests
{
    public class KnownTypeAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        public void Ctor_String(string methodName)
        {
            var attribute = new KnownTypeAttribute(methodName);
            Assert.Equal(methodName, attribute.MethodName);
            Assert.Null(attribute.Type);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void Ctor_Type(Type type)
        {
            var attribute = new KnownTypeAttribute(type);
            Assert.Null(attribute.MethodName);
            Assert.Equal(type, attribute.Type);
        }
    }
}
