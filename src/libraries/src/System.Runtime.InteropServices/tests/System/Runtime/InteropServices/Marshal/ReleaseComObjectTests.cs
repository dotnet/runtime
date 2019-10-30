// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class ReleaseComObjectTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void ReleaseComObject_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.FinalReleaseComObject(null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ReleaseComObject_NullObject_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => Marshal.ReleaseComObject(null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ReleaseComObject_NonComObject_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("o", () => Marshal.ReleaseComObject(10));
        }
    }
}
