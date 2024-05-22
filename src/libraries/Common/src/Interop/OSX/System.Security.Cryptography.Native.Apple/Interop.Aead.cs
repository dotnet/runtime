// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;

#pragma warning disable CS3016 // Arrays as attribute arguments are not CLS Compliant

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
                AppleCryptoNative_ChaCha20Poly1305Encrypt(
                    new(keyPtr, key.Length),
                    new(noncePtr, nonce.Length),
                    new(plaintextPtr, plaintext.Length),
                    new(ciphertextPtr, ciphertext.Length),
                    new(tagPtr, tag.Length),
                    new(aadPtr, aad.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
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
                AppleCryptoNative_ChaCha20Poly1305Decrypt(
                    new(keyPtr, key.Length),
                    new(noncePtr, nonce.Length),
                    new(ciphertextPtr, ciphertext.Length),
                    new(tagPtr, tag.Length),
                    new(plaintextPtr, plaintext.Length),
                    new(aadPtr, aad.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
                    CryptographicOperations.ZeroMemory(plaintext);

                    if (AppleCryptoNative_IsAuthenticationFailure(error.Value) == AuthTagMismatch)
                    {
                        throw new AuthenticationTagMismatchException();
                    }
                    else
                    {
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
                SwiftError error;
                AppleCryptoNative_AesGcmEncrypt(
                    new(keyPtr, key.Length),
                    new(noncePtr, nonce.Length),
                    new(plaintextPtr, plaintext.Length),
                    new(ciphertextPtr, ciphertext.Length),
                    new(tagPtr, tag.Length),
                    new(aadPtr, aad.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
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
                SwiftError error;
                AppleCryptoNative_AesGcmDecrypt(
                    new(keyPtr, key.Length),
                    new(noncePtr, nonce.Length),
                    new(ciphertextPtr, ciphertext.Length),
                    new(tagPtr, tag.Length),
                    new(plaintextPtr, plaintext.Length),
                    new(aadPtr, aad.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
                    CryptographicOperations.ZeroMemory(plaintext);

                    if (AppleCryptoNative_IsAuthenticationFailure(error.Value) == AuthTagMismatch)
                    {
                        throw new AuthenticationTagMismatchException();
                    }
                    else
                    {
                        throw new CryptographicException();
                    }
                }
            }
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial void AppleCryptoNative_ChaCha20Poly1305Encrypt(
            UnsafeBufferPointer<byte> key,
            UnsafeBufferPointer<byte> nonce,
            UnsafeBufferPointer<byte> plaintext,
            UnsafeMutableBufferPointer<byte> ciphertext,
            UnsafeMutableBufferPointer<byte> tag,
            UnsafeBufferPointer<byte> aad,
            out SwiftError error
        );

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial void AppleCryptoNative_ChaCha20Poly1305Decrypt(
            UnsafeBufferPointer<byte> key,
            UnsafeBufferPointer<byte> nonce,
            UnsafeBufferPointer<byte> ciphertext,
            UnsafeBufferPointer<byte> tag,
            UnsafeMutableBufferPointer<byte> plaintext,
            UnsafeBufferPointer<byte> aad,
            out SwiftError error
        );

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial void AppleCryptoNative_AesGcmEncrypt(
            UnsafeBufferPointer<byte> key,
            UnsafeBufferPointer<byte> nonce,
            UnsafeBufferPointer<byte> plaintext,
            UnsafeMutableBufferPointer<byte> ciphertext,
            UnsafeMutableBufferPointer<byte> tag,
            UnsafeBufferPointer<byte> aad,
            out SwiftError error
        );

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial void AppleCryptoNative_AesGcmDecrypt(
            UnsafeBufferPointer<byte> key,
            UnsafeBufferPointer<byte> nonce,
            UnsafeBufferPointer<byte> ciphertext,
            UnsafeBufferPointer<byte> tag,
            UnsafeMutableBufferPointer<byte> plaintext,
            UnsafeBufferPointer<byte> aad,
            out SwiftError error
        );

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvSwift) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static unsafe partial bool AppleCryptoNative_IsAuthenticationFailure(void* error);
    }
}
