// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;
using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    internal static partial class CngCommon
    {
        private const int StatusUnsuccessfulRetryCount = 1;

        public static unsafe byte[] SignHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, AsymmetricPaddingMode paddingMode, void* pPaddingInfo, int estimatedSize)
        {
#if DEBUG
            estimatedSize = 2;  // Make sure the NTE_BUFFER_TOO_SMALL and TPM_E_PCP_BUFFER_TOO_SMALL scenario gets exercised.
#endif
            byte[] signature = new byte[estimatedSize];
            int numBytesNeeded;
            ErrorCode errorCode = Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, signature, out numBytesNeeded, paddingMode);

            if (errorCode == ErrorCode.STATUS_UNSUCCESSFUL)
            {
                errorCode = Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, signature, out numBytesNeeded, paddingMode);
            }

            if (errorCode.IsBufferTooSmall())
            {
                signature = new byte[numBytesNeeded];
                errorCode = Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, signature, out numBytesNeeded, paddingMode);
            }

            if (errorCode == ErrorCode.STATUS_UNSUCCESSFUL)
            {
                errorCode = Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, signature, out numBytesNeeded, paddingMode);
            }

            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            Array.Resize(ref signature, numBytesNeeded);
            return signature;
        }

        public static unsafe bool TrySignHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, Span<byte> signature, AsymmetricPaddingMode paddingMode, void* pPaddingInfo, out int bytesWritten)
        {
            for (int i = 0; i <= StatusUnsuccessfulRetryCount; i++)
            {
                ErrorCode error = Interop.NCrypt.NCryptSignHash(
                    keyHandle,
                    pPaddingInfo,
                    hash,
                    signature,
                    out int numBytesNeeded,
                    paddingMode);

                switch (error)
                {
                    case ErrorCode.ERROR_SUCCESS:
                        bytesWritten = numBytesNeeded;
                        Debug.Assert(bytesWritten <= signature.Length);
                        return true;

                    case ErrorCode code when code.IsBufferTooSmall():
                        bytesWritten = 0;
                        return false;

                    case ErrorCode.STATUS_UNSUCCESSFUL:
                        // Retry
                        break;

                    default:
                        throw error.ToCryptographicException();
                }
            }

            throw ErrorCode.STATUS_UNSUCCESSFUL.ToCryptographicException();
        }

        public static unsafe bool VerifyHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, AsymmetricPaddingMode paddingMode, void* pPaddingInfo)
        {
            ErrorCode errorCode = Interop.NCrypt.NCryptVerifySignature(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, paddingMode);

            if (errorCode == ErrorCode.STATUS_UNSUCCESSFUL)
            {
                errorCode = Interop.NCrypt.NCryptVerifySignature(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, paddingMode);
            }

            return errorCode == ErrorCode.ERROR_SUCCESS;  // For consistency with other AsymmetricAlgorithm-derived classes, return "false" for any error code rather than making the caller catch an exception.
        }
    }
}
