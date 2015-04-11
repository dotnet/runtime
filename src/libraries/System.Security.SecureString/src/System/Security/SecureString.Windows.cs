// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System.Security
{
    public sealed partial class SecureString
    {
        [System.Security.SecurityCritical]  // auto-generated
        internal SecureString(SecureString str)
        {
            AllocateBuffer(str.EncryptedBufferLength);
            SafeBSTRHandle.Copy(str._encryptedBuffer, _encryptedBuffer);
            _decryptedLength = str._decryptedLength;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe void InitializeSecureString(char* value, int length)
        {
            if (length == 0)
            {
                AllocateBuffer(0);
                _decryptedLength = 0;
                return;
            }

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

        [System.Security.SecuritySafeCritical]  // auto-generated
        private void AppendCharCore(char c)
        {
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

        // clears the current contents. Only available if writable
        [System.Security.SecuritySafeCritical]  // auto-generated
        private void ClearCore()
        {
            _decryptedLength = 0;
            _encryptedBuffer.ClearBuffer();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private void DisposeCore()
        {
            if (_encryptedBuffer != null && !_encryptedBuffer.IsInvalid)
            {
                _encryptedBuffer.Dispose();
                _encryptedBuffer = null;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe void InsertAtCore(int index, char c)
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe void RemoveAtCore(int index)
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        private void SetAtCore(int index, char c)
        {
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

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe IntPtr ToUniStrCore()
        {
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

        [System.Security.SecurityCritical]  // auto-generated
        private void EnsureNotDisposed()
        {
            if (_encryptedBuffer == null)
            {
                throw new ObjectDisposedException(null);
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        [System.Security.SecurityCritical] // auto-generated
        private SafeBSTRHandle _encryptedBuffer;

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
