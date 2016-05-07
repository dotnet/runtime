// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System.Security
{
    public sealed partial class SecureString
    {
        internal SecureString(SecureString str)
        {
            AllocateBuffer(str.EncryptedBufferLength);
            SafeBSTRHandle.Copy(str._encryptedBuffer, _encryptedBuffer, str.EncryptedBufferLength * sizeof(char));
            _decryptedLength = str._decryptedLength;
        }

        private unsafe void InitializeSecureString(char* value, int length)
        {
            if (length == 0)
            {
                AllocateBuffer(0);
                _decryptedLength = 0;
                return;
            }

            _encryptedBuffer = SafeBSTRHandle.Allocate(0);
            SafeBSTRHandle decryptedBuffer = SafeBSTRHandle.Allocate((uint)length);
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
                {
                    decryptedBuffer.ReleasePointer();
                }
            }

            ProtectMemory(decryptedBuffer);
        }

        private void AppendCharCore(char c)
        {
            SafeBSTRHandle decryptedBuffer = UnprotectMemory();
            try
            {
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
        private void ClearCore()
        {
            _decryptedLength = 0;
            _encryptedBuffer.ClearBuffer();
        }

        private void DisposeCore()
        {
            if (_encryptedBuffer != null && !_encryptedBuffer.IsInvalid)
            {
                _encryptedBuffer.Dispose();
                _encryptedBuffer = null;
            }
        }

        private unsafe void InsertAtCore(int index, char c)
        {
            byte* bufferPtr = null;
            SafeBSTRHandle decryptedBuffer = UnprotectMemory();
            try
            {
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
                if (bufferPtr != null)
                {
                    decryptedBuffer.ReleasePointer();
                }
                ProtectMemory(decryptedBuffer);
            }
        }

        private unsafe void RemoveAtCore(int index)
        {
            byte* bufferPtr = null;
            SafeBSTRHandle decryptedBuffer = UnprotectMemory();
            try
            {
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
                if (bufferPtr != null)
                {
                    decryptedBuffer.ReleasePointer();
                }
                ProtectMemory(decryptedBuffer);
            }
        }

        private void SetAtCore(int index, char c)
        {
            SafeBSTRHandle decryptedBuffer = UnprotectMemory();
            try
            {
                decryptedBuffer.Write<char>((uint)index * sizeof(char), c);
            }
            finally
            {
                ProtectMemory(decryptedBuffer);
            }
        }

        internal unsafe IntPtr MarshalToStringCore(bool globalAlloc, bool unicode)
        {
            int length = _decryptedLength;
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            byte* bufferPtr = null;

            SafeBSTRHandle decryptedBuffer = UnprotectMemory();
            try
            {
                decryptedBuffer.AcquirePointer(ref bufferPtr);
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
                    int resultByteLength = 1 + Interop.mincore.WideCharToMultiByte(
                        Interop.mincore.CP_ACP, Interop.mincore.WC_NO_BEST_FIT_CHARS, (char*)bufferPtr, length, null, 0, (IntPtr)(&defaultChar), IntPtr.Zero);
                    ptr = globalAlloc ? Marshal.AllocHGlobal(resultByteLength) : Marshal.AllocCoTaskMem(resultByteLength);
                    Interop.mincore.WideCharToMultiByte(
                        Interop.mincore.CP_ACP, Interop.mincore.WC_NO_BEST_FIT_CHARS, (char*)bufferPtr, length, (byte*)ptr, resultByteLength - 1, (IntPtr)(&defaultChar), IntPtr.Zero);
                    *(resultByteLength - 1 + (byte*)ptr) = 0;
                }
                result = ptr;
            }
            finally
            {
                // If we failed for any reason, free the new buffer
                if (result == IntPtr.Zero && ptr != IntPtr.Zero)
                {
                    Interop.NtDll.ZeroMemory(ptr, (UIntPtr)(length * sizeof(char)));
                    MarshalFree(ptr, globalAlloc);
                }

                if (bufferPtr != null)
                {
                    decryptedBuffer.ReleasePointer();
                }
            }
            return result;
        }

        private void EnsureNotDisposed()
        {
            if (_encryptedBuffer == null)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private SafeBSTRHandle _encryptedBuffer;

        private uint EncryptedBufferLength
        {
            get
            {
                Debug.Assert(_encryptedBuffer != null, "Buffer is not initialized!");
                return _encryptedBuffer.Length;
            }
        }

        private void AllocateBuffer(uint size)
        {
            _encryptedBuffer = SafeBSTRHandle.Allocate(size);
        }

        private void EnsureCapacity(ref SafeBSTRHandle decryptedBuffer, int capacity)
        {
            if (capacity > MaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_Capacity);
            }

            Debug.Assert(capacity > _decryptedLength, $"Expected to only be called when growing capacity = {capacity}, _decryptedLength = {_decryptedLength}");

            SafeBSTRHandle newBuffer = SafeBSTRHandle.Allocate((uint)capacity);
            SafeBSTRHandle.Copy(decryptedBuffer, newBuffer, (uint)_decryptedLength * sizeof(char));
            decryptedBuffer.Dispose();
            decryptedBuffer = newBuffer;
        }

        private void ProtectMemory(SafeBSTRHandle decryptedBuffer)
        {
            Debug.Assert(!decryptedBuffer.IsInvalid, "Invalid buffer!");

            if (_decryptedLength == 0)
            {
                return;
            }

            try
            {
                SafeBSTRHandle newEncryptedBuffer;
                if (!CryptProtectData(decryptedBuffer, out newEncryptedBuffer))
                {
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
                }

                _encryptedBuffer.Dispose();
                _encryptedBuffer = newEncryptedBuffer;
            }
            finally
            {
                decryptedBuffer.ClearBuffer();
                decryptedBuffer.Dispose();
            }
        }

        private SafeBSTRHandle UnprotectMemory()
        {
            Debug.Assert(!_encryptedBuffer.IsInvalid, "Invalid buffer!");

            if (_decryptedLength == 0)
            {
                return _encryptedBuffer;
            }

            SafeBSTRHandle decryptedBuffer;
            if (!CryptUnprotectData(_encryptedBuffer, out decryptedBuffer))
            {
                throw Marshal.GetLastWin32Error().ToCryptographicException();
            }

            return decryptedBuffer;
        }

        private static unsafe bool CryptProtectData(SafeBSTRHandle decryptedBuffer, out SafeBSTRHandle encryptedBuffer)
        {
            byte* uncryptedBufferPtr = null;
            Interop.Crypt32.DATA_BLOB pDataOut = default(Interop.Crypt32.DATA_BLOB);
            try
            {
                decryptedBuffer.AcquirePointer(ref uncryptedBufferPtr);

                Interop.Crypt32.DATA_BLOB optionalEntropyBlob = default(Interop.Crypt32.DATA_BLOB);
                Interop.Crypt32.DATA_BLOB pDataIn = new Interop.Crypt32.DATA_BLOB((IntPtr)uncryptedBufferPtr, decryptedBuffer.Length * sizeof(char));
                if (Interop.Crypt32.CryptProtectData(ref pDataIn, string.Empty, ref optionalEntropyBlob, IntPtr.Zero, IntPtr.Zero, 0, out pDataOut))
                {
                    encryptedBuffer = SafeBSTRHandle.Allocate(pDataOut.pbData, pDataOut.cbData);
                    return true;
                }
                else
                {
                    encryptedBuffer = null;
                    return false;
                }
            }
            finally
            {
                if (uncryptedBufferPtr != null)
                {
                    decryptedBuffer.ReleasePointer();
                }

                if (pDataOut.pbData != IntPtr.Zero)
                {
                    Interop.NtDll.ZeroMemory(pDataOut.pbData, (UIntPtr)pDataOut.cbData);
                    Marshal.FreeHGlobal(pDataOut.pbData);
                }
            }
        }

        internal static unsafe bool CryptUnprotectData(SafeBSTRHandle encryptedBuffer, out SafeBSTRHandle decryptedBuffer)
        {
            byte* cryptedBufferPtr = null;
            Interop.Crypt32.DATA_BLOB pDataOut = default(Interop.Crypt32.DATA_BLOB);
            try
            {
                encryptedBuffer.AcquirePointer(ref cryptedBufferPtr);

                Interop.Crypt32.DATA_BLOB optionalEntropyBlob = default(Interop.Crypt32.DATA_BLOB);
                Interop.Crypt32.DATA_BLOB pDataIn = new Interop.Crypt32.DATA_BLOB((IntPtr)cryptedBufferPtr, encryptedBuffer.Length * sizeof(char));
                if (Interop.Crypt32.CryptUnprotectData(ref pDataIn, IntPtr.Zero, ref optionalEntropyBlob, IntPtr.Zero, IntPtr.Zero, 0, out pDataOut))
                {
                    decryptedBuffer = SafeBSTRHandle.Allocate(pDataOut.pbData, pDataOut.cbData);
                    return true;
                }
                else
                {
                    decryptedBuffer = null;
                    return false;
                }
            }
            finally
            {
                if (cryptedBufferPtr != null)
                {
                    encryptedBuffer.ReleasePointer();
                }

                if (pDataOut.pbData != IntPtr.Zero)
                {
                    Interop.NtDll.ZeroMemory(pDataOut.pbData, (UIntPtr)pDataOut.cbData);
                    Marshal.FreeHGlobal(pDataOut.pbData);
                }
            }
        }
    }
}
