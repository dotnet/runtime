// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // A MemoryStream represents a Stream in memory (ie, it has no backing store).
    // This stream may reduce the need for temporary buffers and files in
    // an application.
    //
    // There are two ways to create a MemoryStream.  You can initialize one
    // from an unsigned byte array, or you can create an empty one.  Empty
    // memory streams are resizable, while ones created with a byte array provide
    // a stream "view" of the data.
    public class MemoryStream : Stream
    {
        private byte[] _buffer;    // Either allocated internally or externally.
        private readonly int _origin;       // For user-provided arrays, start at this origin
        private int _position;     // read/write head.
        private int _length;       // Number of bytes within the memory stream
        private int _capacity;     // length of usable portion of buffer for stream
        // Note that _capacity == _buffer.Length for non-user-provided byte[]'s

        private bool _expandable;  // User-provided buffers aren't expandable.
        private bool _writable;    // Can user write to this stream?
        private readonly bool _exposable;   // Whether the array can be returned to the user.
        private bool _isOpen;      // Is this stream open or closed?

        private CachedCompletedInt32Task _lastReadTask; // The last successful task returned from ReadAsync

        // When non-null, all operations are delegated to this instance.
        // Only set by the ReadOnlyMemory<byte>/Memory<byte> constructors.
        private readonly MemoryMemoryStream? _memoryMemoryStream;

        private static int MemStreamMaxLength => Array.MaxLength;

        public MemoryStream()
            : this(0)
        {
        }

        public MemoryStream(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(capacity, MemStreamMaxLength);

            _buffer = capacity != 0 ? new byte[capacity] : [];
            _capacity = capacity;
            _expandable = true;
            _writable = true;
            _exposable = true;
            _isOpen = true;
        }

        public MemoryStream(byte[] buffer)
            : this(buffer, true)
        {
        }

        public MemoryStream(byte[] buffer, bool writable)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            _buffer = buffer;
            _length = _capacity = buffer.Length;
            _writable = writable;
            _isOpen = true;
        }

        public MemoryStream(byte[] buffer, int index, int count)
            : this(buffer, index, count, true, false)
        {
        }

        public MemoryStream(byte[] buffer, int index, int count, bool writable)
            : this(buffer, index, count, writable, false)
        {
        }

        public MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (buffer.Length - index < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            _buffer = buffer;
            _origin = _position = index;
            _length = _capacity = index + count;
            _writable = writable;
            _exposable = publiclyVisible;  // Can TryGetBuffer/GetBuffer return the array?
            _isOpen = true;
        }

        /// <summary>Initializes a new non-writable instance of the <see cref="MemoryStream"/> class based on the specified <see cref="ReadOnlyMemory{T}"/>.</summary>
        /// <param name="memory">The read-only memory from which to create the current stream.</param>
        public MemoryStream(ReadOnlyMemory<byte> memory)
        {
            _memoryMemoryStream = new MemoryMemoryStream(memory);
            _buffer = [];
            _isOpen = true;
        }

        /// <summary>Initializes a new writable instance of the <see cref="MemoryStream"/> class based on the specified <see cref="Memory{T}"/>.</summary>
        /// <param name="memory">The memory from which to create the current stream.</param>
        public MemoryStream(Memory<byte> memory)
            : this(memory, true)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MemoryStream"/> class based on the specified <see cref="Memory{T}"/> with the <see cref="CanWrite"/> property set as specified.</summary>
        /// <param name="memory">The memory from which to create the current stream.</param>
        /// <param name="writable"><see langword="true"/> to enable writing; otherwise, <see langword="false"/>.</param>
        public MemoryStream(Memory<byte> memory, bool writable)
        {
            _memoryMemoryStream = new MemoryMemoryStream(memory, writable);
            _buffer = [];
            _writable = writable;
            _isOpen = true;
        }

        public override bool CanRead => _isOpen;

        public override bool CanSeek => _isOpen;

        public override bool CanWrite => _writable;

        private void EnsureNotClosed()
        {
            if (!_isOpen)
                ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
        }

        private void EnsureWriteable()
        {
            if (!CanWrite)
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isOpen = false;
                _writable = false;
                _expandable = false;
                // Don't set buffer to null - allow TryGetBuffer, GetBuffer & ToArray to work.
                _lastReadTask = default;
                _memoryMemoryStream?.Dispose();
            }
        }

        // returns a bool saying whether we allocated a new array.
        private bool EnsureCapacity(int value)
        {
            // Check for overflow
            if (value < 0)
                throw new IOException(SR.IO_StreamTooLong);

            if (value > _capacity)
            {
                int newCapacity = Math.Max(value, 256);

                // We are ok with this overflowing since the next statement will deal
                // with the cases where _capacity*2 overflows.
                if (newCapacity < _capacity * 2)
                {
                    newCapacity = _capacity * 2;
                }

                // We want to expand the array up to Array.MaxLength.
                // And we want to give the user the value that they asked for
                if ((uint)(_capacity * 2) > Array.MaxLength)
                {
                    newCapacity = Math.Max(value, Array.MaxLength);
                }

                Capacity = newCapacity;
                return true;
            }
            return false;
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            try
            {
                Flush();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public virtual byte[] GetBuffer()
        {
            if (_memoryMemoryStream is not null)
                throw new UnauthorizedAccessException(SR.UnauthorizedAccess_MemStreamBuffer);
            if (!_exposable)
                throw new UnauthorizedAccessException(SR.UnauthorizedAccess_MemStreamBuffer);
            return _buffer;
        }

        public virtual bool TryGetBuffer(out ArraySegment<byte> buffer)
        {
            if (_memoryMemoryStream is not null)
            {
                buffer = default;
                return false;
            }

            if (!_exposable)
            {
                buffer = default;
                return false;
            }

            buffer = new ArraySegment<byte>(_buffer, offset: _origin, count: _length - _origin);
            return true;
        }

        // -------------- PERF: Internal functions for fast direct access of MemoryStream buffer (cf. BinaryReader for usage) ---------------

        // PERF: Internal sibling of GetBuffer, always returns a buffer (cf. GetBuffer())
        internal byte[] InternalGetBuffer()
        {
            return _buffer;
        }

        // PERF: True cursor position, we don't need _origin for direct access
        internal int InternalGetPosition()
        {
            return _position;
        }

        // PERF: Expose internal buffer for BinaryReader instead of going via the regular Stream interface which requires to copy the data out
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> InternalReadSpan(int count)
        {
            if (_memoryMemoryStream is not null)
                return _memoryMemoryStream.InternalReadSpan(count);

            EnsureNotClosed();

            int origPos = _position;
            int newPos = origPos + count;

            if ((uint)newPos > (uint)_length)
            {
                _position = _length;
                ThrowHelper.ThrowEndOfFileException();
            }

            var span = new ReadOnlySpan<byte>(_buffer, origPos, count);
            _position = newPos;
            return span;
        }

        // PERF: Get actual length of bytes available for read; do sanity checks; shift position - i.e. everything except actual copying bytes
        internal int InternalEmulateRead(int count)
        {
            if (_memoryMemoryStream is not null)
                return _memoryMemoryStream.InternalEmulateRead(count);

            EnsureNotClosed();

            int n = _length - _position;
            if (n > count)
                n = count;
            if (n < 0)
                n = 0;

            Debug.Assert(_position + n >= 0);  // len is less than 2^31 -1.
            _position += n;
            return n;
        }

        // PERF: Reads up to count bytes from the current position and returns them as a span, advancing the position.
        // Unlike InternalReadSpan, does not throw if fewer bytes are available.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> InternalRead(int count)
        {
            if (_memoryMemoryStream is not null)
            {
                int n = Math.Min(_memoryMemoryStream.RemainingBytes, count);
                if (n <= 0)
                    return default;
                return _memoryMemoryStream.InternalReadSpan(n);
            }

            EnsureNotClosed();

            int available = _length - _position;
            if (available > count)
                available = count;
            if (available <= 0)
                return default;

            var span = new ReadOnlySpan<byte>(_buffer, _position, available);
            _position += available;
            return span;
        }

        // Gets & sets the capacity (number of bytes allocated) for this stream.
        // The capacity cannot be set to a value less than the current length
        // of the stream.
        //
        public virtual int Capacity
        {
            get
            {
                if (_memoryMemoryStream is not null)
                    return _memoryMemoryStream.Capacity;
                EnsureNotClosed();
                return _capacity - _origin;
            }
            set
            {
                if (_memoryMemoryStream is not null)
                {
                    _memoryMemoryStream.SetCapacity(value);
                    return;
                }
                // Only update the capacity if the MS is expandable and the value is different than the current capacity.
                // Special behavior if the MS isn't expandable: we don't throw if value is the same as the current capacity
                if (value < Length)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_SmallCapacity);

                EnsureNotClosed();

                if (!_expandable && (value != Capacity))
                    throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);

                // MemoryStream has this invariant: _origin > 0 => !expandable (see ctors)
                if (_expandable && value != _capacity)
                {
                    if (value > 0)
                    {
                        byte[] newBuffer = new byte[value];
                        if (_length > 0)
                        {
                            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
                        }
                        _buffer = newBuffer;
                    }
                    else
                    {
                        _buffer = [];
                    }
                    _capacity = value;
                }
            }
        }

        public override long Length
        {
            get
            {
                if (_memoryMemoryStream is not null)
                    return _memoryMemoryStream.Length;
                EnsureNotClosed();
                return _length - _origin;
            }
        }

        public override long Position
        {
            get
            {
                if (_memoryMemoryStream is not null)
                    return _memoryMemoryStream.Position;
                EnsureNotClosed();
                return _position - _origin;
            }
            set
            {
                if (_memoryMemoryStream is not null)
                {
                    _memoryMemoryStream.Position = value;
                    return;
                }
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                EnsureNotClosed();

                if (value > MemStreamMaxLength - _origin)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Format(SR.ArgumentOutOfRange_StreamLength, Array.MaxLength));
                _position = _origin + (int)value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (_memoryMemoryStream is not null)
                return _memoryMemoryStream.Read(new Span<byte>(buffer, offset, count));

            EnsureNotClosed();

            int n = _length - _position;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;

            Debug.Assert(_position + n >= 0);  // len is less than 2^31 -1.

            if (n <= 8)
            {
                int byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _buffer[_position + byteCount];
            }
            else
                Buffer.BlockCopy(_buffer, _position, buffer, offset, n);
            _position += n;

            return n;
        }

        public override int Read(Span<byte> buffer)
        {
            if (GetType() != typeof(MemoryStream))
            {
                // MemoryStream is not sealed, and a derived type may have overridden Read(byte[], int, int) prior
                // to this Read(Span<byte>) overload being introduced.  In that case, this Read(Span<byte>) overload
                // should use the behavior of Read(byte[],int,int) overload.
                return base.Read(buffer);
            }

            if (_memoryMemoryStream is not null)
                return _memoryMemoryStream.Read(buffer);

            EnsureNotClosed();

            int n = Math.Min(_length - _position, buffer.Length);
            if (n <= 0)
                return 0;

            new Span<byte>(_buffer, _position, n).CopyTo(buffer);

            _position += n;
            return n;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            // If cancellation was requested, bail early
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            try
            {
                int n = Read(buffer, offset, count);
                return _lastReadTask.GetTask(n);
            }
            catch (OperationCanceledException oce)
            {
                return Task.FromCanceled<int>(oce);
            }
            catch (Exception exception)
            {
                return Task.FromException<int>(exception);
            }
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            try
            {
                // ReadAsync(Memory<byte>,...) needs to delegate to an existing virtual to do the work, in case an existing derived type
                // has changed or augmented the logic associated with reads.  If the Memory wraps an array, we could delegate to
                // ReadAsync(byte[], ...), but that would defeat part of the purpose, as ReadAsync(byte[], ...) often needs to allocate
                // a Task<int> for the return value, so we want to delegate to one of the synchronous methods.  We could always
                // delegate to the Read(Span<byte>) method, and that's the most efficient solution when dealing with a concrete
                // MemoryStream, but if we're dealing with a type derived from MemoryStream, Read(Span<byte>) will end up delegating
                // to Read(byte[], ...), which requires it to get a byte[] from ArrayPool and copy the data.  So, we special-case the
                // very common case of the Memory<byte> wrapping an array: if it does, we delegate to Read(byte[], ...) with it,
                // as that will be efficient in both cases, and we fall back to Read(Span<byte>) if the Memory<byte> wrapped something
                // else; if this is a concrete MemoryStream, that'll be efficient, and only in the case where the Memory<byte> wrapped
                // something other than an array and this is a MemoryStream-derived type that doesn't override Read(Span<byte>) will
                // it then fall back to doing the ArrayPool/copy behavior.
                return new ValueTask<int>(
                    MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> destinationArray) ?
                        Read(destinationArray.Array!, destinationArray.Offset, destinationArray.Count) :
                        Read(buffer.Span));
            }
            catch (OperationCanceledException oce)
            {
                return new ValueTask<int>(Task.FromCanceled<int>(oce));
            }
            catch (Exception exception)
            {
                return ValueTask.FromException<int>(exception);
            }
        }

        public override int ReadByte()
        {
            if (_memoryMemoryStream is not null)
                return _memoryMemoryStream.ReadByte();

            EnsureNotClosed();

            if (_position >= _length)
                return -1;

            return _buffer[_position++];
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overridden.
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (GetType() != typeof(MemoryStream))
            {
                base.CopyTo(destination, bufferSize);
                return;
            }

            // Validate the arguments the same way Stream does for back-compat.
            ValidateCopyToArguments(destination, bufferSize);

            if (_memoryMemoryStream is not null)
            {
                _memoryMemoryStream.CopyTo(destination);
                return;
            }

            EnsureNotClosed();

            int originalPosition = _position;

            // Seek to the end of the MemoryStream.
            int remaining = InternalEmulateRead(_length - originalPosition);

            // If we were already at or past the end, there's no copying to do so just quit.
            if (remaining > 0)
            {
                // Call Write() on the other Stream, using our internal buffer and avoiding any
                // intermediary allocations.
                destination.Write(_buffer, originalPosition, remaining);
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // This implementation offers better performance compared to the base class version.

            ValidateCopyToArguments(destination, bufferSize);

            if (_memoryMemoryStream is not null)
                return _memoryMemoryStream.CopyToAsync(destination, cancellationToken);

            EnsureNotClosed();

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to ReadAsync() which a subclass might have overridden.
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into ReadAsync) when we are not sure.
            if (GetType() != typeof(MemoryStream))
                return base.CopyToAsync(destination, bufferSize, cancellationToken);

            // If canceled - return fast:
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            // Avoid copying data from this buffer into a temp buffer:
            // (require that InternalEmulateRead does not throw,
            // otherwise it needs to be wrapped into try-catch-Task.FromException like memStrDest.Write below)

            int pos = _position;
            int n = InternalEmulateRead(_length - _position);

            // If we were already at or past the end, there's no copying to do so just quit.
            if (n == 0)
                return Task.CompletedTask;

            // If destination is not a memory stream, write there asynchronously:
            if (destination is not MemoryStream memStrDest)
                return destination.WriteAsync(_buffer, pos, n, cancellationToken);

            try
            {
                // If destination is a MemoryStream, CopyTo synchronously:
                memStrDest.Write(_buffer, pos, n);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (_memoryMemoryStream is not null)
                return _memoryMemoryStream.Seek(offset, loc);

            EnsureNotClosed();

            return SeekCore(offset, loc switch
            {
                SeekOrigin.Begin => _origin,
                SeekOrigin.Current => _position,
                SeekOrigin.End => _length,
                _ => throw new ArgumentException(SR.Argument_InvalidSeekOrigin)
            });
        }

        private long SeekCore(long offset, int loc)
        {
            if (offset > MemStreamMaxLength - loc)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.ArgumentOutOfRange_StreamLength, Array.MaxLength));
            int tempPosition = unchecked(loc + (int)offset);
            if (unchecked(loc + offset) < _origin || tempPosition < _origin)
                throw new IOException(SR.IO_SeekBeforeBegin);
            _position = tempPosition;

            Debug.Assert(_position >= _origin);
            return _position - _origin;
        }

        // Sets the length of the stream to a given value.  The new
        // value must be nonnegative and less than the space remaining in
        // the array, MemStreamMaxLength - origin
        // Origin is 0 in all cases other than a MemoryStream created on
        // top of an existing array and a specific starting offset was passed
        // into the MemoryStream constructor.  The upper bounds prevents any
        // situations where a stream may be created on top of an array then
        // the stream is made longer than the maximum possible length of the
        // array (MemStreamMaxLength).
        //
        public override void SetLength(long value)
        {
            if (_memoryMemoryStream is not null)
            {
                _memoryMemoryStream.SetLength(value);
                return;
            }

            if (value < 0 || value > MemStreamMaxLength)
                throw new ArgumentOutOfRangeException(nameof(value), SR.Format(SR.ArgumentOutOfRange_StreamLength, Array.MaxLength));

            EnsureWriteable();

            // Origin wasn't publicly exposed above.
            Debug.Assert(MemStreamMaxLength == Array.MaxLength);  // Check parameter validation logic in this method if this fails.
            if (value > (MemStreamMaxLength - _origin))
                throw new ArgumentOutOfRangeException(nameof(value), SR.Format(SR.ArgumentOutOfRange_StreamLength, Array.MaxLength));

            int newLength = _origin + (int)value;
            bool allocatedNewArray = EnsureCapacity(newLength);
            if (!allocatedNewArray && newLength > _length)
                Array.Clear(_buffer, _length, newLength - _length);
            _length = newLength;
            if (_position > newLength)
                _position = newLength;
        }

        public virtual byte[] ToArray()
        {
            if (_memoryMemoryStream is not null)
                return _memoryMemoryStream.ToArray();
            int count = _length - _origin;
            if (count == 0)
                return [];
            byte[] copy = GC.AllocateUninitializedArray<byte>(count);
            _buffer.AsSpan(_origin, count).CopyTo(copy);
            return copy;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (_memoryMemoryStream is not null)
            {
                _memoryMemoryStream.Write(new ReadOnlySpan<byte>(buffer, offset, count));
                return;
            }

            EnsureNotClosed();
            EnsureWriteable();

            int i = _position + count;
            // Check for overflow
            if (i < 0)
                throw new IOException(SR.IO_StreamTooLong);

            if (i > _length)
            {
                bool mustZero = _position > _length;
                if (i > _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray)
                    {
                        mustZero = false;
                    }
                }
                if (mustZero)
                {
                    Array.Clear(_buffer, _length, i - _length);
                }
                _length = i;
            }
            if ((count <= 8) && (buffer != _buffer))
            {
                int byteCount = count;
                while (--byteCount >= 0)
                {
                    _buffer[_position + byteCount] = buffer[offset + byteCount];
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            }
            _position = i;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() != typeof(MemoryStream))
            {
                // MemoryStream is not sealed, and a derived type may have overridden Write(byte[], int, int) prior
                // to this Write(Span<byte>) overload being introduced.  In that case, this Write(Span<byte>) overload
                // should use the behavior of Write(byte[],int,int) overload.
                base.Write(buffer);
                return;
            }

            if (_memoryMemoryStream is not null)
            {
                _memoryMemoryStream.Write(buffer);
                return;
            }

            EnsureNotClosed();
            EnsureWriteable();

            // Check for overflow
            int i = _position + buffer.Length;
            if (i < 0)
                throw new IOException(SR.IO_StreamTooLong);

            if (i > _length)
            {
                bool mustZero = _position > _length;
                if (i > _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray)
                    {
                        mustZero = false;
                    }
                }
                if (mustZero)
                {
                    Array.Clear(_buffer, _length, i - _length);
                }
                _length = i;
            }

            buffer.CopyTo(new Span<byte>(_buffer, _position, buffer.Length));
            _position = i;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            // If cancellation is already requested, bail early
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            try
            {
                Write(buffer, offset, count);
                return Task.CompletedTask;
            }
            catch (OperationCanceledException oce)
            {
                return Task.FromCanceled(oce);
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            try
            {
                // See corresponding comment in ReadAsync for why we don't just always use Write(ReadOnlySpan<byte>).
                // Unlike ReadAsync, we could delegate to WriteAsync(byte[], ...) here, but we don't for consistency.
                if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> sourceArray))
                {
                    Write(sourceArray.Array!, sourceArray.Offset, sourceArray.Count);
                }
                else
                {
                    Write(buffer.Span);
                }
                return default;
            }
            catch (OperationCanceledException oce)
            {
                return new ValueTask(Task.FromCanceled(oce));
            }
            catch (Exception exception)
            {
                return ValueTask.FromException(exception);
            }
        }

        public override void WriteByte(byte value)
        {
            if (_memoryMemoryStream is not null)
            {
                _memoryMemoryStream.WriteByte(value);
                return;
            }

            EnsureNotClosed();
            EnsureWriteable();

            if (_position >= _length)
            {
                int newLength = _position + 1;
                bool mustZero = _position > _length;
                if (newLength >= _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(newLength);
                    if (allocatedNewArray)
                    {
                        mustZero = false;
                    }
                }
                if (mustZero)
                {
                    Array.Clear(_buffer, _length, _position - _length);
                }
                _length = newLength;
            }
            _buffer[_position++] = value;
        }

        // Writes this MemoryStream to another stream.
        public virtual void WriteTo(Stream stream)
        {
            if (_memoryMemoryStream is not null)
            {
                _memoryMemoryStream.WriteTo(stream);
                return;
            }

            ArgumentNullException.ThrowIfNull(stream);

            EnsureNotClosed();

            stream.Write(_buffer, _origin, _length - _origin);
        }

        private sealed class MemoryMemoryStream
        {
            private readonly ReadOnlyMemory<byte> _memory;
            private readonly Memory<byte> _writableMemory;
            private int _position;
            private int _length;
            private bool _writable;
            private bool _isOpen;

            public MemoryMemoryStream(ReadOnlyMemory<byte> memory)
            {
                _memory = memory;
                _length = memory.Length;
                _isOpen = true;
            }

            public MemoryMemoryStream(Memory<byte> memory, bool writable)
            {
                _memory = memory;
                if (writable)
                {
                    _writableMemory = memory;
                }
                _writable = writable;
                _length = memory.Length;
                _isOpen = true;
            }

            public int Capacity
            {
                get
                {
                    EnsureNotClosed();
                    return _memory.Length;
                }
            }

            public void SetCapacity(int value)
            {
                if (value < _length)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_SmallCapacity);
                EnsureNotClosed();
                if (value != _memory.Length)
                    throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);
            }

            public long Length
            {
                get
                {
                    EnsureNotClosed();
                    return _length;
                }
            }

            public long Position
            {
                get
                {
                    EnsureNotClosed();
                    return _position;
                }
                set
                {
                    ArgumentOutOfRangeException.ThrowIfNegative(value);
                    EnsureNotClosed();
                    if (value > MemStreamMaxLength)
                        throw new ArgumentOutOfRangeException(nameof(value), SR.Format(SR.ArgumentOutOfRange_StreamLength, Array.MaxLength));
                    _position = (int)value;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureNotClosed()
            {
                if (!_isOpen)
                    ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
            }

            public int RemainingBytes
            {
                get
                {
                    EnsureNotClosed();
                    int n = _length - _position;
                    return n > 0 ? n : 0;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureWriteable()
            {
                if (!_writable)
                    ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            public int Read(Span<byte> buffer)
            {
                EnsureNotClosed();

                int n = Math.Min(_length - _position, buffer.Length);
                if (n <= 0)
                    return 0;

                _memory.Span.Slice(_position, n).CopyTo(buffer);
                _position += n;
                return n;
            }

            public int ReadByte()
            {
                EnsureNotClosed();

                if (_position >= _length)
                    return -1;

                return _memory.Span[_position++];
            }

            public void Write(ReadOnlySpan<byte> buffer)
            {
                EnsureNotClosed();
                EnsureWriteable();

                int i = _position + buffer.Length;
                if (i < 0)
                    throw new IOException(SR.IO_StreamTooLong);

                if (i > _memory.Length)
                    throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);

                if (i > _length)
                {
                    if (_position > _length)
                    {
                        _writableMemory.Span.Slice(_length, _position - _length).Clear();
                    }
                    _length = i;
                }

                buffer.CopyTo(_writableMemory.Span.Slice(_position));
                _position = i;
            }

            public void WriteByte(byte value)
            {
                EnsureNotClosed();
                EnsureWriteable();

                if (_position >= _length)
                {
                    int newLength = _position + 1;
                    if (newLength > _memory.Length)
                        throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);

                    if (_position > _length)
                    {
                        _writableMemory.Span.Slice(_length, _position - _length).Clear();
                    }
                    _length = newLength;
                }
                _writableMemory.Span[_position++] = value;
            }

            public long Seek(long offset, SeekOrigin loc)
            {
                EnsureNotClosed();

                long tempPosition = loc switch
                {
                    SeekOrigin.Begin => offset,
                    SeekOrigin.Current => _position + offset,
                    SeekOrigin.End => _length + offset,
                    _ => throw new ArgumentException(SR.Argument_InvalidSeekOrigin)
                };

                if (tempPosition < 0)
                    throw new IOException(SR.IO_SeekBeforeBegin);
                if (tempPosition > MemStreamMaxLength)
                    throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.ArgumentOutOfRange_StreamLength, Array.MaxLength));

                _position = (int)tempPosition;
                return _position;
            }

            public void SetLength(long value)
            {
                if (value < 0 || value > MemStreamMaxLength)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Format(SR.ArgumentOutOfRange_StreamLength, Array.MaxLength));

                EnsureWriteable();

                int newLength = (int)value;
                if (newLength > _memory.Length)
                    throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);

                if (newLength > _length)
                    _writableMemory.Span.Slice(_length, newLength - _length).Clear();

                _length = newLength;
                if (_position > newLength)
                    _position = newLength;
            }

            public byte[] ToArray()
            {
                if (_length == 0)
                    return [];
                byte[] copy = GC.AllocateUninitializedArray<byte>(_length);
                _memory.Span.Slice(0, _length).CopyTo(copy);
                return copy;
            }

            public void WriteTo(Stream stream)
            {
                ArgumentNullException.ThrowIfNull(stream);
                EnsureNotClosed();
                stream.Write(_memory.Span.Slice(0, _length));
            }

            public void CopyTo(Stream destination)
            {
                EnsureNotClosed();

                int remaining = _length - _position;
                if (remaining > 0)
                {
                    destination.Write(_memory.Span.Slice(_position, remaining));
                    _position = _length;
                }
            }

            public Task CopyToAsync(Stream destination, CancellationToken cancellationToken)
            {
                EnsureNotClosed();

                if (cancellationToken.IsCancellationRequested)
                    return Task.FromCanceled(cancellationToken);

                int pos = _position;
                int n = _length - _position;
                _position = _length;

                if (n == 0)
                    return Task.CompletedTask;

                return destination.WriteAsync(_memory.Slice(pos, n), cancellationToken).AsTask();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<byte> InternalReadSpan(int count)
            {
                EnsureNotClosed();

                int origPos = _position;
                int newPos = origPos + count;

                if ((uint)newPos > (uint)_length)
                {
                    _position = _length;
                    ThrowHelper.ThrowEndOfFileException();
                }

                var span = _memory.Span.Slice(origPos, count);
                _position = newPos;
                return span;
            }

            public int InternalEmulateRead(int count)
            {
                EnsureNotClosed();

                int n = _length - _position;
                if (n > count)
                    n = count;
                if (n < 0)
                    n = 0;

                _position += n;
                return n;
            }

            public void Dispose()
            {
                _isOpen = false;
                _writable = false;
            }
        }
    }
}
