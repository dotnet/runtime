// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyKeyFileAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("keyFile")]
        [InlineData("KeyFile.snk")]
        public void Ctor_String(string keyFile)
        {
            var attribute = new AssemblyKeyFileAttribute(keyFile);
            Assert.Equal(keyFile, attribute.KeyFile);
        }
    }
}
