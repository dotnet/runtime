// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class HandleRefTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var handleRef = new HandleRef();
            Assert.Null(handleRef.Wrapper);
            Assert.Equal(IntPtr.Zero, handleRef.Handle);
            Assert.Equal(IntPtr.Zero, (IntPtr)handleRef);
            Assert.Equal(IntPtr.Zero, HandleRef.ToIntPtr(handleRef));
        }

        [Theory]
        [InlineData(null, 0)]
        [InlineData("Wrapper", 1337)]
        public void Ctor_Wrapper_Handle(object wrapper, int handle)
        {
            var handleRef = new HandleRef(wrapper, (IntPtr)handle);
            Assert.Same(wrapper, handleRef.Wrapper);
            Assert.Equal((IntPtr)handle, handleRef.Handle);
            Assert.Equal((IntPtr)handle, (IntPtr)handleRef);
            Assert.Equal((IntPtr)handle, HandleRef.ToIntPtr(handleRef));
        }
    }
}
