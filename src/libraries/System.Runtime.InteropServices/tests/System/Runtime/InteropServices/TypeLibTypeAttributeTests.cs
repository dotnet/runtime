// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class TypeLibTypeAttributeTests
    {
        [Theory]
        [InlineData((TypeLibTypeFlags)(-1))]
        [InlineData((TypeLibTypeFlags)0)]
        [InlineData(TypeLibTypeFlags.FLicensed)]
        public void Ctor_TypeLibTypeFlags(TypeLibTypeFlags flags)
        {
            var attribute = new TypeLibTypeAttribute(flags);
            Assert.Equal(flags, attribute.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(4)]
        public void Ctor_ShortFlags(short flags)
        {
            var attribute = new TypeLibTypeAttribute(flags);
            Assert.Equal((TypeLibTypeFlags)flags, attribute.Value);
        }
    }
}
