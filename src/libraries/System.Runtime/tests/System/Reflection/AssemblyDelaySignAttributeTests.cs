// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyDelaySignAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool delaySign)
        {
            var attribute = new AssemblyDelaySignAttribute(delaySign);
            Assert.Equal(delaySign, attribute.DelaySign);
        }
    }
}
