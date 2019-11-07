// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class TypeLibFuncAttributeTests
    {
        [Theory]
        [InlineData((TypeLibFuncFlags)(-1))]
        [InlineData((TypeLibFuncFlags)0)]
        [InlineData(TypeLibFuncFlags.FBindable)]
        public void Ctor_TypeLibFuncFlags(TypeLibFuncFlags flags)
        {
            var attribute = new TypeLibFuncAttribute(flags);
            Assert.Equal(flags, attribute.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(4)]
        public void Ctor_ShortFlags(short flags)
        {
            var attribute = new TypeLibFuncAttribute(flags);
            Assert.Equal((TypeLibFuncFlags)flags, attribute.Value);
        }
    }
}
