// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Threading.Tasks;
using System.Text;

namespace System.IO.Tests
{
    public class BinaryWriter_DisposeAsync
    {
        [Fact]
        public void DisposeAsync_CanInvokeMultipleTimes()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            Assert.True(bw.DisposeAsync().IsCompletedSuccessfully);
            Assert.True(bw.DisposeAsync().IsCompletedSuccessfully);
        }

        [Fact]
        public void DisposeAsync_CanDisposeAsyncAfterDispose()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            bw.Dispose();
            Assert.True(bw.DisposeAsync().IsCompletedSuccessfully);
        }

        [Fact]
        public async Task DisposeAsync_FlushesStream()
        {
            var ms = new MemoryStream();
            var bs = new BufferedStream(ms);
            var bw = new BinaryWriter(bs);
            try
            {
                bw.Write(42);
                Assert.Equal(4, bs.Position);
                Assert.Equal(0, ms.Position);
            }
            finally
            {
                await bw.DisposeAsync();
            }
            Assert.Throws<ObjectDisposedException>(() => ms.Position);
            Assert.Equal(4, ms.ToArray().Length);
        }

        [Fact]
        public async Task DisposeAsync_LeaveOpenTrue_LeftOpen()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
            try
            {
                bw.Write(42);
            }
            finally
            {
                await bw.DisposeAsync();
            }
            Assert.Equal(4, ms.Position); // doesn't throw
        }

        [Fact]
        public async Task DisposeAsync_DerivedTypeForcesDisposeToBeUsedUnlessOverridden()
        {
            var ms = new MemoryStream();
            var bw = new OverrideDisposeBinaryWriter(ms);
            try
            {
                bw.Write(42);
            }
            finally
            {
                Assert.False(bw.DisposeInvoked);
                await bw.DisposeAsync();
                Assert.True(bw.DisposeInvoked);
            }
        }

        [Fact]
        public async Task DisposeAsync_DerivedTypeDisposeAsyncInvoked()
        {
            var ms = new MemoryStream();
            var bw = new OverrideDisposeAndDisposeAsyncBinaryWriter(ms);
            try
            {
                bw.Write(42);
            }
            finally
            {
                Assert.False(bw.DisposeInvoked);
                Assert.False(bw.DisposeAsyncInvoked);
                await bw.DisposeAsync();
                Assert.False(bw.DisposeInvoked);
                Assert.True(bw.DisposeAsyncInvoked);
            }
        }

        private sealed class OverrideDisposeBinaryWriter : BinaryWriter
        {
            public bool DisposeInvoked;
            public OverrideDisposeBinaryWriter(Stream output) : base(output) { }
            protected override void Dispose(bool disposing) => DisposeInvoked = true;
        }

        private sealed class OverrideDisposeAndDisposeAsyncBinaryWriter : BinaryWriter
        {
            public bool DisposeInvoked, DisposeAsyncInvoked;
            public OverrideDisposeAndDisposeAsyncBinaryWriter(Stream output) : base(output) { }
            protected override void Dispose(bool disposing) => DisposeInvoked = true;
            public override ValueTask DisposeAsync() { DisposeAsyncInvoked = true; return default; }
        }
    }
}
