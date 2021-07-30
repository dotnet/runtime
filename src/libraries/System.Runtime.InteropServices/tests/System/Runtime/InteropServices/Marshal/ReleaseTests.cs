// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ReleaseTests
    {
        [Fact]
        [SkipOnMono("ComWrappers are not supported on Mono")]
        public void Release_ValidPointer_Success()
        {
            var cw = new ComWrappersImpl();
            IntPtr iUnknown = cw.GetOrCreateComInterfaceForObject(new object(), CreateComInterfaceFlags.None);
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
        public void Release_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pUnk", () => Marshal.Release(IntPtr.Zero));
        }
    }
}
