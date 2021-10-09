// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Security
{
    public sealed partial class SecureString : IDisposable
    {
        private const int MaxLength = 65536;
        private readonly object _methodLock = new object();
        private UnmanagedBuffer? _buffer;
        private int _decryptedLength;
        private bool _encrypted;
        private bool _readOnly;

        public SecureString()
        {
            Initialize(ReadOnlySpan<char>.Empty);
        }

        [CLSCompliant(false)]
        public unsafe SecureString(char* value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (length > MaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_Length);
            }

            Initialize(new ReadOnlySpan<char>(value, length));
        }

        private void Initialize(ReadOnlySpan<char> value)
        {
            _buffer = UnmanagedBuffer.Allocate(GetAlignedByteSize(value.Length));
            _decryptedLength = value.Length;

            SafeBuffer? bufferToRelease = null;
            try
            {
                Span<char> span = AcquireSpan(ref bufferToRelease);
                value.CopyTo(span);
            }
            finally
            {
                ProtectMemory();
                bufferToRelease?.DangerousRelease();
            }
        }

        private SecureString(SecureString str)
        {
            Debug.Assert(str._buffer != null, "Expected other SecureString's buffer to be non-null");
            Debug.Assert(str._encrypted, "Expected to be used only on encrypted SecureStrings");

            _buffer = UnmanagedBuffer.Allocate((int)str._buffer.ByteLength);
            Debug.Assert(_buffer != null);
            UnmanagedBuffer.Copy(str._buffer, _buffer, str._buffer.ByteLength);

            _decryptedLength = str._decryptedLength;
            _encrypted = str._encrypted;
        }

        public int Length
        {
            get
            {
                EnsureNotDisposed();
                return Volatile.Read(ref _decryptedLength);
            }
        }

        private void EnsureCapacity(int capacity)
        {
            if (capacity > MaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_Capacity);
            }

            Debug.Assert(_buffer != null);
            if ((uint)capacity * sizeof(char) <= _buffer.ByteLength)
            {
                return;
            }

            UnmanagedBuffer oldBuffer = _buffer;
            UnmanagedBuffer newBuffer = UnmanagedBuffer.Allocate(GetAlignedByteSize(capacity));
            UnmanagedBuffer.Copy(oldBuffer, newBuffer, (uint)_decryptedLength * sizeof(char));
            _buffer = newBuffer;
            oldBuffer.Dispose();
        }

        public void AppendChar(char c)
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();

                Debug.Assert(_buffer != null);

                SafeBuffer? bufferToRelease = null;

                try
                {
                    UnprotectMemory();

                    EnsureCapacity(_decryptedLength + 1);

                    Span<char> span = AcquireSpan(ref bufferToRelease);
                    span[_decryptedLength] = c;
                    _decryptedLength++;
                }
                finally
                {
                    ProtectMemory();
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        // clears the current contents. Only available if writable
        public void Clear()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();

                Debug.Assert(_buffer != null);

                _decryptedLength = 0;

                SafeBuffer? bufferToRelease = null;
                try
                {
                    Span<char> span = AcquireSpan(ref bufferToRelease);
                    span.Clear();
                }
                finally
                {
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        // Do a deep-copy of the SecureString
        public SecureString Copy()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                return new SecureString(this);
            }
        }

        public void Dispose()
        {
            lock (_methodLock)
            {
                if (_buffer != null)
                {
                    _buffer.Dispose();
                    _buffer = null;
                }
            }
        }

        public void InsertAt(int index, char c)
        {
            lock (_methodLock)
            {
                if (index < 0 || index > _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexString);
                }

                EnsureNotDisposed();
                EnsureNotReadOnly();

                Debug.Assert(_buffer != null);

                SafeBuffer? bufferToRelease = null;

                try
                {
                    UnprotectMemory();

                    EnsureCapacity(_decryptedLength + 1);

                    Span<char> span = AcquireSpan(ref bufferToRelease);
                    span.Slice(index, _decryptedLength - index).CopyTo(span.Slice(index + 1));
                    span[index] = c;
                    _decryptedLength++;
                }
                finally
                {
                    ProtectMemory();
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        public bool IsReadOnly()
        {
            EnsureNotDisposed();
            return Volatile.Read(ref _readOnly);
        }

        public void MakeReadOnly()
        {
            EnsureNotDisposed();
            Volatile.Write(ref _readOnly, true);
        }

        public void RemoveAt(int index)
        {
            lock (_methodLock)
            {
                if (index < 0 || index >= _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexString);
                }

                EnsureNotDisposed();
                EnsureNotReadOnly();

                Debug.Assert(_buffer != null);

                SafeBuffer? bufferToRelease = null;

                try
                {
                    UnprotectMemory();

                    Span<char> span = AcquireSpan(ref bufferToRelease);
                    span.Slice(index + 1, _decryptedLength - (index + 1)).CopyTo(span.Slice(index));
                    _decryptedLength--;
                }
                finally
                {
                    ProtectMemory();
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        public void SetAt(int index, char c)
        {
            lock (_methodLock)
            {
                if (index < 0 || index >= _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexString);
                }

                EnsureNotDisposed();
                EnsureNotReadOnly();

                Debug.Assert(_buffer != null);

                SafeBuffer? bufferToRelease = null;

                try
                {
                    UnprotectMemory();

                    Span<char> span = AcquireSpan(ref bufferToRelease);
                    span[index] = c;
                }
                finally
                {
                    ProtectMemory();
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        private unsafe Span<char> AcquireSpan(ref SafeBuffer? bufferToRelease)
        {
            SafeBuffer buffer = _buffer!;

            bool ignore = false;
            buffer.DangerousAddRef(ref ignore);

            bufferToRelease = buffer;

            return new Span<char>((byte*)buffer.DangerousGetHandle(), (int)(buffer.ByteLength / 2));
        }

        private void EnsureNotReadOnly()
        {
            if (_readOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        private void EnsureNotDisposed()
        {
            if (_buffer == null)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        internal unsafe IntPtr MarshalToBSTR()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();

                UnprotectMemory();

                SafeBuffer? bufferToRelease = null;
                IntPtr ptr = IntPtr.Zero;
                int length = 0;
                try
                {
                    Span<char> span = AcquireSpan(ref bufferToRelease);

                    length = _decryptedLength;
                    ptr = Marshal.AllocBSTR(length);
                    span.Slice(0, length).CopyTo(new Span<char>((void*)ptr, length));

                    IntPtr result = ptr;
                    ptr = IntPtr.Zero;
                    return result;
                }
                finally
                {
                    // If we failed for any reason, free the new buffer
                    if (ptr != IntPtr.Zero)
                    {
                        new Span<char>((void*)ptr, length).Clear();
                        Marshal.FreeBSTR(ptr);
                    }

                    ProtectMemory();
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        internal unsafe IntPtr MarshalToString(bool globalAlloc, bool unicode)
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();

                UnprotectMemory();

                SafeBuffer? bufferToRelease = null;
                IntPtr ptr = IntPtr.Zero;
                int byteLength = 0;
                try
                {
                    Span<char> span = AcquireSpan(ref bufferToRelease).Slice(0, _decryptedLength);

                    if (unicode)
                    {
                        byteLength = (span.Length + 1) * sizeof(char);
                    }
                    else
                    {
                        byteLength = Marshal.GetAnsiStringByteCount(span);
                    }

                    if (globalAlloc)
                    {
                        ptr = Marshal.AllocHGlobal(byteLength);
                    }
                    else
                    {
                        ptr = Marshal.AllocCoTaskMem(byteLength);
                    }

                    if (unicode)
                    {
                        Span<char> resultSpan = new Span<char>((void*)ptr, byteLength / sizeof(char));
                        span.CopyTo(resultSpan);
                        resultSpan[resultSpan.Length - 1] = '\0';
                    }
                    else
                    {
                        Marshal.GetAnsiStringBytes(span, new Span<byte>((void*)ptr, byteLength));
                    }

                    IntPtr result = ptr;
                    ptr = IntPtr.Zero;
                    return result;
                }
                finally
                {
                    // If we failed for any reason, free the new buffer
                    if (ptr != IntPtr.Zero)
                    {
                        new Span<byte>((void*)ptr, byteLength).Clear();

                        if (globalAlloc)
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                        else
                        {
                            Marshal.FreeCoTaskMem(ptr);
                        }
                    }

                    ProtectMemory();
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        /// <summary>SafeBuffer for managing memory meant to be kept confidential.</summary>
        private sealed class UnmanagedBuffer : SafeBuffer
        {
            // A local copy of byte length to be able to access it in ReleaseHandle without the risk of throwing exceptions
            private int _byteLength;

#pragma warning disable CA1419 // not intended for use with P/Invoke
            private UnmanagedBuffer() : base(true) { }
#pragma warning restore CA1419

            public static UnmanagedBuffer Allocate(int byteLength)
            {
                Debug.Assert(byteLength >= 0);
                UnmanagedBuffer buffer = new UnmanagedBuffer();
                buffer.SetHandle(Marshal.AllocHGlobal(byteLength));
                buffer.Initialize((ulong)byteLength);
                buffer._byteLength = byteLength;
                return buffer;
            }

            internal static unsafe void Copy(UnmanagedBuffer source, UnmanagedBuffer destination, ulong bytesLength)
            {
                if (bytesLength == 0)
                {
                    return;
                }

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
                    {
                        destination.ReleasePointer();
                    }
                    if (srcPtr != null)
                    {
                        source.ReleasePointer();
                    }
                }
            }

            protected override unsafe bool ReleaseHandle()
            {
                new Span<byte>((void*)handle, _byteLength).Clear();
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }
    }
}
