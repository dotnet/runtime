// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class AttributeUsageAttributeTests
    {
        [Fact]
        public static void Ctor()
        {
            var attribute = new AttributeUsageAttribute(AttributeTargets.Delegate | AttributeTargets.GenericParameter);
            Assert.Equal(AttributeTargets.Delegate | AttributeTargets.GenericParameter, attribute.ValidOn);

            Assert.False(attribute.AllowMultiple);
            attribute.AllowMultiple = true;
            Assert.True(attribute.AllowMultiple);
            attribute.AllowMultiple = false;
            Assert.False(attribute.AllowMultiple);

            Assert.True(attribute.Inherited);
            attribute.Inherited = false;
            Assert.False(attribute.Inherited);
            attribute.Inherited = true;
            Assert.True(attribute.Inherited);
        }
    }
}
