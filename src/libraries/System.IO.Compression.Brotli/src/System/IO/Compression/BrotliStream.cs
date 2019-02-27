// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public sealed partial class BrotliStream : Stream
    {
        private const int DefaultInternalBufferSize = (1 << 16) - 16; //65520;
        private Stream _stream;
        private byte[] _buffer;
        private readonly bool _leaveOpen;
        private readonly CompressionMode _mode;

        public BrotliStream(Stream stream, CompressionMode mode) : this(stream, mode, leaveOpen: false) { }
        public BrotliStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            switch (mode)
            {
                case CompressionMode.Compress:
                    if (!stream.CanWrite)
                        throw new ArgumentException(SR.Stream_FalseCanWrite, nameof(stream));
                    break;
                case CompressionMode.Decompress:
                    if (!stream.CanRead)
                        throw new ArgumentException(SR.Stream_FalseCanRead, nameof(stream));
                    break;
                default:
                    throw new ArgumentException(SR.ArgumentOutOfRange_Enum, nameof(mode));
            }

            _mode = mode;
            _stream = stream;
            _leaveOpen = leaveOpen;
            _buffer = ArrayPool<byte>.Shared.Rent(DefaultInternalBufferSize);
        }

        private void EnsureNotDisposed()
        {
            if (_stream == null)
                throw new ObjectDisposedException(GetType().Name, SR.ObjectDisposed_StreamClosed);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _stream != null)
                {
                    if (_mode == CompressionMode.Compress)
                    {
                        WriteCore(ReadOnlySpan<byte>.Empty, isFinalBlock: true);
                    }

                    if (!_leaveOpen)
                    {
                        _stream.Dispose();
                    }
                }
            }
            finally
            {
                ReleaseStateForDispose();
                base.Dispose(disposing);
            }
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                if (_stream != null)
                {
                    if (_mode == CompressionMode.Compress)
                    {
                        await WriteAsyncMemoryCore(ReadOnlyMemory<byte>.Empty, CancellationToken.None, isFinalBlock: true).ConfigureAwait(false);
                    }

                    if (!_leaveOpen)
                    {
                        await _stream.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                ReleaseStateForDispose();
            }
        }

        private void ReleaseStateForDispose()
        {
            _stream = null;
            _encoder.Dispose();
            _decoder.Dispose();

            byte[] buffer = _buffer;
            if (buffer != null)
            {
                _buffer = null;
                if (!AsyncOperationIsActive)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private static void ValidateParameters(byte[] array, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedPosNum);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedPosNum);

            if (array.Length - offset < count)
                throw new ArgumentException(SR.InvalidArgumentOffsetCount);
        }

        public Stream BaseStream => _stream;
        public override bool CanRead => _mode == CompressionMode.Decompress  && _stream != null && _stream.CanRead;
        public override bool CanWrite => _mode == CompressionMode.Compress  && _stream != null && _stream.CanWrite;
        public override bool CanSeek => false;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void SetLength(long value) => throw new NotSupportedException();

        private int _activeAsyncOperation; // 1 == true, 0 == false
        private bool AsyncOperationIsActive => _activeAsyncOperation != 0;

        private void EnsureNoActiveAsyncOperation()
        {
            if (AsyncOperationIsActive)
                ThrowInvalidBeginCall();
        }

        private void AsyncOperationStarting()
        {
            if (Interlocked.CompareExchange(ref _activeAsyncOperation, 1, 0) != 0)
            {
                ThrowInvalidBeginCall();
            }
        }

        private void AsyncOperationCompleting()
        {
            int oldValue = Interlocked.CompareExchange(ref _activeAsyncOperation, 0, 1);
            Debug.Assert(oldValue == 1, $"Expected {nameof(_activeAsyncOperation)} to be 1, got {oldValue}");
        }

        private static void ThrowInvalidBeginCall()
        {
            throw new InvalidOperationException(SR.InvalidBeginCall);
        }
    }
}
