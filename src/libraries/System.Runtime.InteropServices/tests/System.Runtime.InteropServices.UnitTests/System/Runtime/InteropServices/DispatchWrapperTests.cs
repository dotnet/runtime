// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class DispatchWrapperTests
    {
        [Fact]
        public void Ctor_Null_Success()
        {
            var wrapper = new DispatchWrapper(null);
            Assert.Null(wrapper.WrappedObject);
        }

        [Theory]
        [InlineData("")]
        [InlineData(0)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Ctor_NonNull_ThrowsPlatformNotSupportedException(object value)
        {
            Assert.Throws<PlatformNotSupportedException>(() => new DispatchWrapper(value));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [InlineData("")]
        [InlineData(0)]
        public void Ctor_NonDispatchObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => new DispatchWrapper(value));
        }
    }
}
