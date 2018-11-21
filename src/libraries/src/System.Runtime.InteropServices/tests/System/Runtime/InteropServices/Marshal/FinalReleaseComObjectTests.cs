// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class FinalReleaseComObjectTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void FinalReleaseComObject_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.FinalReleaseComObject(null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FinalReleaseComObject_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("o", () => Marshal.FinalReleaseComObject(null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FinalReleaseComObject_NonComObject_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("o", () => Marshal.FinalReleaseComObject(10));
        }
    }
}
