// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System.Security
{
    public sealed partial class SecureString
    {
        private static int GetAlignedByteSize(int length)
        {
            int byteSize = Math.Max(length, 1) * sizeof(char);

            const int blockSize = (int)Interop.Crypt32.CRYPTPROTECTMEMORY_BLOCK_SIZE;
            return ((byteSize + (blockSize - 1)) / blockSize) * blockSize;
        }

        private void ProtectMemory()
        {
            Debug.Assert(_buffer != null);
            Debug.Assert(!_buffer.IsInvalid, "Invalid buffer!");

            if (_decryptedLength != 0 &&
                !_encrypted &&
                !Interop.Crypt32.CryptProtectMemory(_buffer, (uint)_buffer.ByteLength, Interop.Crypt32.CRYPTPROTECTMEMORY_SAME_PROCESS))
            {
                throw new CryptographicException(Marshal.GetLastPInvokeError());
            }

            _encrypted = true;
        }

        private void UnprotectMemory()
        {
            Debug.Assert(_buffer != null);
            Debug.Assert(!_buffer.IsInvalid, "Invalid buffer!");

            if (_decryptedLength != 0 &&
                _encrypted &&
                !Interop.Crypt32.CryptUnprotectMemory(_buffer, (uint)_buffer.ByteLength, Interop.Crypt32.CRYPTPROTECTMEMORY_SAME_PROCESS))
            {
                throw new CryptographicException(Marshal.GetLastPInvokeError());
            }

            _encrypted = false;
        }
    }
}
