// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Security
{
    // SecureString attempts to provide a defense-in-depth solution.
    // 
    // On Windows, this is done with several mechanisms:
    // 1. keeping the data in unmanaged memory so that copies of it aren't implicitly made by the GC moving it around
    // 2. zero'ing out that unmanaged memory so that the string is reliably removed from memory when done with it
    // 3. encrypting the data while it's not being used (it's unencrypted to manipulate and use it)
    // 
    // On Unix, we do 1 and 2, but we don't do 3 as there's no CryptProtectData equivalent.

    public sealed partial class SecureString
    {
        private UnmanagedBuffer _buffer;

        internal SecureString(SecureString str)
        {
            // Allocate enough space to store the provided string
            EnsureCapacity(str._decryptedLength);
            _decryptedLength = str._decryptedLength;

            // Copy the string into the newly allocated space
            if (_decryptedLength > 0)
            {
                UnmanagedBuffer.Copy(str._buffer, _buffer, (ulong)(str._decryptedLength * sizeof(char)));
            }
        }

        private unsafe void InitializeSecureString(char* value, int length)
        {
            // Allocate enough space to store the provided string
            EnsureCapacity(length);
            _decryptedLength = length;
            if (length == 0)
            {
                return;
            }

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
                {
                    _buffer.ReleasePointer();
                }
            }
        }

        private void DisposeCore()
        {
            if (_buffer != null && !_buffer.IsInvalid)
            {
                _buffer.Dispose();
                _buffer = null;
            }
        }

        private void EnsureNotDisposed()
        {
            if (_buffer == null)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ClearCore()
        {
            _decryptedLength = 0;
            _buffer.Clear();
        }

        private unsafe void AppendCharCore(char c)
        {
            // Make sure we have enough space for the new character, then write it at the end.
            EnsureCapacity(_decryptedLength + 1);
            _buffer.Write((ulong)(_decryptedLength * sizeof(char)), c);
            _decryptedLength++;
        }

        private unsafe void InsertAtCore(int index, char c)
        {
            // Make sure we have enough space for the new character, then shift all of the characters above it and insert it.
            EnsureCapacity(_decryptedLength + 1);
            byte* ptr = null;
            try
            {
                _buffer.AcquirePointer(ref ptr);
                ptr += index * sizeof(char);
                long bytesToShift = (_decryptedLength - index) * sizeof(char);
                Buffer.MemoryCopy(ptr, ptr + sizeof(char), bytesToShift, bytesToShift);
                *((char*)ptr) = c;
                ++_decryptedLength;
            }
            finally
            {
                if (ptr != null)
                {
                    _buffer.ReleasePointer();
                }
            }
        }

        private unsafe void RemoveAtCore(int index)
        {
            // Shift down all values above the specified index, then null out the empty space at the end.
            byte* ptr = null;
            try
            {
                _buffer.AcquirePointer(ref ptr);
                ptr += index * sizeof(char);
                long bytesToShift = (_decryptedLength - index - 1) * sizeof(char);
                Buffer.MemoryCopy(ptr + sizeof(char), ptr, bytesToShift, bytesToShift);
                *((char*)(ptr + bytesToShift)) = (char)0;
                --_decryptedLength;
            }
            finally
            {
                if (ptr != null)
                {
                    _buffer.ReleasePointer();
                }
            }
        }

        private void SetAtCore(int index, char c)
        {
            // Overwrite the character at the specified index
            _buffer.Write((ulong)(index * sizeof(char)), c);
        }

        internal unsafe IntPtr MarshalToBSTR()
        {
            int length = _decryptedLength;
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            byte* bufferPtr = null;
            
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

            byte* bufferPtr = null;
            IntPtr stringPtr = IntPtr.Zero, result = IntPtr.Zero;
            try
            {
                _buffer.AcquirePointer(ref bufferPtr);
                if (unicode)
                {
                    int resultLength = (length + 1) * sizeof(char);
                    stringPtr = globalAlloc ? Marshal.AllocHGlobal(resultLength) : Marshal.AllocCoTaskMem(resultLength);
                    Buffer.MemoryCopy(
                        source: bufferPtr,
                        destination: (byte*)stringPtr.ToPointer(),
                        destinationSizeInBytes: resultLength,
                        sourceBytesToCopy: length * sizeof(char));
                    *(length + (char*)stringPtr) = '\0';
                }
                else
                {
                    int resultLength = Encoding.UTF8.GetByteCount((char*)bufferPtr, length) + 1;
                    stringPtr = globalAlloc ? Marshal.AllocHGlobal(resultLength) : Marshal.AllocCoTaskMem(resultLength);
                    int encodedLength = Encoding.UTF8.GetBytes((char*)bufferPtr, length, (byte*)stringPtr, resultLength);
                    Debug.Assert(encodedLength + 1 == resultLength, $"Expected encoded length to match result, got {encodedLength} != {resultLength}");
                    *(resultLength - 1 + (byte*)stringPtr) = 0;
                }

                result = stringPtr;
            }
            finally
            {
                // If there was a failure, such that result isn't initialized, 
                // release the string if we had one.
                if (stringPtr != IntPtr.Zero && result == IntPtr.Zero)
                {
                    RuntimeImports.RhZeroMemory(stringPtr, (UIntPtr)(length * sizeof(char)));
                    MarshalFree(stringPtr, globalAlloc);
                }

                if (bufferPtr != null)
                {
                    _buffer.ReleasePointer();
                }
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
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_Capacity);
            }

            // If we already have enough space allocated, we're done
            if (_buffer != null && (capacity * sizeof(char)) <= (int)_buffer.ByteLength)
            {
                return;
            }

            // We need more space, so allocate a new buffer, copy all our data into it,
            // and then swap the new for the old.
            UnmanagedBuffer newBuffer = UnmanagedBuffer.Allocate(capacity * sizeof(char));
            if (_buffer != null)
            {
                UnmanagedBuffer.Copy(_buffer, newBuffer, _buffer.ByteLength);
                _buffer.Dispose();
            }
            _buffer = newBuffer;
        }

        /// <summary>SafeBuffer for managing memory meant to be kept confidential.</summary>
        private sealed class UnmanagedBuffer : SafeBuffer
        {
            internal UnmanagedBuffer() : base(true) { }

            internal static UnmanagedBuffer Allocate(int bytes)
            {
                Debug.Assert(bytes >= 0);
                UnmanagedBuffer buffer = new UnmanagedBuffer();
                buffer.SetHandle(Marshal.AllocHGlobal(bytes));
                buffer.Initialize((ulong)bytes);
                return buffer;
            }

            internal unsafe void Clear()
            {
                byte* ptr = null;
                try
                {
                    AcquirePointer(ref ptr);
                    RuntimeImports.RhZeroMemory((IntPtr)ptr, (UIntPtr)ByteLength);
                }
                finally
                {
                    if (ptr != null)
                    {
                        ReleasePointer();
                    }
                }
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
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }
    }
}
