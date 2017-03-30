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
            Debug.Assert(input != null, "Expected valid input, got null");
            Debug.Assert(inputOffset >= 0, $"Expected non-negative inputOffset, got {inputOffset}");
            Debug.Assert(count > 0, $"Expected positive count, got {count}");
            Debug.Assert((count % BlockSizeInBytes) == 0, $"Expected count aligned to block size {BlockSizeInBytes}, got {count}");
            Debug.Assert(input.Length - inputOffset >= count, $"Expected valid input length/offset/count triplet, got {input.Length}/{inputOffset}/{count}");
            Debug.Assert(output != null, "Expected valid output, got null");
            Debug.Assert(outputOffset >= 0, $"Expected non-negative outputOffset, got {outputOffset}");
            Debug.Assert(output.Length - outputOffset >= count, $"Expected valid output length/offset/count triplet, got {output.Length}/{outputOffset}/{count}");

            return CipherUpdate(input, inputOffset, count, output, outputOffset);
        }

        public override byte[] TransformFinal(byte[] input, int inputOffset, int count)
        {
            Debug.Assert(input != null, "Expected valid input, got null");
            Debug.Assert(inputOffset >= 0, $"Expected non-negative inputOffset, got {inputOffset}");
            Debug.Assert(count > 0, $"Expected positive count, got {count}");
            Debug.Assert((count % BlockSizeInBytes) == 0, $"Expected count aligned to block size {BlockSizeInBytes}, got {count}");
            Debug.Assert(input.Length - inputOffset >= count, $"Expected valid input length/offset/count triplet, got {input.Length}/{inputOffset}/{count}");

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

            fixed (byte* outputStart = &output[0])
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
            int ccStatus;
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
                    out ccStatus);
            }

            ProcessInteropError(ret, ccStatus);

            return bytesWritten;
        }

        private unsafe void OpenCryptor(
            Interop.AppleCrypto.PAL_SymmetricAlgorithm algorithm,
            CipherMode cipherMode,
            byte[] key)
        {
            int ret;
            int ccStatus;

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

            byte[] iv = IV;

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
