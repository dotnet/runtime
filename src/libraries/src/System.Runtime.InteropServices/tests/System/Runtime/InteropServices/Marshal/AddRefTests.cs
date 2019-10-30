// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class AddRefTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void AddRef_ValidPointer_Success()
        {
            IntPtr iUnknown = Marshal.GetIUnknownForObject(new object());
            try
            {
                Assert.Equal(2, Marshal.AddRef(iUnknown));
                Assert.Equal(3, Marshal.AddRef(iUnknown));

                Marshal.Release(iUnknown);
                Marshal.Release(iUnknown);
                Assert.Equal(2, Marshal.AddRef(iUnknown));
                Marshal.Release(iUnknown);
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void AddRef_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.AddRef(IntPtr.Zero));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void AddRef_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pUnk", () => Marshal.AddRef(IntPtr.Zero));
        }
    }
}
