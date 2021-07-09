// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.InteropServices;

namespace System.Tests
{
    public class PosixSignalContextTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(-1000)]
        [InlineData(1000)]
        public void Constructor(int value)
        {
            var ctx = new PosixSignalContext((PosixSignal)value);
            Assert.Equal(value, (int)ctx.Signal);
        }
    }
}
