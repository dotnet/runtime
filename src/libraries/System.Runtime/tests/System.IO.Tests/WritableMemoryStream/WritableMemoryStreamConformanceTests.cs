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
            // WritableMemoryStream wraps a fixed-capacity Memory<byte> buffer where Length == capacity.
            // Unlike MemoryStream, there's no concept of "logical length" separate from capacity.
            // This means WritableMemoryStream doesn't support the common pattern of creating an empty stream
            // and writing to it to grow it. Many conformance tests rely on this pattern.
            //
            // Returning null here skips tests that require creating an initially-empty writable stream,
            // as those tests fundamentally conflict with WritableMemoryStream's buffer-wrapping semantics.
            if (initialData == null || initialData.Length == 0)
            {
                return Task.FromResult<Stream?>(null);
            }

            var memory = new Memory<byte>(initialData);
            return Task.FromResult<Stream?>(new WritableMemoryStream(memory));
        }

        // Note to both skipped tests: It was already verified that this works when using just WritableMemoryStream,
        // before adding the 'forking' in Stream behavior for fast-path MemoryStream usage.

    }
}
