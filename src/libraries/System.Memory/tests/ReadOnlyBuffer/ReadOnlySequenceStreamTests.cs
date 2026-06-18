// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Buffers;
using System.IO;
using Xunit;

namespace System.Memory.Tests
{
    public class ReadOnlySequenceStreamTests
    {
        [Fact]
        public void SeekingBeyondEmptyBufferIsAllowed()
        {
            var stream = new ReadOnlySequenceStream(ReadOnlySequence<byte>.Empty);

            Assert.Equal(0, stream.Length);
            Assert.Equal(0, stream.Position);

            byte[] buffer = new byte[10];
            int bytesRead = stream.Read(buffer, 0, 10);
            Assert.Equal(0, bytesRead);

            stream.Seek(0, SeekOrigin.Begin);
            Assert.Equal(0, stream.Position);

            long newPosition = stream.Seek(1, SeekOrigin.Begin);
            Assert.Equal(1, newPosition);
            Assert.Equal(1, stream.Position);
        }
    }
}
