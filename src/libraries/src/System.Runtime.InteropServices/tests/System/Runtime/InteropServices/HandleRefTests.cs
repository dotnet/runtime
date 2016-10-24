// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class HandleRefTests
    {
        [Fact]
        public void Properties()
        {
            var obj = new object();
            var ptr = new IntPtr(1337);
            var handleRef = new HandleRef(obj, ptr);
            Assert.Equal(ptr, (IntPtr)handleRef);
            Assert.Equal(ptr, HandleRef.ToIntPtr(handleRef));
            Assert.Equal(ptr, handleRef.Handle);
            Assert.Same(obj, handleRef.Wrapper);
        }
    }
}
