// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security
{
    // TODO: Issue #1387.
    // This implementation lacks encryption. We need to investigate adding such encryption support, at which point
    // we could potentially remove the current implementation's reliance on mlock and mprotect (mlock places additional 
    // constraints on non-privileged processes due to RLIMIT_MEMLOCK), neither of which provides a guarantee that the
    // data-at-rest in memory can't be accessed; they just make it more difficult.  If we don't encrypt, at least on Linux
    // we should consider also using madvise to set MADV_DONTDUMP and MADV_DONTFORK for the allocated pages.  And we
    // should ensure the documentation gets updated appropriately.

    public sealed partial class SecureString
    {
        private ProtectedBuffer _buffer;

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe SecureString(SecureString str)
        {
            // Allocate enough space to store the provided string
            EnsureCapacity(str._decryptedLength);
            _decryptedLength = str._decryptedLength;

            // Copy the string into the newly allocated space
            if (_decryptedLength > 0)
                using (str._buffer.Unprotect())
                    ProtectedBuffer.Copy(str._buffer, _buffer, (ulong)(str._decryptedLength * sizeof(char)));

            // Protect the buffer
            _buffer.Protect();
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe void InitializeSecureString(char* value, int length)
        {
            // Allocate enough space to store the provided string
            EnsureCapacity(length);
            _decryptedLength = length;
            if (length == 0)
                return;

            // Copy the string into the newly allocated space
            byte* ptr = null;
            try
            {
                _buffer.AcquirePointer(ref ptr);
                Buffer.MemoryCopy(value, ptr, _buffer.ByteLength, (ulong)(length * sizeof(char)));
            }
            finally
            {
                if (ptr != null)
                    _buffer.ReleasePointer();
            }

            // Protect the buffer
            _buffer.Protect();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private void DisposeCore()
        {
            if (_buffer != null && !_buffer.IsInvalid)
            {
                _buffer.Dispose();
                _buffer = null;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void EnsureNotDisposed()
        {
            if (_buffer == null)
                throw new ObjectDisposedException(GetType().Name);
        }

        // clears the current contents. Only available if writable
        [System.Security.SecuritySafeCritical]  // auto-generated
        private void ClearCore()
        {
            _decryptedLength = 0;
            using (_buffer.Unprotect())
                _buffer.Clear();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe void AppendCharCore(char c)
        {
            // Make sure we have enough space for the new character,
            // then write it at the end.
            EnsureCapacity(_decryptedLength + 1);
            using (_buffer.Unprotect())
                _buffer.Write((ulong)(_decryptedLength * sizeof(char)), c);
            _decryptedLength++;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe void InsertAtCore(int index, char c)
        {
            // Make sure we have enough space for the new character,
            // then shift all of the characters above it and insert it.
            EnsureCapacity(_decryptedLength + 1);
            byte* ptr = null;
            try
            {
                _buffer.AcquirePointer(ref ptr);
                char* charPtr = (char*)ptr;
                using (_buffer.Unprotect())
                {
                    for (int i = _decryptedLength; i > index; i--)
                        charPtr[i] = charPtr[i - 1];
                    charPtr[index] = c;
                }
                ++_decryptedLength;
            }
            finally
            {
                if (ptr != null)
                    _buffer.ReleasePointer();
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe void RemoveAtCore(int index)
        {
            // Shift down all values above the specified index,
            // then null out the empty space at the end.
            byte* ptr = null;
            try
            {
                _buffer.AcquirePointer(ref ptr);
                char* charPtr = (char*)ptr;
                using (_buffer.Unprotect())
                {
                    for (int i = index; i < _decryptedLength - 1; i++)
                        charPtr[i] = charPtr[i + 1];
                    charPtr[--_decryptedLength] = (char)0;
                }
            }
            finally
            {
                if (ptr != null)
                    _buffer.ReleasePointer();
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private void SetAtCore(int index, char c)
        {
            // Overwrite the character at the specified index
            using (_buffer.Unprotect())
                _buffer.Write((ulong)(index * sizeof(char)), c);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe IntPtr ToUniStrCore()
        {
            int length = _decryptedLength;

            byte* bufferPtr = null;
            IntPtr stringPtr = IntPtr.Zero, result = IntPtr.Zero;
            try
            {
                // Allocate space for the string to be returned, including space for a null terminator
                stringPtr = Marshal.AllocCoTaskMem((length + 1) * sizeof(char));

                _buffer.AcquirePointer(ref bufferPtr);

                // Copy all of our data into it
                using (_buffer.Unprotect())
                    Buffer.MemoryCopy(
                        source: bufferPtr,
                        destination: (byte*)stringPtr.ToPointer(),
                        destinationSizeInBytes: ((length + 1) * sizeof(char)),
                        sourceBytesToCopy: length * sizeof(char));

                // Add the null termination
                *(length + (char*)stringPtr.ToPointer()) = '\0';

                // Finally store the string pointer into our result.  We maintain
                // a separate result variable to make clean up in the finally easier.
                result = stringPtr;
            }
            finally
            {
                // If there was a failure, such that result isn't initialized, 
                // release the string if we had one.
                if (stringPtr != IntPtr.Zero && result == IntPtr.Zero)
                {
                    ProtectedBuffer.ZeroMemory((byte*)stringPtr, (ulong)(length * sizeof(char)));
                    Marshal.FreeCoTaskMem(stringPtr);
                }

                if (bufferPtr != null)
                    _buffer.ReleasePointer();
            }

            return result;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private void EnsureCapacity(int capacity)
        {
            // Make sure the requested capacity doesn't exceed SecureString's defined limit
            if (capacity > MaxLength)
                throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_Capacity);

            // If we already have enough space allocated, we're done
            if (_buffer != null && (capacity * sizeof(char)) <= (int)_buffer.ByteLength)
                return;

            // We need more space, so allocate a new buffer, copy all our data into it,
            // and then swap the new for the old.
            ProtectedBuffer newBuffer = ProtectedBuffer.Allocate(capacity * sizeof(char));
            if (_buffer != null)
            {
                using (_buffer.Unprotect())
                    ProtectedBuffer.Copy(_buffer, newBuffer, _buffer.ByteLength);
                newBuffer.Protect();
                _buffer.Dispose();
            }
            _buffer = newBuffer;
        }

        /// <summary>SafeBuffer for managing memory meant to be kept confidential.</summary>
        private sealed class ProtectedBuffer : SafeBuffer
        {
            private static readonly int s_pageSize = 
                Interop.libc.sysconf(Interop.libc.SysConfNames._SC_PAGESIZE);

            internal ProtectedBuffer() : base(true) { }

            internal static ProtectedBuffer Allocate(int bytes)
            {
                Debug.Assert(bytes >= 0);

                // Round the number of bytes up to the next page size boundary.  mmap
                // is going to allocate pages, anyway, and we lock/protect entire pages,
                // so we might as well benefit from being able to use all of that space,
                // rather than allocating it and having it be unusable.  As a SecureString
                // grows, this will significantly help in avoiding unnecessary recreations
                // of the buffer.
                Debug.Assert(s_pageSize > 0);
                bytes = RoundUpToPageSize(bytes);
                Debug.Assert(bytes % s_pageSize == 0);

                ProtectedBuffer buffer = new ProtectedBuffer();
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    // Allocate the page(s) for the buffer.
                    ptr = Interop.libc.mmap(
                        IntPtr.Zero,
                        (IntPtr)bytes,
                        Interop.libc.MemoryMappedProtections.PROT_READ | Interop.libc.MemoryMappedProtections.PROT_WRITE,
                        Interop.libc.MemoryMappedFlags.MAP_ANONYMOUS | Interop.libc.MemoryMappedFlags.MAP_PRIVATE, 0,
                        0);
                    if (ptr == IntPtr.Zero)
                        throw CreateExceptionFromErrno();

                    // Lock the pages into memory to minimize the chances that the pages get
                    // swapped out, making the contents available on disk.
                    if (Interop.libc.mlock(ptr, (IntPtr)bytes) != 0)
                        throw CreateExceptionFromErrno();
                }
                catch
                {
                    // Something failed; release the allocation
                    if (ptr != IntPtr.Zero)
                        Interop.libc.munmap(ptr, (IntPtr)bytes); // ignore any errors
                    throw;
                }

                // The memory was allocated; initialize the buffer with it.
                buffer.SetHandle(ptr);
                buffer.Initialize((ulong)bytes);
                return buffer;
            }

            internal void Protect()
            {
                // Make the pages unreadable/writable; attempts to read/write this memory will result in seg faults.
                ChangeProtection(Interop.libc.MemoryMappedProtections.PROT_NONE);
            }

            internal ProtectOnDispose Unprotect()
            {
                // Make the pages readable/writable; attempts to read/write this memory will succeed.
                // Then return a disposable that will re-protect the memory when done with it.
                ChangeProtection(Interop.libc.MemoryMappedProtections.PROT_READ | Interop.libc.MemoryMappedProtections.PROT_WRITE);
                return new ProtectOnDispose(this);
            }

            internal struct ProtectOnDispose : IDisposable
            {
                private readonly ProtectedBuffer _buffer;

                internal ProtectOnDispose(ProtectedBuffer buffer)
                {
                    Debug.Assert(buffer != null);
                    _buffer = buffer;
                }

                public void Dispose()
                {
                    _buffer.Protect();
                }
            }

            private unsafe void ChangeProtection(Interop.libc.MemoryMappedProtections prots)
            {
                byte* ptr = null;
                try
                {
                    AcquirePointer(ref ptr);
                    if (Interop.libc.mprotect((IntPtr)ptr, (IntPtr)ByteLength, prots) != 0)
                        throw CreateExceptionFromErrno();
                }
                finally
                {
                    if (ptr != null)
                        ReleasePointer();
                }
            }

            internal unsafe void Clear()
            {
                byte* ptr = null;
                try
                {
                    AcquirePointer(ref ptr);
                    ZeroMemory(ptr, ByteLength);
                }
                finally
                {
                    if (ptr != null)
                        ReleasePointer();
                }
            }

            internal static unsafe void Copy(ProtectedBuffer source, ProtectedBuffer destination, ulong bytesLength)
            {
                if (bytesLength == 0)
                    return;

                byte* srcPtr = null, dstPtr = null;
                try
                {
                    source.AcquirePointer(ref srcPtr);
                    destination.AcquirePointer(ref dstPtr);
                    Buffer.MemoryCopy(srcPtr, dstPtr, destination.ByteLength, bytesLength);
                }
                finally
                {
                    if (dstPtr != null)
                        destination.ReleasePointer();
                    if (srcPtr != null)
                        source.ReleasePointer();
                }
            }

            protected override unsafe bool ReleaseHandle()
            {
                bool success = true;

                IntPtr h = handle;
                if (h != IntPtr.Zero)
                {
                    IntPtr len = (IntPtr)ByteLength;
                    success &= Interop.libc.mprotect(h, len, 
                        Interop.libc.MemoryMappedProtections.PROT_READ | Interop.libc.MemoryMappedProtections.PROT_WRITE) == 0;
                    if (success)
                    {
                        ZeroMemory((byte*)h, ByteLength);
                        success &= (Interop.libc.munlock(h, len) == 0);
                    }
                    success &= (Interop.libc.munmap(h, len) == 0);
                }

                return success;
            }

            internal static unsafe void ZeroMemory(byte* ptr, ulong len)
            {
                for (ulong i = 0; i < len; i++)
                    *ptr++ = 0;
            }

            private static Exception CreateExceptionFromErrno()
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                return (errorInfo.Error == Interop.Error.ENOMEM || errorInfo.Error == Interop.Error.EPERM) ?
                    (Exception)new OutOfMemoryException(SR.OutOfMemory_MemoryResourceLimits) :
                    (Exception)new InvalidOperationException(errorInfo.GetErrorMessage());
            }

            private static int RoundUpToPageSize(int bytes)
            {
                return bytes > 0 ?
                    (bytes + (s_pageSize - 1)) & ~(s_pageSize - 1) :
                    s_pageSize;
            }
        }

    }
}
