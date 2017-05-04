// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** 
**
** Purpose: Create a stream over unmanaged memory, mostly
**          useful for memory-mapped files.
**
**
===========================================================*/

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;


namespace System.IO
{
    /*
     * This class is used to access a contiguous block of memory, likely outside 
     * the GC heap (or pinned in place in the GC heap, but a MemoryStream may 
     * make more sense in those cases).  It's great if you have a pointer and
     * a length for a section of memory mapped in by someone else and you don't
     * want to copy this into the GC heap.  UnmanagedMemoryStream assumes these 
     * two things:
     *
     * 1) All the memory in the specified block is readable or writable,
     *    depending on the values you pass to the constructor.
     * 2) The lifetime of the block of memory is at least as long as the lifetime
     *    of the UnmanagedMemoryStream.
     * 3) You clean up the memory when appropriate.  The UnmanagedMemoryStream 
     *    currently will do NOTHING to free this memory.
     * 4) All calls to Write and WriteByte may not be threadsafe currently.
     *
     * It may become necessary to add in some sort of 
     * DeallocationMode enum, specifying whether we unmap a section of memory, 
     * call free, run a user-provided delegate to free the memory, etc.  
     * We'll suggest user write a subclass of UnmanagedMemoryStream that uses
     * a SafeHandle subclass to hold onto the memory.
     * Check for problems when using this in the negative parts of a 
     * process's address space.  We may need to use unsigned longs internally
     * and change the overflow detection logic.
     * 
     * -----SECURITY MODEL AND SILVERLIGHT-----
     * A few key notes about exposing UMS in silverlight:
     * 1. No ctors are exposed to transparent code. This version of UMS only
     * supports byte* (not SafeBuffer). Therefore, framework code can create
     * a UMS and hand it to transparent code. Transparent code can use most
     * operations on a UMS, but not operations that directly expose a 
     * pointer.
     * 
     * 2. Scope of "unsafe" and non-CLS compliant operations reduced: The
     * Whidbey version of this class has CLSCompliant(false) at the class 
     * level and unsafe modifiers at the method level. These were reduced to 
     * only where the unsafe operation is performed -- i.e. immediately 
     * around the pointer manipulation. Note that this brings UMS in line 
     * with recent changes in pu/clr to support SafeBuffer.
     * 
     * 3. Currently, the only caller that creates a UMS is ResourceManager, 
     * which creates read-only UMSs, and therefore operations that can 
     * change the length will throw because write isn't supported. A 
     * conservative option would be to formalize the concept that _only_
     * read-only UMSs can be creates, and enforce this by making WriteX and
     * SetLength SecurityCritical. However, this is a violation of 
     * security inheritance rules, so we must keep these safe. The 
     * following notes make this acceptable for future use.
     *    a. a race condition in WriteX that could have allowed a thread to 
     *    read from unzeroed memory was fixed
     *    b. memory region cannot be expanded beyond _capacity; in other 
     *    words, a UMS creator is saying a writable UMS is safe to 
     *    write to anywhere in the memory range up to _capacity, specified
     *    in the ctor. Even if the caller doesn't specify a capacity, then
     *    length is used as the capacity.
     */
    public class UnmanagedMemoryStream : Stream
    {
        private SafeBuffer _buffer;
        private unsafe byte* _mem;
        private long _length;
        private long _capacity;
        private long _position;
        private long _offset;
        private FileAccess _access;
        internal bool _isOpen;
        [NonSerialized]
        private Task<Int32> _lastReadTask; // The last successful task returned from ReadAsync 


        // Needed for subclasses that need to map a file, etc.
        protected UnmanagedMemoryStream()
        {
            unsafe
            {
                _mem = null;
            }
            _isOpen = false;
        }

        public UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length)
        {
            Initialize(buffer, offset, length, FileAccess.Read);
        }

        public UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length, FileAccess access)
        {
            Initialize(buffer, offset, length, access);
        }

        protected void Initialize(SafeBuffer buffer, long offset, long length, FileAccess access)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.ByteLength < (ulong)(offset + length))
            {
                throw new ArgumentException(SR.Argument_InvalidSafeBufferOffLen);
            }
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            {
                throw new ArgumentOutOfRangeException(nameof(access));
            }
            Contract.EndContractBlock();

            if (_isOpen)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CalledTwice);
            }

            // check for wraparound
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    buffer.AcquirePointer(ref pointer);
                    if ((pointer + offset + length) < pointer)
                    {
                        throw new ArgumentException(SR.ArgumentOutOfRange_UnmanagedMemStreamWrapAround);
                    }
                }
                finally
                {
                    if (pointer != null)
                    {
                        buffer.ReleasePointer();
                    }
                }
            }

            _offset = offset;
            _buffer = buffer;
            _length = length;
            _capacity = length;
            _access = access;
            _isOpen = true;
        }

        [CLSCompliant(false)]
        public unsafe UnmanagedMemoryStream(byte* pointer, long length)
        {
            Initialize(pointer, length, length, FileAccess.Read);
        }

        [CLSCompliant(false)]
        public unsafe UnmanagedMemoryStream(byte* pointer, long length, long capacity, FileAccess access)
        {
            Initialize(pointer, length, capacity, access);
        }

        [CLSCompliant(false)]
        protected unsafe void Initialize(byte* pointer, long length, long capacity, FileAccess access)
        {
            if (pointer == null)
                throw new ArgumentNullException(nameof(pointer));
            if (length < 0 || capacity < 0)
                throw new ArgumentOutOfRangeException((length < 0) ? nameof(length) : nameof(capacity), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (length > capacity)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_LengthGreaterThanCapacity);
            Contract.EndContractBlock();
            // Check for wraparound.
            if (((byte*)((long)pointer + capacity)) < pointer)
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_UnmanagedMemStreamWrapAround);
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
                throw new ArgumentOutOfRangeException(nameof(access), SR.ArgumentOutOfRange_Enum);
            if (_isOpen)
                throw new InvalidOperationException(SR.InvalidOperation_CalledTwice);

            _mem = pointer;
            _offset = 0;
            _length = length;
            _capacity = capacity;
            _access = access;
            _isOpen = true;
        }

        public override bool CanRead
        {
            [Pure]
            get { return _isOpen && (_access & FileAccess.Read) != 0; }
        }

        public override bool CanSeek
        {
            [Pure]
            get { return _isOpen; }
        }

        public override bool CanWrite
        {
            [Pure]
            get { return _isOpen && (_access & FileAccess.Write) != 0; }
        }

        protected override void Dispose(bool disposing)
        {
            _isOpen = false;
            unsafe { _mem = null; }

            // Stream allocates WaitHandles for async calls. So for correctness 
            // call base.Dispose(disposing) for better perf, avoiding waiting
            // for the finalizers to run on those types.
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            if (!_isOpen) __Error.StreamIsClosed();
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


        public override long Length
        {
            get
            {
                if (!_isOpen) __Error.StreamIsClosed();
                return Interlocked.Read(ref _length);
            }
        }

        public long Capacity
        {
            get
            {
                if (!_isOpen) __Error.StreamIsClosed();
                return _capacity;
            }
        }

        public override long Position
        {
            get
            {
                if (!CanSeek) __Error.StreamIsClosed();
                Contract.EndContractBlock();
                return Interlocked.Read(ref _position);
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedNonNegNum);
                Contract.EndContractBlock();
                if (!CanSeek) __Error.StreamIsClosed();

                Interlocked.Exchange(ref _position, value);
            }
        }

        [CLSCompliant(false)]
        public unsafe byte* PositionPointer
        {
            get
            {
                if (_buffer != null)
                {
                    throw new NotSupportedException(SR.NotSupported_UmsSafeBuffer);
                }

                // Use a temp to avoid a race
                long pos = Interlocked.Read(ref _position);
                if (pos > _capacity)
                    throw new IndexOutOfRangeException(SR.IndexOutOfRange_UMSPosition);
                byte* ptr = _mem + pos;
                if (!_isOpen) __Error.StreamIsClosed();
                return ptr;
            }
            set
            {
                if (_buffer != null)
                    throw new NotSupportedException(SR.NotSupported_UmsSafeBuffer);
                if (!_isOpen) __Error.StreamIsClosed();

                if (value < _mem)
                    throw new IOException(SR.IO_SeekBeforeBegin);
                long newPosition = (long)value - (long)_mem;
                if (newPosition < 0)
                    throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_UnmanagedMemStreamLength);


                Interlocked.Exchange(ref _position, newPosition);
            }
        }

        internal unsafe byte* Pointer
        {
            get
            {
                if (_buffer != null)
                    throw new NotSupportedException(SR.NotSupported_UmsSafeBuffer);

                return _mem;
            }
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();  // Keep this in sync with contract validation in ReadAsync

            if (!_isOpen) __Error.StreamIsClosed();
            if (!CanRead) __Error.ReadNotSupported();

            // Use a local variable to avoid a race where another thread 
            // changes our position after we decide we can read some bytes.
            long pos = Interlocked.Read(ref _position);
            long len = Interlocked.Read(ref _length);
            long n = len - pos;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;

            int nInt = (int)n; // Safe because n <= count, which is an Int32
            if (nInt < 0)
                return 0;  // _position could be beyond EOF
            Debug.Assert(pos + nInt >= 0, "_position + n >= 0");  // len is less than 2^63 -1.

            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    if (_buffer != null)
                    {
                        byte* pointer = null;

                        RuntimeHelpers.PrepareConstrainedRegions();
                        try
                        {
                            _buffer.AcquirePointer(ref pointer);
                            Buffer.Memcpy(pBuffer + offset, pointer + pos + _offset, nInt);
                        }
                        finally
                        {
                            if (pointer != null)
                            {
                                _buffer.ReleasePointer();
                            }
                        }
                    }
                    else
                    {
                        Buffer.Memcpy(pBuffer + offset, _mem + pos, nInt);
                    }
                }
            }
            Interlocked.Exchange(ref _position, pos + n);
            return nInt;
        }

        public override Task<Int32> ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();  // contract validation copied from Read(...) 

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<Int32>(cancellationToken);

            try
            {
                Int32 n = Read(buffer, offset, count);
                Task<Int32> t = _lastReadTask;
                return (t != null && t.Result == n) ? t : (_lastReadTask = Task.FromResult<Int32>(n));
            }
            catch (Exception ex)
            {
                Debug.Assert(!(ex is OperationCanceledException));
                return Task.FromException<Int32>(ex);
            }
        }

        public override int ReadByte()
        {
            if (!_isOpen) __Error.StreamIsClosed();
            if (!CanRead) __Error.ReadNotSupported();

            long pos = Interlocked.Read(ref _position);  // Use a local to avoid a race condition
            long len = Interlocked.Read(ref _length);
            if (pos >= len)
                return -1;
            Interlocked.Exchange(ref _position, pos + 1);
            int result;
            if (_buffer != null)
            {
                unsafe
                {
                    byte* pointer = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        _buffer.AcquirePointer(ref pointer);
                        result = *(pointer + pos + _offset);
                    }
                    finally
                    {
                        if (pointer != null)
                        {
                            _buffer.ReleasePointer();
                        }
                    }
                }
            }
            else
            {
                unsafe
                {
                    result = _mem[pos];
                }
            }
            return result;
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (!_isOpen) __Error.StreamIsClosed();
            switch (loc)
            {
                case SeekOrigin.Begin:
                    if (offset < 0)
                        throw new IOException(SR.IO_SeekBeforeBegin);
                    Interlocked.Exchange(ref _position, offset);
                    break;

                case SeekOrigin.Current:
                    long pos = Interlocked.Read(ref _position);
                    if (offset + pos < 0)
                        throw new IOException(SR.IO_SeekBeforeBegin);
                    Interlocked.Exchange(ref _position, offset + pos);
                    break;

                case SeekOrigin.End:
                    long len = Interlocked.Read(ref _length);
                    if (len + offset < 0)
                        throw new IOException(SR.IO_SeekBeforeBegin);
                    Interlocked.Exchange(ref _position, len + offset);
                    break;

                default:
                    throw new ArgumentException(SR.Argument_InvalidSeekOrigin);
            }

            long finalPos = Interlocked.Read(ref _position);
            Debug.Assert(finalPos >= 0, "_position >= 0");
            return finalPos;
        }

        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_NeedNonNegNum);
            Contract.EndContractBlock();
            if (_buffer != null)
                throw new NotSupportedException(SR.NotSupported_UmsSafeBuffer);
            if (!_isOpen) __Error.StreamIsClosed();
            if (!CanWrite) __Error.WriteNotSupported();

            if (value > _capacity)
                throw new IOException(SR.IO_FixedCapacity);

            long pos = Interlocked.Read(ref _position);
            long len = Interlocked.Read(ref _length);
            if (value > len)
            {
                unsafe
                {
                    Buffer.ZeroMemory(_mem + len, value - len);
                }
            }
            Interlocked.Exchange(ref _length, value);
            if (pos > value)
            {
                Interlocked.Exchange(ref _position, value);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();  // Keep contract validation in sync with WriteAsync(..)

            if (!_isOpen) __Error.StreamIsClosed();
            if (!CanWrite) __Error.WriteNotSupported();

            long pos = Interlocked.Read(ref _position);  // Use a local to avoid a race condition
            long len = Interlocked.Read(ref _length);
            long n = pos + count;
            // Check for overflow
            if (n < 0)
                throw new IOException(SR.IO_StreamTooLong);

            if (n > _capacity)
            {
                throw new NotSupportedException(SR.IO_FixedCapacity);
            }

            if (_buffer == null)
            {
                // Check to see whether we are now expanding the stream and must 
                // zero any memory in the middle.
                if (pos > len)
                {
                    unsafe
                    {
                        Buffer.ZeroMemory(_mem + len, pos - len);
                    }
                }

                // set length after zeroing memory to avoid race condition of accessing unzeroed memory
                if (n > len)
                {
                    Interlocked.Exchange(ref _length, n);
                }
            }

            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    if (_buffer != null)
                    {
                        long bytesLeft = _capacity - pos;
                        if (bytesLeft < count)
                        {
                            throw new ArgumentException(SR.Arg_BufferTooSmall);
                        }

                        byte* pointer = null;
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try
                        {
                            _buffer.AcquirePointer(ref pointer);
                            Buffer.Memcpy(pointer + pos + _offset, pBuffer + offset, count);
                        }
                        finally
                        {
                            if (pointer != null)
                            {
                                _buffer.ReleasePointer();
                            }
                        }
                    }
                    else
                    {
                        Buffer.Memcpy(_mem + pos, pBuffer + offset, count);
                    }
                }
            }
            Interlocked.Exchange(ref _position, n);
            return;
        }

        public override Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();  // contract validation copied from Write(..) 

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            try
            {
                Write(buffer, offset, count);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.Assert(!(ex is OperationCanceledException));
                return Task.FromException<Int32>(ex);
            }
        }


        public override void WriteByte(byte value)
        {
            if (!_isOpen) __Error.StreamIsClosed();
            if (!CanWrite) __Error.WriteNotSupported();

            long pos = Interlocked.Read(ref _position);  // Use a local to avoid a race condition
            long len = Interlocked.Read(ref _length);
            long n = pos + 1;
            if (pos >= len)
            {
                // Check for overflow
                if (n < 0)
                    throw new IOException(SR.IO_StreamTooLong);

                if (n > _capacity)
                    throw new NotSupportedException(SR.IO_FixedCapacity);

                // Check to see whether we are now expanding the stream and must 
                // zero any memory in the middle.
                // don't do if created from SafeBuffer
                if (_buffer == null)
                {
                    if (pos > len)
                    {
                        unsafe
                        {
                            Buffer.ZeroMemory(_mem + len, pos - len);
                        }
                    }

                    // set length after zeroing memory to avoid race condition of accessing unzeroed memory
                    Interlocked.Exchange(ref _length, n);
                }
            }

            if (_buffer != null)
            {
                unsafe
                {
                    byte* pointer = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        _buffer.AcquirePointer(ref pointer);
                        *(pointer + pos + _offset) = value;
                    }
                    finally
                    {
                        if (pointer != null)
                        {
                            _buffer.ReleasePointer();
                        }
                    }
                }
            }
            else
            {
                unsafe
                {
                    _mem[pos] = value;
                }
            }
            Interlocked.Exchange(ref _position, n);
        }
    }
}
