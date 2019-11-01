// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class UnmanagedFunctionPointerAttributeTests
    {
        [Theory]
        [InlineData((CallingConvention)(-1))]
        [InlineData(CallingConvention.Cdecl)]
        [InlineData((CallingConvention)int.MaxValue)]
        public void Ctor_CallingConvention(CallingConvention callingConvention)
        {
            var attribute = new UnmanagedFunctionPointerAttribute(callingConvention);
            Assert.Equal(callingConvention, attribute.CallingConvention);
        }
    }
}
