// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

using PAL_SymmetricAlgorithm = Interop.AppleCrypto.PAL_SymmetricAlgorithm;
using PAL_ChainingMode = Interop.AppleCrypto.PAL_ChainingMode;

namespace System.Security.Cryptography
{
    internal sealed class AppleCCCryptorLite : ILiteSymmetricCipher
    {
        private readonly SafeAppleCryptorHandle _cryptor;
        private readonly bool _canReset;

#if DEBUG
        private bool _isFinalized;
#endif

        public int BlockSizeInBytes { get; }
        public int PaddingSizeInBytes { get; }

        public unsafe AppleCCCryptorLite(
            PAL_SymmetricAlgorithm algorithm,
            CipherMode cipherMode,
            int blockSizeInBytes,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            bool encrypting,
            int feedbackSizeInBytes,
            int paddingSizeInBytes)
        {
            int ret;
            int ccStatus;

            fixed (byte* pbKey = key)
            fixed (byte* pbIv = iv)
            {
                ret = Interop.AppleCrypto.CryptorCreate(
                    encrypting
                        ? Interop.AppleCrypto.PAL_SymmetricOperation.Encrypt
                        : Interop.AppleCrypto.PAL_SymmetricOperation.Decrypt,
                    algorithm,
                    GetPalChainMode(algorithm, cipherMode, feedbackSizeInBytes),
                    Interop.AppleCrypto.PAL_PaddingMode.None,
                    pbKey,
                    key.Length,
                    pbIv,
                    Interop.AppleCrypto.PAL_SymmetricOptions.None,
                    out _cryptor,
                    out ccStatus);
            }

            ProcessInteropError(ret, ccStatus);

            _canReset = cipherMode != CipherMode.CFB;
            BlockSizeInBytes = blockSizeInBytes;
            PaddingSizeInBytes = paddingSizeInBytes;
        }

        public int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
#if DEBUG
            if (_isFinalized)
            {
                Debug.Fail("Cipher was reused without being reset.");
                throw new CryptographicException();
            }

            _isFinalized = true;
#endif

            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);
            Debug.Assert(input.Length <= output.Length);

            int written = 0;

            if (input.Overlaps(output, out int offset) && offset != 0)
            {
                byte[] rented = CryptoPool.Rent(output.Length);

                try
                {
                    written = ProcessFinalBlock(input, rented);
                    rented.AsSpan(0, written).CopyTo(output);
                }
                finally
                {
                    CryptoPool.Return(rented, clearSize: written);
                }
            }
            else
            {
                written = ProcessFinalBlock(input, output);
            }

            return written;
        }

        public int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
#if DEBUG
            if (_isFinalized)
            {
                Debug.Fail("Cipher was reused without being reset.");
                throw new CryptographicException();
            }
#endif

            Debug.Assert(input.Length > 0);
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            return CipherUpdate(input, output);
        }

        public void Dispose()
        {
            _cryptor.Dispose();
        }

        public unsafe void Reset(ReadOnlySpan<byte> iv)
        {
            if (!_canReset)
            {
                Debug.Fail("Cipher cannot be reset.");
                throw new CryptographicException();
            }

            fixed (byte* pbIv = iv)
            {
                int ret = Interop.AppleCrypto.CryptorReset(_cryptor, pbIv, out int ccStatus);
                ProcessInteropError(ret, ccStatus);
            }

#if DEBUG
            _isFinalized = false;
#endif
        }

        private unsafe int CipherUpdate(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int ret;
            int ccStatus;
            int bytesWritten;

            if (input.Length == 0)
            {
                return 0;
            }

            fixed (byte* pInput = input)
            fixed (byte* pOutput = output)
            {
                ret = Interop.AppleCrypto.CryptorUpdate(
                    _cryptor,
                    pInput,
                    input.Length,
                    pOutput,
                    output.Length,
                    out bytesWritten,
                    out ccStatus);
            }

            ProcessInteropError(ret, ccStatus);

            return bytesWritten;
        }

        private static PAL_ChainingMode GetPalChainMode(PAL_SymmetricAlgorithm algorithm, CipherMode cipherMode, int feedbackSizeInBytes)
        {
            return cipherMode switch
            {
                CipherMode.CBC => PAL_ChainingMode.CBC,
                CipherMode.ECB => PAL_ChainingMode.ECB,
                CipherMode.CFB when feedbackSizeInBytes == 1 => PAL_ChainingMode.CFB8,
                CipherMode.CFB => PAL_ChainingMode.CFB,
                _ => throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CipherModeNotSupported, cipherMode)),
            };
        }

        private unsafe int ProcessFinalBlock(ReadOnlySpan<byte> input, Span<byte> output)
        {
            if (input.Length == 0)
            {
                return 0;
            }

            int outputBytes = CipherUpdate(input, output);
            int ret;
            int errorCode;

            Debug.Assert(output.Length > 0);

            fixed (byte* outputStart = output)
            {
                byte* outputCurrent = outputStart + outputBytes;
                int bytesWritten;

                ret = Interop.AppleCrypto.CryptorFinal(
                    _cryptor,
                    outputCurrent,
                    output.Length - outputBytes,
                    out bytesWritten,
                    out errorCode);

                outputBytes += bytesWritten;
            }

            ProcessInteropError(ret, errorCode);

            return outputBytes;
        }

        private static void ProcessInteropError(int functionReturnCode, int ccStatus)
        {
            // Success
            if (functionReturnCode == 1)
            {
                return;
            }

            // Platform error
            if (functionReturnCode == 0)
            {
                Debug.Assert(ccStatus != 0, "Interop function returned 0 but a system code of success");
                throw Interop.AppleCrypto.CreateExceptionForCCError(
                    ccStatus,
                    Interop.AppleCrypto.CCCryptorStatus);
            }

            // Usually this will be -1, a general indication of bad inputs.
            Debug.Fail($"Interop boundary returned unexpected value {functionReturnCode}");
            throw new CryptographicException();
        }
    }
}
