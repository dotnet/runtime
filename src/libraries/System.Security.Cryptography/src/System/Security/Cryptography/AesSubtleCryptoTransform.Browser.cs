// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed class AesSubtleCryptoTransform : BasicSymmetricCipher, ILiteSymmetricCipher
    {
        private const int BlockSizeBytes = AesImplementation.BlockSizeBytes;

        private readonly bool _encrypting;

        private readonly byte[] _key;
        private byte[]? _lastBlockBuffer;

        public AesSubtleCryptoTransform(byte[] key,
                                        byte[] iv,
                                        bool encrypting)
            : base(iv, BlockSizeBytes, BlockSizeBytes)
        {
            _encrypting = encrypting;

            // iv is guaranteed to be cloned before this method, but not key
            _key = key.CloneByteArray();
        }

        public AesSubtleCryptoTransform(ReadOnlySpan<byte> key,
                                        ReadOnlySpan<byte> iv,
                                        bool encrypting)
            : base(iv.ToArray(), BlockSizeBytes, BlockSizeBytes)
        {
            _encrypting = encrypting;

            _key = key.ToArray();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // We need to always zeroize the following fields because they contain sensitive data
                CryptographicOperations.ZeroMemory(_key);
                CryptographicOperations.ZeroMemory(_lastBlockBuffer);
            }

            base.Dispose(disposing);
        }

        public override int Transform(ReadOnlySpan<byte> input, Span<byte> output) =>
            Transform(input, output, isFinal: false);

        public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int bytesWritten = Transform(input, output, isFinal: true);
            Reset();
            return bytesWritten;
        }

        private int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool isFinal)
        {
            Debug.Assert(output.Length >= input.Length);
            Debug.Assert(input.Length % BlockSizeInBytes == 0);

            if (input.IsEmpty)
            {
                return 0;
            }

            // Note: SubtleCrypto always uses PKCS7 padding.

            // In order to implement streaming on top of SubtleCrypto's "one shot" API, we have to do the following:
            // 1. Remember the last block of cipher text to pass as the "IV" of the next block.
            // 2. When encrypting a complete block, PKCS7 padding will always add one block of '0x10' padding bytes. We
            // need to strip this padding block off in between Transform calls. This is done by Interop.BrowserCrypto.EncryptDecrypt.
            // 3. When decrypting, we need to do the inverse: append an encrypted block of '0x10' padding bytes, so
            // SubtleCrypto will decrypt input as a complete message. This is done by Interop.BrowserCrypto.EncryptDecrypt.

            return _encrypting ?
                EncryptBlock(input, output, isFinal) :
                DecryptBlock(input, output, isFinal);
        }

        private int EncryptBlock(ReadOnlySpan<byte> input, Span<byte> output, bool isFinal)
        {
            int bytesWritten = EncryptDecrypt(input, output);

            if (!isFinal)
            {
                SaveLastBlock(output.Slice(0, bytesWritten));
            }

            return bytesWritten;
        }

        private int DecryptBlock(ReadOnlySpan<byte> input, Span<byte> output, bool isFinal)
        {
            Span<byte> lastInputBlockCopy = stackalloc byte[BlockSizeBytes];
            if (!isFinal)
            {
                // Save the lastInputBlock in a temp buffer first, in case input and output are overlapped
                // and decrypting to the output overwrites the input.
                ReadOnlySpan<byte> lastInputBlock = input.Slice(input.Length - BlockSizeBytes);
                lastInputBlock.CopyTo(lastInputBlockCopy);
            }

            int numBytesWritten = EncryptDecrypt(input, output);

            if (!isFinal)
            {
                SaveLastBlock(lastInputBlockCopy);
            }

            return numBytesWritten;
        }

        private void SaveLastBlock(ReadOnlySpan<byte> buffer)
        {
            Debug.Assert(buffer.Length > 0 && buffer.Length % BlockSizeBytes == 0);

            ReadOnlySpan<byte> lastBlock = buffer.Slice(buffer.Length - BlockSizeBytes);
            if (_lastBlockBuffer is null)
            {
                _lastBlockBuffer = lastBlock.ToArray();
            }
            else
            {
                Debug.Assert(_lastBlockBuffer.Length == BlockSizeBytes);
                lastBlock.CopyTo(_lastBlockBuffer);
            }
        }

        private unsafe int EncryptDecrypt(ReadOnlySpan<byte> input, Span<byte> output)
        {
            byte[] iv = _lastBlockBuffer ?? IV!;

            fixed (byte* pKey = _key)
            fixed (byte* pIV = iv)
            fixed (byte* pInput = input)
            fixed (byte* pOutput = output)
            {
                int bytesWritten = Interop.BrowserCrypto.EncryptDecrypt(
                    _encrypting ? 1 : 0,
                    pKey, _key.Length,
                    pIV, iv.Length,
                    pInput, input.Length,
                    pOutput, output.Length);

                if (bytesWritten < 0)
                {
                    throw new CryptographicException(SR.Format(SR.Unknown_SubtleCrypto_Error, bytesWritten));
                }

                return bytesWritten;
            }
        }

        //
        // resets the state of the transform
        //

        void ILiteSymmetricCipher.Reset(ReadOnlySpan<byte> iv) => throw new NotImplementedException(); // never invoked

        private void Reset()
        {
            CryptographicOperations.ZeroMemory(_lastBlockBuffer);
            _lastBlockBuffer = null;
        }
    }
}
