// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class InterfaceTypeAttributeTests
    {
        [Theory]
        [InlineData((ComInterfaceType)(-1))]
        [InlineData(ComInterfaceType.InterfaceIsIInspectable)]
        [InlineData((ComInterfaceType)5)]
        public void Ctor_ComInterfaceType(ComInterfaceType interfaceType)
        {
            var attribute = new InterfaceTypeAttribute(interfaceType);
            Assert.Equal(interfaceType, attribute.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(5)]
        public void Ctor_ShortInterfaceType(short interfaceType)
        {
            var attribute = new InterfaceTypeAttribute(interfaceType);
            Assert.Equal((ComInterfaceType)interfaceType, attribute.Value);
        }
    }
}
