// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class BindToMonikerTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void BindToMoniker_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.BindToMoniker(null));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNative), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(null)]
        [InlineData("")]
        public void BindToMoniker_NullOrEmptyMonikerName_ThrowsAnyException(string monikerName)
        {
            Assert.ThrowsAny<Exception>(() => Marshal.BindToMoniker(monikerName));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNative))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("name")]
        public void BindToMoniker_InvalidMonikerName_ThrowsCOMException(string monikerName)
        {
            Assert.Throws<COMException>(() => Marshal.BindToMoniker(monikerName));
        }
    }
}
