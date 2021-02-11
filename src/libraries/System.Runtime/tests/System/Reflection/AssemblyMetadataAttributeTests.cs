// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyMetadataAttributeTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("key", "value")]
        public void Ctor_String_String(string key, string value)
        {
            var attribute = new AssemblyMetadataAttribute(key, value);
            Assert.Equal(key, attribute.Key);
            Assert.Equal(value, attribute.Value);
        }
    }
}
