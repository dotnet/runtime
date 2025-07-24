// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Threading;

namespace System.IO.Compression.Tests.Utilities
{
    public sealed class ClampedReadStream : Stream
    {
        public Stream BaseStream { get; }

        public int ReadSizeLimit { get; }

        public override bool CanRead =>
            BaseStream.CanRead;

        public override bool CanSeek =>
            BaseStream.CanSeek;

        public override bool CanWrite =>
            BaseStream.CanWrite;

        public override long Length =>
            BaseStream.Length;

        public override bool CanTimeout => BaseStream.CanTimeout;

        public override long Position { get => BaseStream.Position; set => BaseStream.Position = value; }

        public override int ReadTimeout { get => BaseStream.ReadTimeout; set => BaseStream.ReadTimeout = value; }

        public override int WriteTimeout { get => BaseStream.WriteTimeout; set => BaseStream.WriteTimeout = value; }

        public ClampedReadStream(Stream baseStream, int readSizeLimit)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(readSizeLimit, 1);

            BaseStream = baseStream;
            ReadSizeLimit = readSizeLimit;
        }

        public override void Flush() =>
            BaseStream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            BaseStream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) =>
            BaseStream.Read(buffer, offset, Math.Clamp(count, 0, ReadSizeLimit));

        public override int Read(Span<byte> buffer) =>
            BaseStream.Read(buffer.Slice(0, Math.Clamp(buffer.Length, 0, ReadSizeLimit)));

        public override int ReadByte() =>
            BaseStream.ReadByte();

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            BaseStream.BeginRead(buffer, offset, Math.Clamp(count, 0, ReadSizeLimit), callback, state);

        public override int EndRead(IAsyncResult asyncResult) =>
            BaseStream.EndRead(asyncResult);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            BaseStream.ReadAsync(buffer, offset, Math.Clamp(count, 0, ReadSizeLimit), cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            BaseStream.ReadAsync(buffer.Slice(0, Math.Clamp(buffer.Length, 0, ReadSizeLimit)), cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) =>
            BaseStream.Seek(offset, origin);

        public override void SetLength(long value) =>
            BaseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) =>
            BaseStream.Write(buffer, offset, count);

        public override void Write(ReadOnlySpan<byte> buffer) =>
            BaseStream.Write(buffer);

        public override void CopyTo(Stream destination, int bufferSize) =>
            BaseStream.CopyTo(destination, bufferSize);

        public override void WriteByte(byte value) =>
            BaseStream.WriteByte(value);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            BaseStream.BeginWrite(buffer, offset, count, callback, state);

        public override void EndWrite(IAsyncResult asyncResult) =>
            BaseStream.EndWrite(asyncResult);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            BaseStream.WriteAsync(buffer, offset, count, cancellationToken);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            BaseStream.WriteAsync(buffer, cancellationToken);

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
            BaseStream.CopyToAsync(destination, bufferSize, cancellationToken);

        public override ValueTask DisposeAsync() =>
            BaseStream.DisposeAsync();

        public override void Close() =>
            BaseStream.Close();
    }
}
