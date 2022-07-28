// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.InteropServices;

namespace System.Tests
{
    public class PosixSignalContextTests
    {
        [Theory]
        [InlineData(PosixSignal.SIGINT)]
        [InlineData((PosixSignal)0)]
        [InlineData((PosixSignal)1000)]
        [InlineData((PosixSignal)(-1000))]
        public void Constructor(PosixSignal value)
        {
            var ctx = new PosixSignalContext(value);
            Assert.Equal(value, ctx.Signal);
            Assert.False(ctx.Cancel);
        }

        [Fact]
        public void Cancel_Roundtrips()
        {
            var ctx = new PosixSignalContext(PosixSignal.SIGINT);
            Assert.Equal(PosixSignal.SIGINT, ctx.Signal);
            Assert.False(ctx.Cancel);

            for (int i = 0; i < 2; i++)
            {
                ctx.Cancel = true;
                Assert.True(ctx.Cancel);

                ctx.Cancel = false;
                Assert.False(ctx.Cancel);
            }
        }
    }
}
