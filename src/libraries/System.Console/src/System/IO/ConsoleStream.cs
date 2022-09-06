// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // Provides the platform-agnostic functionality for streams used as console input and output.
    // Platform-specific implementations derive from ConsoleStream to implement Read and Write
    // (and optionally Flush), as well as any additional ctor/Dispose logic necessary.
    internal abstract class ConsoleStream : Stream
    {
        private bool _canRead, _canWrite;

        internal ConsoleStream(FileAccess access)
        {
            Debug.Assert(access == FileAccess.Read || access == FileAccess.Write);
            _canRead = ((access & FileAccess.Read) == FileAccess.Read);
            _canWrite = ((access & FileAccess.Write) == FileAccess.Write);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateWrite(buffer, offset, count);
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override void WriteByte(byte value) => Write(new ReadOnlySpan<byte>(in value));

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateWrite(buffer, offset, count);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                Write(new ReadOnlySpan<byte>(buffer, offset, count));
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ValidateCanWrite();

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            try
            {
                Write(buffer.Span);
                return ValueTask.CompletedTask;
            }
            catch (Exception ex)
            {
                return ValueTask.FromException(ex);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateRead(buffer, offset, count);
            return Read(new Span<byte>(buffer, offset, count));
        }

        public override int ReadByte()
        {
            byte b = 0;
            int result = Read(new Span<byte>(ref b));
            return result != 0 ? b : -1;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateRead(buffer, offset, count);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            try
            {
                return Task.FromResult(Read(new Span<byte>(buffer, offset, count)));
            }
            catch (Exception exception)
            {
                return Task.FromException<int>(exception);
            }
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ValidateCanRead();

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            try
            {
                return ValueTask.FromResult(Read(buffer.Span));
            }
            catch (Exception exception)
            {
                return ValueTask.FromException<int>(exception);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _canRead = false;
            _canWrite = false;
            base.Dispose(disposing);
        }

        public sealed override bool CanRead => _canRead;

        public sealed override bool CanWrite => _canWrite;

        public sealed override bool CanSeek => false;

        public sealed override long Length => throw Error.GetSeekNotSupported();

        public sealed override long Position
        {
            get => throw Error.GetSeekNotSupported();
            set => throw Error.GetSeekNotSupported();
        }

        public override void Flush() { }

        public sealed override void SetLength(long value) => throw Error.GetSeekNotSupported();

        public sealed override long Seek(long offset, SeekOrigin origin) => throw Error.GetSeekNotSupported();

        protected void ValidateRead(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            ValidateCanRead();
        }

        private void ValidateCanRead()
        {
            if (!_canRead)
            {
                throw Error.GetReadNotSupported();
            }
        }

        protected void ValidateWrite(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            ValidateCanWrite();
        }

        private void ValidateCanWrite()
        {
            if (!_canWrite)
            {
                throw Error.GetWriteNotSupported();
            }
        }
    }
}
