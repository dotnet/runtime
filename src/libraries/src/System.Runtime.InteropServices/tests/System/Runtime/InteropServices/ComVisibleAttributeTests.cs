// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [ComVisible(true)]
    public class ComVisibleAttributeTests
    {
        [Fact]
        public void Exists()
        {
            Type type = typeof(ComVisibleAttributeTests);
            ComVisibleAttribute attribute = Assert.IsType<ComVisibleAttribute>(Assert.Single(type.GetCustomAttributes(typeof(ComVisibleAttribute), inherit: false)));
            Assert.True(attribute.Value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Visible(bool visibility)
        {
            var attribute = new ComVisibleAttribute(visibility);
            Assert.Equal(visibility, attribute.Value);
        }
    }
}
