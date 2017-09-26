// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Win32.SafeHandles;
using ErrorCode = Interop.NCrypt.ErrorCode;
using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;

namespace Internal.Cryptography
{
    internal static partial class CngCommon
    {
        public static unsafe byte[] SignHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, AsymmetricPaddingMode paddingMode, void* pPaddingInfo, int estimatedSize)
        {
#if DEBUG
            estimatedSize = 2;  // Make sure the NTE_BUFFER_TOO_SMALL scenario gets exercised.
#endif
            byte[] signature = new byte[estimatedSize];
            int numBytesNeeded;
            ErrorCode errorCode = Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, out numBytesNeeded, paddingMode);
            if (errorCode == ErrorCode.NTE_BUFFER_TOO_SMALL)
            {
                signature = new byte[numBytesNeeded];
                errorCode = Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, out numBytesNeeded, paddingMode);
            }
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            Array.Resize(ref signature, numBytesNeeded);
            return signature;
        }

        public static unsafe bool TrySignHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, Span<byte> signature, AsymmetricPaddingMode paddingMode, void* pPaddingInfo, out int bytesWritten)
        {
            ErrorCode error = Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, out int numBytesNeeded, paddingMode);
            switch (error)
            {
                case ErrorCode.ERROR_SUCCESS:
                    bytesWritten = numBytesNeeded;
                    return true;

                case ErrorCode.NTE_BUFFER_TOO_SMALL:
                    bytesWritten = 0;
                    return false;

                default:
                    throw error.ToCryptographicException();
            }
        }

        public static unsafe bool VerifyHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, AsymmetricPaddingMode paddingMode, void* pPaddingInfo)
        {
            ErrorCode errorCode = Interop.NCrypt.NCryptVerifySignature(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, paddingMode);
            return errorCode == ErrorCode.ERROR_SUCCESS;  // For consistency with other AsymmetricAlgorithm-derived classes, return "false" for any error code rather than making the caller catch an exception.
        }
    }
}
