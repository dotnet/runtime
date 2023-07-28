// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>Provides a stream whose implementation is supplied by delegates or by an inner stream.</summary>
    internal sealed class DelegateDelegatingStream : DelegatingStream
    {
        public delegate int ReadSpanDelegate(Span<byte> buffer);

        public static DelegateDelegatingStream NopDispose(Stream innerStream) =>
            new DelegateDelegatingStream(innerStream)
            {
                DisposeFunc = _ => { },
                DisposeAsyncFunc = () => ValueTask.CompletedTask
            };

        public DelegateDelegatingStream(Stream innerStream) : base(innerStream) { }

        public Func<bool> CanReadFunc { get; set; }
        public Func<bool> CanSeekFunc { get; set; }
        public Func<bool> CanWriteFunc { get; set; }
        public Action FlushFunc { get; set; }
        public Func<CancellationToken, Task> FlushAsyncFunc { get; set; }
        public Func<long> LengthFunc { get; set; }
        public Func<long> GetPositionFunc { get; set; }
        public Action<long> SetPositionFunc { get; set; }
        public Func<byte[], int, int, int> ReadFunc { get; set; }
        public ReadSpanDelegate ReadSpanFunc { get; set; }
        public Func<byte[], int, int, CancellationToken, Task<int>> ReadAsyncArrayFunc { get; set; }
        public Func<Memory<byte>, CancellationToken, ValueTask<int>> ReadAsyncMemoryFunc { get; set; }
        public Func<long, SeekOrigin, long> SeekFunc { get; set; }
        public Action<long> SetLengthFunc { get; set; }
        public Action<byte[], int, int> WriteFunc { get; set; }
        public Func<byte[], int, int, CancellationToken, Task> WriteAsyncArrayFunc { get; set; }
        public Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> WriteAsyncMemoryFunc { get; set; }
        public Action<bool> DisposeFunc { get; set; }
        public Func<ValueTask> DisposeAsyncFunc { get; set; }

        public override bool CanRead => CanReadFunc != null ? CanReadFunc() : base.CanRead;
        public override bool CanWrite => CanWriteFunc != null ? CanWriteFunc() : base.CanWrite;
        public override bool CanSeek => CanSeekFunc != null ? CanSeekFunc() : base.CanSeek;

        public override void Flush() { if (FlushFunc != null) FlushFunc(); else base.Flush(); }
        public override Task FlushAsync(CancellationToken cancellationToken) => FlushAsyncFunc != null ? FlushAsyncFunc(cancellationToken) : base.FlushAsync(cancellationToken);

        public override long Length => LengthFunc != null ? LengthFunc() : base.Length;
        public override long Position => GetPositionFunc != null ? GetPositionFunc() : base.Position;

        public override int Read(byte[] buffer, int offset, int count) => ReadFunc != null ? ReadFunc(buffer, offset, count) : base.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => ReadSpanFunc != null ? ReadSpanFunc(buffer) : base.Read(buffer);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsyncArrayFunc != null ? ReadAsyncArrayFunc(buffer, offset, count, cancellationToken) : base.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => ReadAsyncMemoryFunc != null ? ReadAsyncMemoryFunc(buffer, cancellationToken) : base.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => SeekFunc != null ? SeekFunc(offset, origin) : base.Seek(offset, origin);
        public override void SetLength(long value) { if (SetLengthFunc != null) SetLengthFunc(value); else base.SetLength(value); }

        public override void Write(byte[] buffer, int offset, int count) { if (WriteFunc != null) WriteFunc(buffer, offset, count); else base.Write(buffer, offset, count); }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsyncArrayFunc != null ? WriteAsyncArrayFunc(buffer, offset, count, cancellationToken) : base.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => WriteAsyncMemoryFunc != null ? WriteAsyncMemoryFunc(buffer, cancellationToken) : base.WriteAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing) { if (DisposeFunc != null) DisposeFunc(disposing); else base.Dispose(disposing); }
        public override ValueTask DisposeAsync() => DisposeAsyncFunc != null ? DisposeAsyncFunc() : base.DisposeAsync();
    }
}
