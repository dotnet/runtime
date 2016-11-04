// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public partial class BufferedStreamTests
    {
        [Fact]
        public void UnderlyingStream()
        {
            var underlyingStream = new MemoryStream();
            var bufferedStream = new BufferedStream(underlyingStream);
            Assert.Same(underlyingStream, bufferedStream.UnderlyingStream);
        }

        [Fact]
        public void BufferSize()
        {
            var bufferedStream = new BufferedStream(new MemoryStream(), 1234);
            Assert.Equal(1234, bufferedStream.BufferSize);
        }
    }
}
