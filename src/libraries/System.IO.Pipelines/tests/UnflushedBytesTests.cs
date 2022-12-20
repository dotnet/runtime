// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class UnflushedBytesTests : PipeTest
    {
        internal class MinimalPipeWriter : PipeWriter
        {
            public override void Advance(int bytes) => throw new NotImplementedException();
            public override void CancelPendingFlush() => throw new NotImplementedException();
            public override void Complete(Exception? exception = null) => throw new NotImplementedException();
            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public override Memory<byte> GetMemory(int sizeHint = 0) => throw new NotImplementedException();
            public override Span<byte> GetSpan(int sizeHint = 0) => throw new NotImplementedException();
        }

        public UnflushedBytesTests() : base(0, 0)
        {
        }

        [Fact]
        public void NonOverriddenUnflushedBytesThrows()
        {
            MinimalPipeWriter writer = new MinimalPipeWriter();
            Assert.False(writer.CanGetUnflushedBytes);
            _ = Assert.Throws<NotSupportedException>(() => { long value = writer.UnflushedBytes; }); ;
        }

        [Fact]
        public void UnflushedBytesWorks()
        {
            byte[] bytes = "abcdefghijklmnopqrstuvwzyz"u8.ToArray();
            Pipe.Writer.Write(bytes);
            Assert.True(Pipe.Writer.CanGetUnflushedBytes);
            Assert.Equal(bytes.Length,Pipe.Writer.UnflushedBytes);
            _ = Pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            Assert.Equal(0, Pipe.Writer.UnflushedBytes);
        }
    }
}
