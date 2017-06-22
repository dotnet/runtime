// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class RequiredAttributeAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void Ctor_RequiredContract(Type requiredContract)
        {
            var attribute = new RequiredAttributeAttribute(requiredContract);
            Assert.Equal(requiredContract, attribute.RequiredContract);
        }
    }
}
