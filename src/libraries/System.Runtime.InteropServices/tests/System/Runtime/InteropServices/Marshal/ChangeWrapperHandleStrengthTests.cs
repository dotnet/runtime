// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ChangeWrapperHandleStrengthTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNative))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ChangeWrapperHandleStrength_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("otp", () => Marshal.ChangeWrapperHandleStrength(null, fIsWeak: true));
            AssertExtensions.Throws<ArgumentNullException>("otp", () => Marshal.ChangeWrapperHandleStrength(null, fIsWeak: false));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void ChangeWrapperHandleStrength_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.ChangeWrapperHandleStrength(null, fIsWeak: true));
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.ChangeWrapperHandleStrength(null, fIsWeak: false));
        }
    }
}
