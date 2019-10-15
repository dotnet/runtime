// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.WindowsRuntime.Tests
{
    public class DefaultInterfaceAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void Ctor_DefaultInterface(Type defaultInterface)
        {
            var attribute = new ComDefaultInterfaceAttribute(defaultInterface);
            Assert.Equal(defaultInterface, attribute.Value);
        }
    }
}
