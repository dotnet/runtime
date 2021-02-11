// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class ObfuscateAssemblyAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool assemblyIsPrivate)
        {
            var attribute = new ObfuscateAssemblyAttribute(assemblyIsPrivate);
            Assert.Equal(assemblyIsPrivate, attribute.AssemblyIsPrivate);
            Assert.True(attribute.StripAfterObfuscation);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StripAfterObfuscation_Set_GetReturnsExpected(bool value)
        {
            var attribute = new ObfuscateAssemblyAttribute(true)
            {
                StripAfterObfuscation = value
            };
            Assert.Equal(value, attribute.StripAfterObfuscation);
        }
    }
}
