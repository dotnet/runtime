// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace System.Reflection.Internal
{
    internal sealed class ImmutableMemoryStream : Stream
    {
        private readonly ImmutableArray<byte> _array;
        private int _position;

        internal ImmutableMemoryStream(ImmutableArray<byte> array)
        {
            Debug.Assert(!array.IsDefault);
            _array = array;
        }

        public ImmutableArray<byte> GetBuffer()
        {
            return _array;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _array.Length; }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (value < 0 || value >= _array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _position = (int)value;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = Math.Min(count, _array.Length - _position);
            _array.CopyTo(_position, buffer, offset, result);
            _position += result;
            return result;
        }

#if NETCOREAPP
        // Duplicate the Read(byte[]) logic here instead of refactoring both to use Spans
        // so we don't affect perf on .NET Framework.
        public override int Read(Span<byte> buffer)
        {
            int result = Math.Min(buffer.Length, _array.Length - _position);
            _array.AsSpan(_position, result).CopyTo(buffer);
            _position += result;
            return result;
        }
#endif

        public override long Seek(long offset, SeekOrigin origin)
        {
            long target;
            try
            {
                target = origin switch
                {
                    SeekOrigin.Begin => offset,
                    SeekOrigin.Current => checked(offset + _position),
                    SeekOrigin.End => checked(offset + _array.Length),
                    _ => throw new ArgumentOutOfRangeException(nameof(origin)),
                };
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (target < 0 || target >= _array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            _position = (int)target;
            return target;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
