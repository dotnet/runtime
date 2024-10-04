// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using Swift;

using AesGcm = Swift.AesGcm;

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

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("tvos13.0")]
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
                Data symmetricKeyData = new Data(keyPtr, key.Length);
                SymmetricKey symmetricKey = new SymmetricKey(symmetricKeyData);

                Data nonceData = new Data(noncePtr, nonce.Length);
                ChaChaPoly.Nonce chaChaPolyNonce = new ChaChaPoly.Nonce(nonceData);

                Data plaintextData = new Data(plaintextPtr, plaintext.Length);
                Data aadData = new Data(aadPtr, aad.Length);

                ChaChaPoly.SealedBox sealedBox = ChaChaPoly.seal(
                    plaintextData,
                    symmetricKey,
                    chaChaPolyNonce,
                    aadData,
                    out SwiftError error);

                if (error.Value != null)
                {
                    chaChaPolyNonce.Dispose();
                    symmetricKey.Dispose();

                    CryptographicOperations.ZeroMemory(ciphertext);
                    CryptographicOperations.ZeroMemory(tag);
                    throw new CryptographicException();
                }

                Data resultCiphertext = sealedBox.Ciphertext;
                Data resultTag = sealedBox.Tag;

                resultCiphertext.CopyBytes(ciphertextPtr, resultCiphertext.Count);
                resultTag.CopyBytes(tagPtr, resultTag.Count);
            }
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("tvos13.0")]
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
                Data symmetricKeyData = new Data(keyPtr, key.Length);
                SymmetricKey symmetricKey = new SymmetricKey(symmetricKeyData);

                Data nonceData = new Data(noncePtr, nonce.Length);
                ChaChaPoly.Nonce chaChaPolyNonce = new ChaChaPoly.Nonce(nonceData);

                Data ciphertextData = new Data(ciphertextPtr, ciphertext.Length);
                Data tagData = new Data(tagPtr, tag.Length);
                Data aadData = new Data(aadPtr, aad.Length);

                ChaChaPoly.SealedBox sealedBox = new ChaChaPoly.SealedBox(chaChaPolyNonce, ciphertextData, tagData);

                Data data = ChaChaPoly.open(
                    sealedBox,
                    symmetricKey,
                    aadData,
                    out SwiftError error);

                if (error.Value != null)
                {
                    chaChaPolyNonce.Dispose();
                    symmetricKey.Dispose();

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

                data.CopyBytes(plaintextPtr, data.Count);
            }
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("tvos13.0")]
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
                Data symmetricKeyData = new Data(keyPtr, key.Length);
                SymmetricKey symmetricKey = new SymmetricKey(symmetricKeyData);

                Data nonceData = new Data(noncePtr, nonce.Length);
                AesGcm.Nonce aesGcmNonce = new AesGcm.Nonce(nonceData);

                Data plaintextData = new Data(plaintextPtr, plaintext.Length);
                Data aadData = new Data(aadPtr, aad.Length);

                AesGcm.SealedBox sealedBox = AesGcm.seal(
                    plaintextData,
                    symmetricKey,
                    aesGcmNonce,
                    aadData,
                    out SwiftError error);

                if (error.Value != null)
                {
                    sealedBox.Dispose();
                    aesGcmNonce.Dispose();
                    symmetricKey.Dispose();

                    CryptographicOperations.ZeroMemory(ciphertext);
                    CryptographicOperations.ZeroMemory(tag);
                    throw new CryptographicException();
                }

                Data resultCiphertext = sealedBox.Ciphertext;
                Data resultTag = sealedBox.Tag;

                resultCiphertext.CopyBytes(ciphertextPtr, resultCiphertext.Count);
                resultTag.CopyBytes(tagPtr, resultTag.Count);
            }
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("tvos13.0")]
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
                Data symmetricKeyData = new Data(keyPtr, key.Length);
                SymmetricKey symmetricKey = new SymmetricKey(symmetricKeyData);

                Data nonceData = new Data(noncePtr, nonce.Length);
                AesGcm.Nonce aesGcmNonce = new AesGcm.Nonce(nonceData);

                Data ciphertextData = new Data(ciphertextPtr, ciphertext.Length);
                Data tagData = new Data(tagPtr, tag.Length);
                Data aadData = new Data(aadPtr, aad.Length);

                AesGcm.SealedBox sealedBox = new AesGcm.SealedBox(aesGcmNonce, ciphertextData, tagData);

                Data data = AesGcm.open(
                    sealedBox,
                    symmetricKey,
                    aadData,
                    out SwiftError error);

                if (error.Value != null)
                {
                    sealedBox.Dispose();
                    aesGcmNonce.Dispose();
                    symmetricKey.Dispose();

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

                data.CopyBytes(plaintextPtr, data.Count);
            }
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvSwift) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static unsafe partial bool AppleCryptoNative_IsAuthenticationFailure(void* error);
    }
}
