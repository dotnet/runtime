// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace System.Security
{
    public sealed partial class SecureString
    {
        internal SecureString(SecureString str)
        {
            Debug.Assert(str != null, "Expected non-null SecureString");
            Debug.Assert(str._buffer != null, "Expected other SecureString's buffer to be non-null");
            Debug.Assert(str._encrypted, "Expected to be used only on encrypted SecureStrings");

            AllocateBuffer(str._buffer.Length);
            SafeBSTRHandle.Copy(str._buffer, _buffer, str._buffer.Length * sizeof(char));

            _decryptedLength = str._decryptedLength;
            _encrypted = str._encrypted;
        }

        private unsafe void InitializeSecureString(char* value, int length)
        {
            Debug.Assert(length >= 0, $"Expected non-negative length, got {length}");

            AllocateBuffer((uint)length);
            _decryptedLength = length;

            byte* bufferPtr = null;
            try
            {
                _buffer.AcquirePointer(ref bufferPtr);
                Buffer.MemoryCopy((byte*)value, bufferPtr, (long)_buffer.ByteLength, length * sizeof(char));
            }
            finally
            {
                if (bufferPtr != null)
                {
                    _buffer.ReleasePointer();
                }
            }

            ProtectMemory();
        }

        private void AppendCharCore(char c)
        {
            UnprotectMemory();
            try
            {
                EnsureCapacity(_decryptedLength + 1);
                _buffer.Write<char>((uint)_decryptedLength * sizeof(char), c);
                _decryptedLength++;
            }
            finally
            {
                ProtectMemory();
            }
        }

        private void ClearCore()
        {
            _decryptedLength = 0;
            _buffer.ClearBuffer();
        }

        private void DisposeCore()
        {
            if (_buffer != null)
            {
                _buffer.Dispose();
                _buffer = null;
            }
        }

        private unsafe void InsertAtCore(int index, char c)
        {
            byte* bufferPtr = null;
            UnprotectMemory();
            try
            {
                EnsureCapacity(_decryptedLength + 1);

                _buffer.AcquirePointer(ref bufferPtr);
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
                ProtectMemory();
                if (bufferPtr != null)
                {
                    _buffer.ReleasePointer();
                }
            }
        }

        private unsafe void RemoveAtCore(int index)
        {
            byte* bufferPtr = null;
            UnprotectMemory();
            try
            {
                _buffer.AcquirePointer(ref bufferPtr);
                char* pBuffer = (char*)bufferPtr;

                for (int i = index; i < _decryptedLength - 1; i++)
                {
                    pBuffer[i] = pBuffer[i + 1];
                }
                pBuffer[--_decryptedLength] = (char)0;
            }
            finally
            {
                ProtectMemory();
                if (bufferPtr != null)
                {
                    _buffer.ReleasePointer();
                }
            }
        }

        private void SetAtCore(int index, char c)
        {
            UnprotectMemory();
            try
            {
                _buffer.Write<char>((uint)index * sizeof(char), c);
            }
            finally
            {
                ProtectMemory();
            }
        }

        internal unsafe IntPtr MarshalToBSTR()
        {
            int length = _decryptedLength;
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            byte* bufferPtr = null;

            UnprotectMemory();
            try
            {
                _buffer.AcquirePointer(ref bufferPtr);
                int resultByteLength = (length + 1) * sizeof(char);

                ptr = PInvokeMarshal.AllocBSTR(length);

                Buffer.MemoryCopy(bufferPtr, (byte*)ptr, resultByteLength, length * sizeof(char));

                result = ptr;
            }
            finally
            {
                ProtectMemory();

                // If we failed for any reason, free the new buffer
                if (result == IntPtr.Zero && ptr != IntPtr.Zero)
                {
                    RuntimeImports.RhZeroMemory(ptr, (UIntPtr)(length * sizeof(char)));
                    PInvokeMarshal.FreeBSTR(ptr);
                }

                if (bufferPtr != null)
                {
                    _buffer.ReleasePointer();
                }
            }
            return result;
        }

        internal unsafe IntPtr MarshalToStringCore(bool globalAlloc, bool unicode)
        {
            int length = _decryptedLength;
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            byte* bufferPtr = null;

            UnprotectMemory();
            try
            {
                _buffer.AcquirePointer(ref bufferPtr);
                if (unicode)
                {
                    int resultByteLength = (length + 1) * sizeof(char);
                    ptr = globalAlloc ? Marshal.AllocHGlobal(resultByteLength) : Marshal.AllocCoTaskMem(resultByteLength);
                    Buffer.MemoryCopy(bufferPtr, (byte*)ptr, resultByteLength, length * sizeof(char));
                    *(length + (char*)ptr) = '\0';
                }
                else
                {
                    uint defaultChar = '?';
                    int resultByteLength = 1 + Interop.Kernel32.WideCharToMultiByte(
                        Interop.Kernel32.CP_ACP, Interop.Kernel32.WC_NO_BEST_FIT_CHARS, (char*)bufferPtr, length, null, 0, (IntPtr)(&defaultChar), IntPtr.Zero);
                    ptr = globalAlloc ? Marshal.AllocHGlobal(resultByteLength) : Marshal.AllocCoTaskMem(resultByteLength);
                    Interop.Kernel32.WideCharToMultiByte(
                        Interop.Kernel32.CP_ACP, Interop.Kernel32.WC_NO_BEST_FIT_CHARS, (char*)bufferPtr, length, (byte*)ptr, resultByteLength - 1, (IntPtr)(&defaultChar), IntPtr.Zero);
                    *(resultByteLength - 1 + (byte*)ptr) = 0;
                }
                result = ptr;
            }
            finally
            {
                ProtectMemory();

                // If we failed for any reason, free the new buffer
                if (result == IntPtr.Zero && ptr != IntPtr.Zero)
                {
                    RuntimeImports.RhZeroMemory(ptr, (UIntPtr)(length * sizeof(char)));
                    MarshalFree(ptr, globalAlloc);
                }

                if (bufferPtr != null)
                {
                    _buffer.ReleasePointer();
                }
            }
            return result;
        }

        private void EnsureNotDisposed()
        {
            if (_buffer == null)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private const int BlockSize = (int)Interop.Crypt32.CRYPTPROTECTMEMORY_BLOCK_SIZE / sizeof(char);
        private SafeBSTRHandle _buffer;
        private bool _encrypted;

        private void AllocateBuffer(uint size)
        {
            _buffer = SafeBSTRHandle.Allocate(GetAlignedSize(size));
        }

        private static uint GetAlignedSize(uint size) =>
            size == 0 || size % BlockSize != 0 ?
                BlockSize + ((size / BlockSize) * BlockSize) :
                size;

        private void EnsureCapacity(int capacity)
        {
            if (capacity > MaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_Capacity);
            }

            if (((uint)capacity * sizeof(char)) <= _buffer.ByteLength)
            {
                return;
            }

            var oldBuffer = _buffer;
            SafeBSTRHandle newBuffer = SafeBSTRHandle.Allocate(GetAlignedSize((uint)capacity));
            SafeBSTRHandle.Copy(oldBuffer, newBuffer, (uint)_decryptedLength * sizeof(char));
            _buffer = newBuffer;
            oldBuffer.Dispose();
        }

        private void ProtectMemory()
        {
            Debug.Assert(!_buffer.IsInvalid, "Invalid buffer!");

            if (_decryptedLength != 0 &&
                !_encrypted &&
                !Interop.Crypt32.CryptProtectMemory(_buffer, _buffer.Length * sizeof(char), Interop.Crypt32.CRYPTPROTECTMEMORY_SAME_PROCESS))
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            _encrypted = true;
        }

        private void UnprotectMemory()
        {
            Debug.Assert(!_buffer.IsInvalid, "Invalid buffer!");

            if (_decryptedLength != 0 &&
                _encrypted &&
                !Interop.Crypt32.CryptUnprotectMemory(_buffer, _buffer.Length * sizeof(char), Interop.Crypt32.CRYPTPROTECTMEMORY_SAME_PROCESS))
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            _encrypted = false;
        }
    }
}
