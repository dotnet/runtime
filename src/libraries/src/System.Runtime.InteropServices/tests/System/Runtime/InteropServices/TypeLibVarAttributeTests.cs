// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class TypeLibVarAttributeTests
    {
        [Theory]
        [InlineData((TypeLibVarFlags)(-1))]
        [InlineData((TypeLibVarFlags)0)]
        [InlineData(TypeLibVarFlags.FBindable)]
        public void Ctor_TypeLibVarFlags(TypeLibVarFlags flags)
        {
            var attribute = new TypeLibVarAttribute(flags);
            Assert.Equal(flags, attribute.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(4)]
        public void Ctor_ShortFlags(short flags)
        {
            var attribute = new TypeLibVarAttribute(flags);
            Assert.Equal((TypeLibVarFlags)flags, attribute.Value);
        }
    }
}
