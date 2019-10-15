// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class CallingConventionTests
    {
        [Theory]
        [InlineData(CallingConvention.Winapi, 1)]
        [InlineData(CallingConvention.Cdecl, 2)]
        [InlineData(CallingConvention.StdCall, 3)]
        [InlineData(CallingConvention.ThisCall, 4)]
        [InlineData(CallingConvention.FastCall, 5)]
        public void CallingConvention_Get_ReturnsExpected(CallingConvention convention, int expected)
        {
            Assert.Equal(expected, (int)convention);
        }
    }
}
