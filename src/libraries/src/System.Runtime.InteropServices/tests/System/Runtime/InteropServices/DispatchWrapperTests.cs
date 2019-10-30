// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
#pragma warning disable 0618 // DispatchWrapper is marked as Obsolete.
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
        public void Ctor_NonNull_ThrowsPlatformNotSupportedException(object value)
        {
            Assert.Throws<PlatformNotSupportedException>(() => new DispatchWrapper(value));
        }
    }
#pragma warning restore 0618
}
