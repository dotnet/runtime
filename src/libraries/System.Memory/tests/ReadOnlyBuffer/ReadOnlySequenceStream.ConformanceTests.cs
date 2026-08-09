// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Buffers;
using System.IO.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Memory.Tests
{
    public class ROSequenceStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => true;
        protected override bool CanSetLength => false;
        protected override bool NopFlushCompletesSynchronously => true;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            if (initialData is null || initialData.Length == 0)
            {
                return Task.FromResult<Stream?>(
                    new ReadOnlySequenceStream(ReadOnlySequence<byte>.Empty));
            }

            return Task.FromResult<Stream?>(
                new ReadOnlySequenceStream(CreateSequence(initialData)));
        }

        protected virtual ReadOnlySequence<byte> CreateSequence(byte[] data)
            => new ReadOnlySequence<byte>(data);

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData)
            => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData)
            => Task.FromResult<Stream?>(null);

        public override async Task Seek_PastEnd_ReadReturns0(SeekMode mode)
        {
            await base.Seek_PastEnd_ReadReturns0(mode);

            // ReadOnlySequenceStream-specific: seeking past end against an empty sequence
            // is allowed, Position reflects the requested offset, and reads stay at 0.
            var stream = new ReadOnlySequenceStream(ReadOnlySequence<byte>.Empty);
            Assert.Equal(0, stream.Length);
            Assert.Equal(0, stream.Position);

            byte[] buffer = new byte[10];
            Assert.Equal(0, stream.Read(buffer, 0, 10));

            stream.Seek(0, SeekOrigin.Begin);
            Assert.Equal(0, stream.Position);

            long newPosition = stream.Seek(1, SeekOrigin.Begin);
            Assert.Equal(1, newPosition);
            Assert.Equal(1, stream.Position);
        }
    }

    /// <summary>
    /// Runs the full conformance suite against multi-segment sequences (3 segments).
    /// </summary>
    public class ROSequenceStreamMultiSegmentConformanceTests : ROSequenceStreamConformanceTests
    {
        protected override ReadOnlySequence<byte> CreateSequence(byte[] data)
            => ReadOnlySequenceFactory<byte>.SplitInThree.CreateWithContent(data);
    }

    /// <summary>
    /// Runs the full conformance suite against maximally-fragmented sequences
    /// (one byte per segment with empty segments interspersed).
    /// </summary>
    public class ROSequenceStreamSegmentPerItemConformanceTests : ROSequenceStreamConformanceTests
    {
        protected override ReadOnlySequence<byte> CreateSequence(byte[] data)
            => ReadOnlySequenceFactory<byte>.SegmentPerItemFactory.CreateWithContent(data);
    }
}
