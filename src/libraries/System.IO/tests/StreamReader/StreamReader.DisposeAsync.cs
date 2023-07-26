// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class StreamReader_disposeAsyncTest
    {

        [Fact]
        public void DisposeAsync_CanInvokeMultipleTimes()
        {
            var ms = new MemoryStream();
            var sr = new StreamReader(ms);
            Assert.True(sr.DisposeAsync().IsCompletedSuccessfully);
            Assert.True(sr.DisposeAsync().IsCompletedSuccessfully);
        }

        [Fact]
        public void DisposeAsync_CanDisposeAsyncAfterDispose()
        {
            var ms = new MemoryStream();
            var sr = new StreamReader(ms);
            sr.Dispose();
            Assert.True(sr.DisposeAsync().IsCompletedSuccessfully);
        }

        [Fact]
        public async Task DisposeAsync_LeaveOpenTrue_LeftOpen()
        {
            var ms = new MemoryStream();
            var sr = new StreamReader(ms, Encoding.ASCII, false, 0x1000, leaveOpen: true);
            await sr.DisposeAsync();
            Assert.Equal(0, ms.Position); // doesn't throw
        }

        [Fact]
        public async Task DisposeAsync_DerivedTypeForcesDisposeToBeUsedUnlessOverridden()
        {
            var ms = new MemoryStream();
            var sr = new OverrideDisposeStreamReader(ms);
            Assert.False(sr.DisposeInvoked);
            await sr.DisposeAsync();
            Assert.True(sr.DisposeInvoked);
        }

        [Fact]
        public async Task DisposeAsync_DerivedTypeDisposeAsyncInvoked()
        {
            var ms = new MemoryStream();
            var sr = new OverrideDisposeAndDisposeAsyncStreamReader(ms);
            Assert.False(sr.DisposeInvoked);
            Assert.False(sr.DisposeAsyncInvoked);
            await sr.DisposeAsync();
            Assert.False(sr.DisposeInvoked);
            Assert.True(sr.DisposeAsyncInvoked);
        }

        private sealed class OverrideDisposeStreamReader : StreamReader
        {
            public bool DisposeInvoked;
            public OverrideDisposeStreamReader(Stream output) : base(output) { }
            protected override void Dispose(bool disposing) => DisposeInvoked = true;
        }

        private sealed class OverrideDisposeAndDisposeAsyncStreamReader : StreamReader
        {
            public bool DisposeInvoked, DisposeAsyncInvoked;
            public OverrideDisposeAndDisposeAsyncStreamReader(Stream output) : base(output) { }
            protected override void Dispose(bool disposing) => DisposeInvoked = true;
            public override ValueTask DisposeAsync() { DisposeAsyncInvoked = true; return default; }
        }
    }
}
