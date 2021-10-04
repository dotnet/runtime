// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class AddRefTests
    {
        [Fact]
        [SkipOnMono("ComWrappers are not supported on Mono")]
        public void AddRef_ValidPointer_Success()
        {
            var cw = new ComWrappersImpl();
            IntPtr iUnknown = cw.GetOrCreateComInterfaceForObject(new object(), CreateComInterfaceFlags.None);
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
