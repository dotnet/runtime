// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ReleaseTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Release_ValidPointer_Success()
        {
            IntPtr iUnknown = Marshal.GetIUnknownForObject(new object());
            try
            {
                Marshal.AddRef(iUnknown);
                Marshal.AddRef(iUnknown);

                Assert.Equal(2, Marshal.Release(iUnknown));
                Assert.Equal(1, Marshal.Release(iUnknown));

                Marshal.AddRef(iUnknown);
                Assert.Equal(1, Marshal.Release(iUnknown));
            }
            finally
            {
                Assert.Equal(0, Marshal.Release(iUnknown));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Release_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.Release(IntPtr.Zero));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Release_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pUnk", () => Marshal.Release(IntPtr.Zero));
        }
    }
}
