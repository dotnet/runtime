// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class StreamWriterTests
    {

        [Fact]
        public void DisposeAsync_CanInvokeMultipleTimes()
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            Assert.True(sw.DisposeAsync().IsCompletedSuccessfully);
            Assert.True(sw.DisposeAsync().IsCompletedSuccessfully);
        }

        [Fact]
        public void DisposeAsync_CanDisposeAsyncAfterDispose()
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            sw.Dispose();
            Assert.True(sw.DisposeAsync().IsCompletedSuccessfully);
        }

        [Fact]
        public async Task DisposeAsync_FlushesStream()
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            try
            {
                sw.Write("hello");
                Assert.Equal(0, ms.Position);
            }
            finally
            {
                await sw.DisposeAsync();
            }
            Assert.Throws<ObjectDisposedException>(() => ms.Position);
            Assert.Equal(5, ms.ToArray().Length);
        }

        [Fact]
        public async Task DisposeAsync_LeaveOpenTrue_LeftOpen()
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms, Encoding.ASCII, 0x1000, leaveOpen: true);
            try
            {
                sw.Write("hello");
            }
            finally
            {
                await sw.DisposeAsync();
            }
            Assert.Equal(5, ms.Position); // doesn't throw
        }

        [Fact]
        public async Task DisposeAsync_DerivedTypeForcesDisposeToBeUsedUnlessOverridden()
        {
            var ms = new MemoryStream();
            var sw = new OverrideDisposeStreamWriter(ms);
            try
            {
                sw.Write("hello");
            }
            finally
            {
                Assert.False(sw.DisposeInvoked);
                await sw.DisposeAsync();
                Assert.True(sw.DisposeInvoked);
            }
        }

        [Fact]
        public async Task DisposeAsync_DerivedTypeDisposeAsyncInvoked()
        {
            var ms = new MemoryStream();
            var sw = new OverrideDisposeAndDisposeAsyncStreamWriter(ms);
            try
            {
                sw.Write("hello");
            }
            finally
            {
                Assert.False(sw.DisposeInvoked);
                Assert.False(sw.DisposeAsyncInvoked);
                await sw.DisposeAsync();
                Assert.False(sw.DisposeInvoked);
                Assert.True(sw.DisposeAsyncInvoked);
            }
        }

        private sealed class OverrideDisposeStreamWriter : StreamWriter
        {
            public bool DisposeInvoked;
            public OverrideDisposeStreamWriter(Stream output) : base(output) { }
            protected override void Dispose(bool disposing) => DisposeInvoked = true;
        }

        private sealed class OverrideDisposeAndDisposeAsyncStreamWriter : StreamWriter
        {
            public bool DisposeInvoked, DisposeAsyncInvoked;
            public OverrideDisposeAndDisposeAsyncStreamWriter(Stream output) : base(output) { }
            protected override void Dispose(bool disposing) => DisposeInvoked = true;
            public override ValueTask DisposeAsync() { DisposeAsyncInvoked = true; return default; }
        }
    }
}
