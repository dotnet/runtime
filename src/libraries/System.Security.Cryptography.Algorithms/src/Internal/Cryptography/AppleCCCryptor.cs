// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
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
            byte[] iv,
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
                _cryptor = null;
            }

            base.Dispose(disposing);
        }

        public override int Transform(byte[] input, int inputOffset, int count, byte[] output, int outputOffset)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(count > 0);
            Debug.Assert((count % BlockSizeInBytes) == 0);
            Debug.Assert(input.Length - inputOffset >= count);
            Debug.Assert(output != null);
            Debug.Assert(outputOffset >= 0);
            Debug.Assert(output.Length - outputOffset >= count);

            return CipherUpdate(input, inputOffset, count, output, outputOffset);
        }

        public override byte[] TransformFinal(byte[] input, int inputOffset, int count)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert((count % BlockSizeInBytes) == 0);
            Debug.Assert(input.Length - inputOffset >= count);

            byte[] output = ProcessFinalBlock(input, inputOffset, count);
            Reset();
            return output;
        }

        private unsafe byte[] ProcessFinalBlock(byte[] input, int inputOffset, int count)
        {
            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] output = new byte[count];
            int outputBytes = CipherUpdate(input, inputOffset, count, output, 0);
            int ret;
            int errorCode;

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

            Exception exception = ProcessInteropError(ret, errorCode);

            if (exception != null)
            {
                throw exception;
            }

            if (outputBytes == output.Length)
            {
                return output;
            }

            if (outputBytes == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] userData = new byte[outputBytes];
            Buffer.BlockCopy(output, 0, userData, 0, outputBytes);
            return userData;
        }

        private unsafe int CipherUpdate(byte[] input, int inputOffset, int count, byte[] output, int outputOffset)
        {
            int ret;
            int errorCode;
            int bytesWritten;

            if (count == 0)
            {
                return 0;
            }

            fixed (byte* inputStart = input)
            fixed (byte* outputStart = output)
            {
                byte* inputCurrent = inputStart + inputOffset;
                byte* outputCurrent = outputStart + outputOffset;

                ret = Interop.AppleCrypto.CryptorUpdate(
                    _cryptor,
                    inputCurrent,
                    count,
                    outputCurrent,
                    output.Length - outputOffset,
                    out bytesWritten,
                    out errorCode);
            }

            Exception exception = ProcessInteropError(ret, errorCode);

            if (exception != null)
            {
                throw exception;
            }

            return bytesWritten;
        }

        private unsafe void OpenCryptor(
            Interop.AppleCrypto.PAL_SymmetricAlgorithm algorithm,
            CipherMode cipherMode,
            byte[] key)
        {
            int ret;
            int errorCode;

            byte[] iv = IV;

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
                    out errorCode);
            }

            Exception exception = ProcessInteropError(ret, errorCode);

            if (exception != null)
            {
                throw exception;
            }
        }

        private Interop.AppleCrypto.PAL_ChainingMode GetPalChainMode(CipherMode cipherMode)
        {
            switch (cipherMode)
            {
                case CipherMode.CBC:
                    return Interop.AppleCrypto.PAL_ChainingMode.CBC;
                case CipherMode.ECB:
                    return Interop.AppleCrypto.PAL_ChainingMode.ECB;
                case CipherMode.CTS:
                    throw new PlatformNotSupportedException(SR.Cryptography_UnsupportedPaddingMode);
                default:
                    Debug.Fail($"Unknown cipher mode: {cipherMode}");
                    throw new CryptographicException(SR.Cryptography_UnknownPaddingMode);
            }
        }

        private unsafe void Reset()
        {
            int ret;
            int errorCode;

            byte[] iv = IV;

            fixed (byte* pbIv = iv)
            {
                ret = Interop.AppleCrypto.CryptorReset(_cryptor, pbIv, out errorCode);
            }

            Exception exception = ProcessInteropError(ret, errorCode);

            if (exception != null)
            {
                throw exception;
            }
        }

        private static Exception ProcessInteropError(int functionReturnCode, int emittedErrorCode)
        {
            // Success
            if (functionReturnCode == 1)
            {
                return null;
            }

            // Platform error
            if (functionReturnCode == 0)
            {
                Debug.Assert(emittedErrorCode != 0, "Interop function returned 0 but a system code of success");
                return Interop.AppleCrypto.CreateExceptionForCCError(
                    emittedErrorCode,
                    Interop.AppleCrypto.CCCryptorStatus);
            }

            // Usually this will be -1, a general indication of bad inputs.
            Debug.Fail($"Interop boundary returned unexpected value {functionReturnCode}");
            return new CryptographicException();
        }
    }
}
