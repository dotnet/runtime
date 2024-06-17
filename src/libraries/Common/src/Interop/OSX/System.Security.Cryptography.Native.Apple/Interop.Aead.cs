// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using Swift.Runtime;

#pragma warning disable CS3016 // Arrays as attribute arguments are not CLS Compliant

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        private static byte NullSentinel;

        // CryptoKit doesn't do well with a null pointer for the buffer data,
        // so provide a sentinel pointer instead.
        private static ref readonly byte GetSwiftRef(ReadOnlySpan<byte> b)
        {
            return ref (b.Length == 0
                ? ref NullSentinel
                : ref MemoryMarshal.GetReference(b));
        }

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
            fixed (byte* plaintextPtr = &GetSwiftRef(plaintext))
            fixed (byte* ciphertextPtr = &GetSwiftRef(ciphertext))
            fixed (byte* tagPtr = tag)
            fixed (byte* aadPtr = &GetSwiftRef(aad))
            {
                AppleCryptoNative_ChaCha20Poly1305Encrypt(
                    new UnsafeBufferPointer<byte>(keyPtr, key.Length),
                    new UnsafeBufferPointer<byte>(noncePtr, nonce.Length),
                    new UnsafeBufferPointer<byte>(plaintextPtr, plaintext.Length),
                    new UnsafeMutableBufferPointer<byte>(ciphertextPtr, ciphertext.Length),
                    new UnsafeMutableBufferPointer<byte>(tagPtr, tag.Length),
                    new UnsafeBufferPointer<byte>(aadPtr, aad.Length),
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
            fixed (byte* ciphertextPtr = &GetSwiftRef(ciphertext))
            fixed (byte* tagPtr = tag)
            fixed (byte* plaintextPtr = &GetSwiftRef(plaintext))
            fixed (byte* aadPtr = &GetSwiftRef(aad))
            {
                AppleCryptoNative_ChaCha20Poly1305Decrypt(
                    new UnsafeBufferPointer<byte>(keyPtr, key.Length),
                    new UnsafeBufferPointer<byte>(noncePtr, nonce.Length),
                    new UnsafeBufferPointer<byte>(ciphertextPtr, ciphertext.Length),
                    new UnsafeBufferPointer<byte>(tagPtr, tag.Length),
                    new UnsafeMutableBufferPointer<byte>(plaintextPtr, plaintext.Length),
                    new UnsafeBufferPointer<byte>(aadPtr, aad.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
                    CryptographicOperations.ZeroMemory(plaintext);

                    if (AppleCryptoNative_IsAuthenticationFailure(error.Value))
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
            fixed (byte* plaintextPtr = &GetSwiftRef(plaintext))
            fixed (byte* ciphertextPtr = &GetSwiftRef(ciphertext))
            fixed (byte* tagPtr = tag)
            fixed (byte* aadPtr = &GetSwiftRef(aad))
            {
                AppleCryptoNative_AesGcmEncrypt(
                    new UnsafeBufferPointer<byte>(keyPtr, key.Length),
                    new UnsafeBufferPointer<byte>(noncePtr, nonce.Length),
                    new UnsafeBufferPointer<byte>(plaintextPtr, plaintext.Length),
                    new UnsafeMutableBufferPointer<byte>(ciphertextPtr, ciphertext.Length),
                    new UnsafeMutableBufferPointer<byte>(tagPtr, tag.Length),
                    new UnsafeBufferPointer<byte>(aadPtr, aad.Length),
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
            fixed (byte* ciphertextPtr = &GetSwiftRef(ciphertext))
            fixed (byte* tagPtr = tag)
            fixed (byte* plaintextPtr = &GetSwiftRef(plaintext))
            fixed (byte* aadPtr = &GetSwiftRef(aad))
            {
                AppleCryptoNative_AesGcmDecrypt(
                    new UnsafeBufferPointer<byte>(keyPtr, key.Length),
                    new UnsafeBufferPointer<byte>(noncePtr, nonce.Length),
                    new UnsafeBufferPointer<byte>(ciphertextPtr, ciphertext.Length),
                    new UnsafeBufferPointer<byte>(tagPtr, tag.Length),
                    new UnsafeMutableBufferPointer<byte>(plaintextPtr, plaintext.Length),
                    new UnsafeBufferPointer<byte>(aadPtr, aad.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
                    CryptographicOperations.ZeroMemory(plaintext);

                    if (AppleCryptoNative_IsAuthenticationFailure(error.Value))
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
            out SwiftError error);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial void AppleCryptoNative_ChaCha20Poly1305Decrypt(
            UnsafeBufferPointer<byte> key,
            UnsafeBufferPointer<byte> nonce,
            UnsafeBufferPointer<byte> ciphertext,
            UnsafeBufferPointer<byte> tag,
            UnsafeMutableBufferPointer<byte> plaintext,
            UnsafeBufferPointer<byte> aad,
            out SwiftError error);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial void AppleCryptoNative_AesGcmEncrypt(
            UnsafeBufferPointer<byte> key,
            UnsafeBufferPointer<byte> nonce,
            UnsafeBufferPointer<byte> plaintext,
            UnsafeMutableBufferPointer<byte> ciphertext,
            UnsafeMutableBufferPointer<byte> tag,
            UnsafeBufferPointer<byte> aad,
            out SwiftError error);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial void AppleCryptoNative_AesGcmDecrypt(
            UnsafeBufferPointer<byte> key,
            UnsafeBufferPointer<byte> nonce,
            UnsafeBufferPointer<byte> ciphertext,
            UnsafeBufferPointer<byte> tag,
            UnsafeMutableBufferPointer<byte> plaintext,
            UnsafeBufferPointer<byte> aad,
            out SwiftError error);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvSwift) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static unsafe partial bool AppleCryptoNative_IsAuthenticationFailure(void* error);
    }
}
