// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public void AddRef_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pUnk", () => Marshal.AddRef(IntPtr.Zero));
        }
    }
}
