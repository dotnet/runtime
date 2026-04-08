// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Buffers;
using System.IO.Tests;
using System.Threading.Tasks;

namespace System.Memory.Tests
{
    /// <summary>
    /// Conformance tests for ReadOnlySequenceStream - a read-only, seekable stream
    /// wrapper around ReadOnlySequence{byte}.
    /// </summary>
    public class ROSequenceStreamConformanceTests : StandaloneStreamConformanceTests
    {
        // StreamConformanceTests flags to specify capabilities
        protected override bool CanSeek => true;
        // SetLength() is not supported because ReadOnlySequence{byte} is immutable.
        protected override bool CanSetLength => false;
        // ReadOnlySequenceStream doesn't buffer writes (it's read-only),
        protected override bool NopFlushCompletesSynchronously => true;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            if (initialData == null || initialData.Length == 0)
            {
                // Create empty sequence for null or empty data
                var emptySequence = ReadOnlySequence<byte>.Empty;
                return Task.FromResult<Stream?>(new ReadOnlySequenceStream(emptySequence));
            }

            // ReadOnlySequence<byte> can be constructed from:
            // 1. ReadOnlyMemory<byte> (single segment)
            // 2. ReadOnlySequenceSegment<byte> chain (multi-segment)
            var sequence = new ReadOnlySequence<byte>(initialData); // Single segment
            return Task.FromResult<Stream?>(new ReadOnlySequenceStream(sequence));
        }

        // Immutable
        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);


        // Immutable
        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);
    }
}
