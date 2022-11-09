// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        internal static unsafe void ChaCha20Poly1305Encrypt(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> aad)
        {
            fixed (byte* keyPtr = key)
            fixed (byte* noncePtr = nonce)
            fixed (byte* plaintextPtr = plaintext)
            fixed (byte* ciphertextPtr = ciphertext)
            fixed (byte* tagPtr = tag)
            fixed (byte* aadPtr = aad)
            {
                const int Success = 1;
                int result = AppleCryptoNative_ChaCha20Poly1305Encrypt(
                    keyPtr, key.Length,
                    noncePtr, nonce.Length,
                    plaintextPtr, plaintext.Length,
                    ciphertextPtr, ciphertext.Length,
                    tagPtr, tag.Length,
                    aadPtr, aad.Length);

                if (result != Success)
                {
                    Debug.Assert(result == 0);
                    CryptographicOperations.ZeroMemory(ciphertext);
                    CryptographicOperations.ZeroMemory(tag);
                    throw new CryptographicException();
                }
            }
        }

        internal static unsafe void ChaCha20Poly1305Decrypt(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> aad)
        {
            fixed (byte* keyPtr = key)
            fixed (byte* noncePtr = nonce)
            fixed (byte* ciphertextPtr = ciphertext)
            fixed (byte* tagPtr = tag)
            fixed (byte* plaintextPtr = plaintext)
            fixed (byte* aadPtr = aad)
            {
                const int Success = 1;
                const int AuthTagMismatch = -1;
                int result = AppleCryptoNative_ChaCha20Poly1305Decrypt(
                    keyPtr, key.Length,
                    noncePtr, nonce.Length,
                    ciphertextPtr, ciphertext.Length,
                    tagPtr, tag.Length,
                    plaintextPtr, plaintext.Length,
                    aadPtr, aad.Length);

                if (result != Success)
                {
                    CryptographicOperations.ZeroMemory(plaintext);

                    if (result == AuthTagMismatch)
                    {
                        throw new AuthenticationTagMismatchException();
                    }
                    else
                    {
                        Debug.Assert(result == 0);
                        throw new CryptographicException();
                    }
                }
            }
        }

        internal static unsafe void AesGcmEncrypt(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> aad)
        {
            fixed (byte* keyPtr = key)
            fixed (byte* noncePtr = nonce)
            fixed (byte* plaintextPtr = plaintext)
            fixed (byte* ciphertextPtr = ciphertext)
            fixed (byte* tagPtr = tag)
            fixed (byte* aadPtr = aad)
            {
                const int Success = 1;
                int result = AppleCryptoNative_AesGcmEncrypt(
                    keyPtr, key.Length,
                    noncePtr, nonce.Length,
                    plaintextPtr, plaintext.Length,
                    ciphertextPtr, ciphertext.Length,
                    tagPtr, tag.Length,
                    aadPtr, aad.Length);

                if (result != Success)
                {
                    Debug.Assert(result == 0);
                    CryptographicOperations.ZeroMemory(ciphertext);
                    CryptographicOperations.ZeroMemory(tag);
                    throw new CryptographicException();
                }
            }
        }

        internal static unsafe void AesGcmDecrypt(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> aad)
        {
            fixed (byte* keyPtr = key)
            fixed (byte* noncePtr = nonce)
            fixed (byte* ciphertextPtr = ciphertext)
            fixed (byte* tagPtr = tag)
            fixed (byte* plaintextPtr = plaintext)
            fixed (byte* aadPtr = aad)
            {
                const int Success = 1;
                const int AuthTagMismatch = -1;
                int result = AppleCryptoNative_AesGcmDecrypt(
                    keyPtr, key.Length,
                    noncePtr, nonce.Length,
                    ciphertextPtr, ciphertext.Length,
                    tagPtr, tag.Length,
                    plaintextPtr, plaintext.Length,
                    aadPtr, aad.Length);

                if (result != Success)
                {
                    CryptographicOperations.ZeroMemory(plaintext);

                    if (result == AuthTagMismatch)
                    {
                        throw new AuthenticationTagMismatchException();
                    }
                    else
                    {
                        Debug.Assert(result == 0);
                        throw new CryptographicException();
                    }
                }
            }
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static unsafe partial int AppleCryptoNative_ChaCha20Poly1305Encrypt(
            byte* keyPtr,
            int keyLength,
            byte* noncePtr,
            int nonceLength,
            byte* plaintextPtr,
            int plaintextLength,
            byte* ciphertextPtr,
            int ciphertextLength,
            byte* tagPtr,
            int tagLength,
            byte* aadPtr,
            int aadLength);

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static unsafe partial int AppleCryptoNative_ChaCha20Poly1305Decrypt(
            byte* keyPtr,
            int keyLength,
            byte* noncePtr,
            int nonceLength,
            byte* ciphertextPtr,
            int ciphertextLength,
            byte* tagPtr,
            int tagLength,
            byte* plaintextPtr,
            int plaintextLength,
            byte* aadPtr,
            int aadLength);

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static unsafe partial int AppleCryptoNative_AesGcmEncrypt(
            byte* keyPtr,
            int keyLength,
            byte* noncePtr,
            int nonceLength,
            byte* plaintextPtr,
            int plaintextLength,
            byte* ciphertextPtr,
            int ciphertextLength,
            byte* tagPtr,
            int tagLength,
            byte* aadPtr,
            int aadLength);

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static unsafe partial int AppleCryptoNative_AesGcmDecrypt(
            byte* keyPtr,
            int keyLength,
            byte* noncePtr,
            int nonceLength,
            byte* ciphertextPtr,
            int ciphertextLength,
            byte* tagPtr,
            int tagLength,
            byte* plaintextPtr,
            int plaintextLength,
            byte* aadPtr,
            int aadLength);
    }
}
