#pragma warning disable IDE0073
// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.
#pragma warning restore IDE0073

using System;
using System.IO;

#if NETSTANDARD2_0
using ArgumentOutOfRangeException = Microsoft.NET.HostModel.ThrowHelpers.ArgumentOutOfRangeException;
using ObjectDisposedException = Microsoft.NET.HostModel.ThrowHelpers.ObjectDisposedException;
#endif

namespace Microsoft.NET.HostModel.MachO.Streams
{
    /// <summary>
    /// Defines a stream as a slice of another existing stream.
    /// </summary>
    internal sealed class SliceStream : Stream
    {
        private Stream _baseStream;
        private readonly long _length;
        private readonly long _basePosition;
        private long _localPosition;

        public SliceStream(Stream baseStream, long position, long length)
        {
            if (!baseStream.CanSeek) throw new ArgumentException("Invalid base stream that can't be seek.");
            ArgumentOutOfRangeException.ThrowIfNegative(position, nameof(position));
            ArgumentOutOfRangeException.ThrowIfNegative(length, nameof(length));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(length + position, baseStream.Length, nameof(length));
            if (position + length > baseStream.Length) throw new System.ArgumentOutOfRangeException(nameof(position), $"The position {position} + length {length} > baseStream.Length {baseStream.Length}");

            _baseStream = baseStream;
            _length = length;
            _basePosition = position;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();

            long remaining = _length - _localPosition;
            if (remaining <= 0) return 0;
            if (remaining < count) count = (int)remaining;

            var savedPosition = _baseStream.Position;
            _baseStream.Position = _basePosition + _localPosition;
            int read = _baseStream.Read(buffer, offset, count);
            _localPosition += read;
            _baseStream.Position = savedPosition;

            return read;
        }
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_baseStream == null, GetType().Name);
        }
        public override long Length
        {
            get { ThrowIfDisposed(); return _length; }
        }
        public override bool CanRead
        {
            get { ThrowIfDisposed(); return _baseStream.CanRead; }
        }
        public override bool CanWrite
        {
            get { ThrowIfDisposed(); return _baseStream.CanWrite; }
        }
        public override bool CanSeek
        {
            get { ThrowIfDisposed(); return _baseStream.CanSeek; }
        }
        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _localPosition;
            }
            set => Seek(value, SeekOrigin.Begin);
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = _localPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition += offset;
                    break;
                case SeekOrigin.End:
                    newPosition = _length - offset;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            if (newPosition < 0) throw new System.ArgumentOutOfRangeException(nameof(offset), $"New resulting position {newPosition} is < 0");
            if (newPosition > _length) throw new System.ArgumentOutOfRangeException(nameof(offset), $"New resulting position {newPosition} is > Length {_length}");

            // Check that we can seek on the origin stream
            _baseStream.Position = _basePosition + newPosition;
            _localPosition = newPosition;

            return newPosition;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            ThrowIfDisposed(); _baseStream.Flush();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                /*if (_baseStream != null)
                {
                    try
                    {
                        _baseStream.Dispose();
                    }
                    catch
                    {
                        // ignored
                    }*/
                    _baseStream = Stream.Null;
                //}
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));
            if (count == 0) return;

            var isOverLength = _localPosition + count > Length;
            var maxLength = isOverLength ? (int)(Length - _localPosition) : count;
            var savedPosition = _baseStream.Position;
            _baseStream.Position = _basePosition + _localPosition;
            _baseStream.Write(buffer, offset, maxLength);
            _baseStream.Position = savedPosition;
            _localPosition += maxLength;
            if (isOverLength)
            {
                throw new InvalidOperationException("Cannot write outside of this stream slice");
            }
        }
    }
}
