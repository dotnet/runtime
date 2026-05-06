// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [ComVisible(true)]
    public class ComVisibleAttributeTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void Exists()
        {
            Type type = typeof(ComVisibleAttributeTests);
            ComVisibleAttribute attribute = Assert.IsType<ComVisibleAttribute>(Assert.Single(type.GetCustomAttributes(typeof(ComVisibleAttribute), inherit: false)));
            Assert.True(attribute.Value);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Visible(bool visibility)
        {
            var attribute = new ComVisibleAttribute(visibility);
            Assert.Equal(visibility, attribute.Value);
        }
    }
}
