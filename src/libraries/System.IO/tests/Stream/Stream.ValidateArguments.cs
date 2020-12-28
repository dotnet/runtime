// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class StreamArgumentValidation
    {
        [Fact]
        public void ValidateBufferArguments()
        {
            AssertExtensions.Throws<ArgumentNullException>("buffer", () => ExposeProtectedStream.ValidateBufferArguments(null, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ExposeProtectedStream.ValidateBufferArguments(new byte[3], -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ExposeProtectedStream.ValidateBufferArguments(new byte[3], 4, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ExposeProtectedStream.ValidateBufferArguments(new byte[3], 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ExposeProtectedStream.ValidateBufferArguments(new byte[3], 0, 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ExposeProtectedStream.ValidateBufferArguments(new byte[3], 3, 1));
        }

        [Fact]
        public void ValidateCopyToArguments()
        {
            AssertExtensions.Throws<ArgumentNullException>("destination", () => ExposeProtectedStream.ValidateCopyToArguments(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => ExposeProtectedStream.ValidateCopyToArguments(new MemoryStream(), 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => ExposeProtectedStream.ValidateCopyToArguments(new MemoryStream(), -1));

            var srcDisposed = new MemoryStream();
            srcDisposed.Dispose();

            var dstDisposed = new MemoryStream();
            dstDisposed.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ExposeProtectedStream.ValidateCopyToArguments(dstDisposed, 1));
            Assert.Throws<ObjectDisposedException>(() => ExposeProtectedStream.ValidateCopyToArguments(dstDisposed, 1));

            Assert.Throws<NotSupportedException>(() => ExposeProtectedStream.ValidateCopyToArguments(new MemoryStream(new byte[1], writable: false), 1));
        }

        private abstract class ExposeProtectedStream : Stream
        {
            public static new void ValidateBufferArguments(byte[] buffer, int offset, int count) =>
                Stream.ValidateBufferArguments(buffer, offset, count);

            public static new void ValidateCopyToArguments(Stream destination, int bufferSize) =>
                Stream.ValidateCopyToArguments(destination, bufferSize);
        }
    }
}
