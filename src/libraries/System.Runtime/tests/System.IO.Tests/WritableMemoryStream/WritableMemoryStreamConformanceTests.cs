// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class WritableMemoryStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => true;
        protected override bool CanSetLength => false;
        protected override bool NopFlushCompletesSynchronously => true;
        // This stream can't grow beyond initial capacity
        protected override bool CanSetLengthGreaterThanCapacity => false;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData)
        {
            // WritableMemoryStream wraps a fixed-capacity Memory<byte> buffer.
            // Length starts at 0 and grows as data is written, but the buffer cannot expand.
            // Returning null for empty data skips conformance tests that rely on
            // creating an initially-empty stream and growing it via writes.
            if (initialData is null || initialData.Length == 0)
            {
                return Task.FromResult<Stream?>(null);
            }

            var memory = new Memory<byte>(new byte[initialData.Length]);
            var stream = new WritableMemoryStream(memory);
            stream.Write(initialData, 0, initialData.Length);
            stream.Position = 0;
            return Task.FromResult<Stream?>(stream);
        }
    }
}
