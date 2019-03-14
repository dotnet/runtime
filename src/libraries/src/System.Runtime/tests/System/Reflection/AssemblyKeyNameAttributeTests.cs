// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyKeyNameAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("keyName")]
        public void Ctor_String(string keyName)
        {
            var attribute = new AssemblyKeyNameAttribute(keyName);
            Assert.Equal(keyName, attribute.KeyName);
        }
    }
}
