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
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Ctor_NullWindows_Success()
        {
            var wrapper = new DispatchWrapper(null);
            Assert.Null(wrapper.WrappedObject);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Ctor_NullUapAot_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new DispatchWrapper(null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Ctor_NullUnix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new DispatchWrapper(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(0)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Marshal.GetIDispatchForObject is not supported in .NET Core.")]
        public void Ctor_NonNull_ThrowsPlatformNotSupportedException(object value)
        {
            Assert.Throws<PlatformNotSupportedException>(() => new DispatchWrapper(value));
        }
    }
#pragma warning restore 0618
}
