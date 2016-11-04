// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.WindowsRuntime.Internal;
using System.Runtime;
using System.Security;
using System.Threading;

namespace System.IO
{
    [Flags]
    internal enum FileAccess
    {
        // Specifies read access to the file. Data can be read from the file and
        // the file pointer can be moved. Combine with WRITE for read-write access.
        Read = 1,

        // Specifies write access to the file. Data can be written to the file and
        // the file pointer can be moved. Combine with READ for read-write access.
        Write = 2,

        // Specifies read and write access to the file. Data can be written to the
        // file and the file pointer can be moved. Data can also be read from the
        // file.
        ReadWrite = 3,
    }

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
     * call free, run a user-provided delegate to free the memory, etc etc.
     * We'll suggest user write a subclass of UnmanagedMemoryStream that uses
     * a SafeHandle subclass to hold onto the memory.
     * Check for problems when using this in the negative parts of a
     * process's address space.  We may need to use unsigned longs internally
     * and change the overflow detection logic.
     */
    internal class UnmanagedMemoryStream : Stream
    {
        private unsafe byte* _mem;
        private long _length;
        private long _capacity;
        private long _position;
        private FileAccess _access;
        internal bool _isOpen;

        // Needed for subclasses that need to map a file, etc.
        protected UnmanagedMemoryStream()
        {
            unsafe
            {
                _mem = null;
            }
            _isOpen = false;
        }

        public unsafe UnmanagedMemoryStream(byte* pointer, long length)
        {
            Initialize(pointer, length, length, FileAccess.Read, false);
        }

        public unsafe UnmanagedMemoryStream(byte* pointer, long length, long capacity, FileAccess access)
        {
            Initialize(pointer, length, capacity, access, false);
        }

        // We must create one of these without doing a security check.  This
        // class is created while security is trying to start up.  Plus, doing
        // a Demand from Assembly.GetManifestResourceStream isn't useful.
        internal unsafe UnmanagedMemoryStream(byte* pointer, long length, long capacity, FileAccess access, bool skipSecurityCheck)
        {
            Initialize(pointer, length, capacity, access, skipSecurityCheck);
        }

        protected unsafe void Initialize(byte* pointer, long length, long capacity, FileAccess access)
        {
            Initialize(pointer, length, capacity, access, false);
        }

        internal unsafe void Initialize(byte* pointer, long length, long capacity, FileAccess access, bool skipSecurityCheck)
        {
            if (pointer == null)
                throw new ArgumentNullException(nameof(pointer));
            if (length < 0 || capacity < 0)
                throw new ArgumentOutOfRangeException((length < 0) ? nameof(length): nameof(capacity), SR.ArgumentOutOfRange_NeedNonNegNum);
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
            _length = length;
            _capacity = capacity;
            _access = access;
            _isOpen = true;
        }

        public override bool CanRead
        {
            [Pure]
            get
            { return _isOpen && (_access & FileAccess.Read) != 0; }
        }

        public override bool CanSeek
        {
            [Pure]
            get
            { return _isOpen; }
        }

        public override bool CanWrite
        {
            [Pure]
            get
            { return _isOpen && (_access & FileAccess.Write) != 0; }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
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
            if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
        }

        public override long Length
        {
            get
            {
                if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
                return Interlocked.Read(ref _length);
            }
        }

        public long Capacity
        {
            get
            {
                if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
                return _capacity;
            }
        }

        public override long Position
        {
            get
            {
                if (!CanSeek) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
                Contract.EndContractBlock();
                return Interlocked.Read(ref _position);
            }
            [System.Security.SecuritySafeCritical]  // auto-generated
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedNonNegNum);
                Contract.EndContractBlock();
                if (!CanSeek) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);

#if WIN32
                unsafe
                {
                    // On 32 bit machines, ensure we don't wrap around.
                    if (value > (long)Int32.MaxValue || _mem + value < _mem)
                        throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_StreamLength);
                }
#endif
                Interlocked.Exchange(ref _position, value);
            }
        }

        public unsafe byte* PositionPointer
        {
            get
            {
                // Use a temp to avoid a race
                long pos = Interlocked.Read(ref _position);
                if (pos > _capacity)
                    throw new IndexOutOfRangeException(SR.IndexOutOfRange_UMSPosition);
                byte* ptr = _mem + pos;
                if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
                return ptr;
            }
            set
            {
                if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);

                if (value < _mem)
                    throw new IOException(SR.IO_SeekBeforeBegin);

                Interlocked.Exchange(ref _position, value - _mem);
            }
        }

        internal unsafe byte* Pointer
        {
            get
            {
                return _mem;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
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
            Contract.EndContractBlock();

            if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
            if (!CanRead) throw new NotSupportedException(SR.NotSupported_UnreadableStream);

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
            Debug.Assert(pos + nInt >= 0, "_position + n >= 0");  // len is less than 2^63 -1.

            unsafe
            {
                Marshal.Copy((IntPtr)(_mem + pos), buffer, offset, nInt);
            }
            Interlocked.Exchange(ref _position, pos + n);
            return nInt;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override int ReadByte()
        {
            if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
            if (!CanRead) throw new NotSupportedException(SR.NotSupported_UnreadableStream);

            long pos = Interlocked.Read(ref _position);  // Use a local to avoid a race condition
            long len = Interlocked.Read(ref _length);
            if (pos >= len)
                return -1;
            Interlocked.Exchange(ref _position, pos + 1);
            int result;
            unsafe
            {
                result = _mem[pos];
            }
            return result;
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_NeedNonNegNum);
            Contract.EndContractBlock();
            if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
            if (!CanWrite) throw new NotSupportedException(SR.NotSupported_UnwritableStream);

            if (value > _capacity)
                throw new IOException(SR.IO_FixedCapacity);

            long pos = Interlocked.Read(ref _position);
            long len = Interlocked.Read(ref _length);
            if (value > len)
            {
                unsafe
                {
                    Helpers.ZeroMemory(_mem + len, value - len);
                }
            }
            Interlocked.Exchange(ref _length, value);
            if (pos > value)
            {
                Interlocked.Exchange(ref _position, value);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
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
            Contract.EndContractBlock();

            if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
            if (!CanWrite) throw new NotSupportedException(SR.NotSupported_UnwritableStream);

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

            // Check to see whether we are now expanding the stream and must
            // zero any memory in the middle.
            if (pos > len)
            {
                unsafe
                {
                    Helpers.ZeroMemory(_mem + len, pos - len);
                }
            }

            // set length after zeroing memory to avoid race condition of accessing unzeroed memory
            if (n > len)
            {
                Interlocked.Exchange(ref _length, n);
            }

            unsafe
            {
                Marshal.Copy(buffer, offset, (IntPtr)(_mem + pos), count);
            }

            Interlocked.Exchange(ref _position, n);
            return;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void WriteByte(byte value)
        {
            if (!_isOpen) throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
            if (!CanWrite) throw new NotSupportedException(SR.NotSupported_UnwritableStream);

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
                if (pos > len)
                {
                    unsafe
                    {
                        Helpers.ZeroMemory(_mem + len, pos - len);
                    }
                }

                // set length after zeroing memory to avoid race condition of accessing unzeroed memory
                Interlocked.Exchange(ref _length, n);
            }

            unsafe
            {
                _mem[pos] = value;
            }
            Interlocked.Exchange(ref _position, n);
        }
    }
}
