// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System.Security
{
    public sealed class SecureString : IDisposable
    {
        [System.Security.SecurityCritical] // auto-generated
        private SafeBSTRHandle _encryptedBuffer;

        private int _decryptedLength;
        private bool _readOnly;

        private const int MaxLength = 65536;

        private readonly object _methodLock = new object();

        [System.Security.SecuritySafeCritical]  // auto-generated
        static SecureString()
        {
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal SecureString(SecureString str)
        {
            AllocateBuffer(str.EncryptedBufferLength);
            SafeBSTRHandle.Copy(str._encryptedBuffer, _encryptedBuffer);
            _decryptedLength = str._decryptedLength;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public SecureString()
        {
            AllocateBuffer(0);
            _decryptedLength = 0;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe void InitializeSecureString(char* value, int length)
        {
            _encryptedBuffer = SafeBSTRHandle.Allocate(null, 0);
            SafeBSTRHandle decryptedBuffer = SafeBSTRHandle.Allocate(null, (uint)length);
            _decryptedLength = length;

            byte* bufferPtr = null;
            try
            {
                decryptedBuffer.AcquirePointer(ref bufferPtr);
                Buffer.MemoryCopy((byte*)value, bufferPtr, decryptedBuffer.Length * sizeof(char), length * sizeof(char));
            }
            finally
            {
                if (bufferPtr != null)
                    decryptedBuffer.ReleasePointer();
            }

            ProtectMemory(decryptedBuffer);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public unsafe SecureString(char* value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (length > MaxLength)
            {
                throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_Length);
            }

            // Refactored since HandleProcessCorruptedStateExceptionsAttribute applies to methods only (yet).
            InitializeSecureString(value, length);
        }

        public int Length
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                lock (_methodLock)
                {
                    EnsureNotDisposed();
                    return _decryptedLength;
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void AppendChar(char c)
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();

                SafeBSTRHandle decryptedBuffer = null;
                try
                {
                    decryptedBuffer = UnProtectMemory();
                    EnsureCapacity(ref decryptedBuffer, _decryptedLength + 1);
                    decryptedBuffer.Write<char>((uint)_decryptedLength * sizeof(char), c);
                    _decryptedLength++;
                }
                finally
                {
                    ProtectMemory(decryptedBuffer);
                }
            }
        }

        // clears the current contents. Only available if writable
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Clear()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();

                _decryptedLength = 0;
                _encryptedBuffer.ClearBuffer();
            }
        }

        // Do a deep-copy of the SecureString 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public SecureString Copy()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                return new SecureString(this);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Dispose()
        {
            lock (_methodLock)
            {
                if (_encryptedBuffer != null && !_encryptedBuffer.IsInvalid)
                {
                    _encryptedBuffer.Dispose();
                    _encryptedBuffer = null;
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void InsertAt(int index, char c)
        {
            lock (_methodLock)
            {
                if (index < 0 || index > _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexString);
                }

                EnsureNotDisposed();
                EnsureNotReadOnly();

                unsafe
                {
                    byte* bufferPtr = null;
                    SafeBSTRHandle decryptedBuffer = null;
                    try
                    {
                        decryptedBuffer = UnProtectMemory();
                        EnsureCapacity(ref decryptedBuffer, _decryptedLength + 1);

                        decryptedBuffer.AcquirePointer(ref bufferPtr);
                        char* pBuffer = (char*)bufferPtr;

                        for (int i = _decryptedLength; i > index; i--)
                        {
                            pBuffer[i] = pBuffer[i - 1];
                        }
                        pBuffer[index] = c;
                        ++_decryptedLength;
                    }
                    finally
                    {
                        ProtectMemory(decryptedBuffer);
                        if (bufferPtr != null)
                            decryptedBuffer.ReleasePointer();
                    }
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public bool IsReadOnly()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                return _readOnly;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void MakeReadOnly()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                _readOnly = true;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void RemoveAt(int index)
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();

                if (index < 0 || index >= _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexString);
                }

                unsafe
                {
                    byte* bufferPtr = null;
                    SafeBSTRHandle decryptedBuffer = null;
                    try
                    {
                        decryptedBuffer = UnProtectMemory();
                        decryptedBuffer.AcquirePointer(ref bufferPtr);
                        char* pBuffer = (char*)bufferPtr;

                        for (int i = index; i < _decryptedLength - 1; i++)
                        {
                            pBuffer[i] = pBuffer[i + 1];
                        }
                        pBuffer[--_decryptedLength] = (char)0;
                    }
                    finally
                    {
                        ProtectMemory(decryptedBuffer);
                        if (bufferPtr != null)
                            decryptedBuffer.ReleasePointer();
                    }
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetAt(int index, char c)
        {
            lock (_methodLock)
            {
                if (index < 0 || index >= _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexString);
                }
                Debug.Assert(index <= Int32.MaxValue / sizeof(char));

                EnsureNotDisposed();
                EnsureNotReadOnly();

                SafeBSTRHandle decryptedBuffer = null;
                try
                {
                    decryptedBuffer = UnProtectMemory();
                    decryptedBuffer.Write<char>((uint)index * sizeof(char), c);
                }
                finally
                {
                    ProtectMemory(decryptedBuffer);
                }
            }
        }

        private uint EncryptedBufferLength
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                Debug.Assert(_encryptedBuffer != null, "Buffer is not initialized!");
                return _encryptedBuffer.Length;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void AllocateBuffer(uint size)
        {
            _encryptedBuffer = SafeBSTRHandle.Allocate(null, size);
            if (_encryptedBuffer.IsInvalid)
            {
                throw new OutOfMemoryException();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void EnsureCapacity(ref SafeBSTRHandle decryptedBuffer, int capacity)
        {
            if (capacity > MaxLength)
            {
                throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_Capacity);
            }

            if (capacity <= _decryptedLength)
            {
                return;
            }

            SafeBSTRHandle newBuffer = SafeBSTRHandle.Allocate(null, (uint)capacity);

            if (newBuffer.IsInvalid)
            {
                throw new OutOfMemoryException();
            }

            SafeBSTRHandle.Copy(decryptedBuffer, newBuffer);
            decryptedBuffer.Dispose();
            decryptedBuffer = newBuffer;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void EnsureNotDisposed()
        {
            if (_encryptedBuffer == null)
            {
                throw new ObjectDisposedException(null);
            }
        }

        private void EnsureNotReadOnly()
        {
            if (_readOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void ProtectMemory(SafeBSTRHandle decryptedBuffer)
        {
            Debug.Assert(!decryptedBuffer.IsInvalid, "Invalid buffer!");

            if (_decryptedLength == 0)
            {
                return;
            }

            try
            {
                SafeBSTRHandle newEncryptedBuffer = null;
                if (Interop.Crypt32.CryptProtectData(decryptedBuffer, out newEncryptedBuffer))
                {
                    _encryptedBuffer.Dispose();
                    _encryptedBuffer = newEncryptedBuffer;
                }
                else
                {
                    throw new CryptographicException(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                decryptedBuffer.ClearBuffer();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe IntPtr ToUniStr()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                int length = _decryptedLength;
                IntPtr ptr = IntPtr.Zero;
                IntPtr result = IntPtr.Zero;
                byte* bufferPtr = null;

                SafeBSTRHandle decryptedBuffer = null;
                try
                {
                    ptr = Marshal.AllocCoTaskMem((length + 1) * 2);

                    if (ptr == IntPtr.Zero)
                    {
                        throw new OutOfMemoryException();
                    }

                    decryptedBuffer = UnProtectMemory();
                    decryptedBuffer.AcquirePointer(ref bufferPtr);
                    Buffer.MemoryCopy(bufferPtr, (byte*)ptr.ToPointer(), ((length + 1) * 2), length * 2);
                    char* endptr = (char*)ptr.ToPointer();
                    *(endptr + length) = '\0';
                    result = ptr;
                }
                finally
                {
                    if (result == IntPtr.Zero)
                    {
                        // If we failed for any reason, free the new buffer
                        if (ptr != IntPtr.Zero)
                        {
                            Interop.NtDll.ZeroMemory(ptr, (UIntPtr)(length * 2));
                            Marshal.FreeCoTaskMem(ptr);
                        }
                    }

                    if (bufferPtr != null)
                        decryptedBuffer.ReleasePointer();
                }
                return result;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private SafeBSTRHandle UnProtectMemory()
        {
            Debug.Assert(!_encryptedBuffer.IsInvalid, "Invalid buffer!");

            SafeBSTRHandle decryptedBuffer = null;
            if (_decryptedLength == 0)
            {
                return _encryptedBuffer;
            }

            if (!Interop.Crypt32.CryptUnProtectData(_encryptedBuffer, out decryptedBuffer))
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            return decryptedBuffer;
        }
    }
}
