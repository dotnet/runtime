// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal sealed class AppleCCCryptor : BasicSymmetricCipher
    {
        private readonly bool _encrypting;
        private SafeAppleCryptorHandle _cryptor;

        public AppleCCCryptor(
            Interop.AppleCrypto.PAL_SymmetricAlgorithm algorithm,
            CipherMode cipherMode,
            int blockSizeInBytes,
            byte[] key,
            byte[]? iv,
            bool encrypting)
            : base(cipherMode.GetCipherIv(iv), blockSizeInBytes)
        {
            _encrypting = encrypting;

            OpenCryptor(algorithm, cipherMode, key);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cryptor?.Dispose();
                _cryptor = null!;
            }

            base.Dispose(disposing);
        }

        public override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert(input.Length > 0);
            Debug.Assert((input.Length % BlockSizeInBytes) == 0);

            return CipherUpdate(input, output);
        }

        public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert((input.Length % BlockSizeInBytes) == 0);
            Debug.Assert(input.Length <= output.Length);

            int written = ProcessFinalBlock(input, output);
            Reset();
            return written;
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

        [MemberNotNull(nameof(_cryptor))]
        private unsafe void OpenCryptor(
            Interop.AppleCrypto.PAL_SymmetricAlgorithm algorithm,
            CipherMode cipherMode,
            byte[] key)
        {
            int ret;
            int ccStatus;

            byte[]? iv = IV;

            fixed (byte* pbKey = key)
            fixed (byte* pbIv = iv)
            {
                ret = Interop.AppleCrypto.CryptorCreate(
                    _encrypting
                        ? Interop.AppleCrypto.PAL_SymmetricOperation.Encrypt
                        : Interop.AppleCrypto.PAL_SymmetricOperation.Decrypt,
                    algorithm,
                    GetPalChainMode(cipherMode),
                    Interop.AppleCrypto.PAL_PaddingMode.None,
                    pbKey,
                    key.Length,
                    pbIv,
                    Interop.AppleCrypto.PAL_SymmetricOptions.None,
                    out _cryptor,
                    out ccStatus);
            }

            ProcessInteropError(ret, ccStatus);
        }

        private Interop.AppleCrypto.PAL_ChainingMode GetPalChainMode(CipherMode cipherMode)
        {
            switch (cipherMode)
            {
                case CipherMode.CBC:
                    return Interop.AppleCrypto.PAL_ChainingMode.CBC;
                case CipherMode.ECB:
                    return Interop.AppleCrypto.PAL_ChainingMode.ECB;
                default:
                    throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CipherModeNotSupported, cipherMode));
            }
        }

        private unsafe void Reset()
        {
            int ret;
            int ccStatus;

            byte[]? iv = IV;

            fixed (byte* pbIv = iv)
            {
                ret = Interop.AppleCrypto.CryptorReset(_cryptor, pbIv, out ccStatus);
            }

            ProcessInteropError(ret, ccStatus);
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
