// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Memory.Tests
{
    /// <summary>
    /// Delegating <see cref="Stream"/> wrapper that forwards every operation to an inner
    /// <see cref="Stream"/> while also taking ownership of an associated
    /// <see cref="System.Buffers.NativeMemoryManager"/>. Disposes the manager when the stream is
    /// disposed so the conformance harness can release the unmanaged buffer at end-of-test.
    /// </summary>
    internal sealed class NativeMemoryOwningStream : Stream
    {
        private readonly System.Buffers.NativeMemoryManager _owner;
        private readonly Stream _inner;

        public NativeMemoryOwningStream(Stream inner, System.Buffers.NativeMemoryManager owner)
        {
            _inner = inner;
            _owner = owner;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => _inner.Read(buffer);
        public override int ReadByte() => _inner.ReadByte();
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);
        public override void WriteByte(byte value) => _inner.WriteByte(value);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                ((IDisposable)_owner).Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

