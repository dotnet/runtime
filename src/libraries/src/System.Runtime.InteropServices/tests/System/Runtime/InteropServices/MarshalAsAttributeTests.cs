// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class MarshalAsAttributeTests
    {
        [Theory]
        [InlineData((UnmanagedType)(-1))]
        [InlineData(UnmanagedType.HString)]
        [InlineData((UnmanagedType)int.MaxValue)]
        public void Ctor_UmanagedTye(UnmanagedType unmanagedType)
        {
            var attribute = new MarshalAsAttribute(unmanagedType);
            Assert.Equal(unmanagedType, attribute.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(47)]
        [InlineData(short.MaxValue)]
        public void Ctor_ShortUnmanagedType(short umanagedType)
        {
            var attribute = new MarshalAsAttribute(umanagedType);
            Assert.Equal((UnmanagedType)umanagedType, attribute.Value);
        }
    }
}
