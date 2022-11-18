// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [ProgId("pizza")]
    public class ProgIdAttributeTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void Exists()
        {
            Type type = typeof(ProgIdAttributeTests);
            ProgIdAttribute attribute = Assert.IsType<ProgIdAttribute>(Assert.Single(type.GetCustomAttributes(typeof(ProgIdAttribute), inherit: false)));
            Assert.Equal("pizza", attribute.Value);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [InlineData(null)]
        [InlineData("ProgId")]
        public void Ctor_ProgId(string progId)
        {
            var attribute = new ProgIdAttribute(progId);
            Assert.Equal(progId, attribute.Value);
        }
    }
}
