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
using Swift.Runtime;

using AesGcm = Swift.Runtime.AesGcm;

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
                var symmetricKeyData = new Data(keyPtr, key.Length);
                var swiftKey = new SymmetricKey(symmetricKeyData);

                var nonceData = new Data(noncePtr, nonce.Length);
                var swiftNonce = new ChaChaPoly.Nonce(nonceData);

                var plaintextData = new Data(plaintextPtr, plaintext.Length);
                var aadData = new Data(aadPtr, aad.Length);

                void* messagePtr = &plaintextData;
                void* authenticatedDataPtr = &aadData;

                var sealedBox = CryptoKit.PInvoke_ChaChaPoly_Seal(
                    messagePtr,
                    swiftKey.payload,
                    swiftNonce.payload,
                    authenticatedDataPtr,
                    plaintextData.Metadata(),
                    aadData.Metadata(),
                    default(DataProtocol).WitnessTable(plaintextData),
                    default(DataProtocol).WitnessTable(aadData),
                    out SwiftError error);

                if (error.Value != null)
                {
                    CryptographicOperations.ZeroMemory(ciphertext);
                    CryptographicOperations.ZeroMemory(tag);
                    throw new CryptographicException();
                }

                GC.KeepAlive(plaintextData);
                GC.KeepAlive(aadData);

                var resultCiphertext = sealedBox.Ciphertext;
                var resultTag = sealedBox.Tag;

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
                var symmetricKeyData = new Data(keyPtr, key.Length);
                var swiftKey = new SymmetricKey(symmetricKeyData);

                var nonceData = new Data(noncePtr, nonce.Length);
                var swiftNonce = new ChaChaPoly.Nonce(nonceData);

                var ciphertextData = new Data(ciphertextPtr, ciphertext.Length);
                var tagData = new Data(tagPtr, tag.Length);
                var aadData = new Data(aadPtr, aad.Length);

                var sealedBox = new ChaChaPoly.SealedBox(swiftNonce, ciphertextData, tagData);

                void* authenticatedDataPtr = &aadData;

                var data = CryptoKit.PInvoke_ChaChaPoly_Open(
                    sealedBox,
                    swiftKey.payload,
                    authenticatedDataPtr,
                    aadData.Metadata(),
                    default(DataProtocol).WitnessTable(aadData),
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

                GC.KeepAlive(aadData);

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
                var symmetricKeyData = new Data(keyPtr, key.Length);
                var swiftKey = new SymmetricKey(symmetricKeyData);

                var nonceData = new Data(noncePtr, nonce.Length);
                var swiftNonce = new AesGcm.Nonce(nonceData);

                var plaintextData = new Data(plaintextPtr, plaintext.Length);
                var aadData = new Data(aadPtr, aad.Length);

                void* messagePtr = &plaintextData;
                void* authenticatedDataPtr = &aadData;

                var sealedBox = new AesGcm.SealedBox();
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(sealedBox.payload);

                CryptoKit.PInvoke_AesGcm_Seal(
                    swiftIndirectResult,
                    messagePtr,
                    swiftKey.payload,
                    swiftNonce.payload,
                    authenticatedDataPtr,
                    plaintextData.Metadata(),
                    aadData.Metadata(),
                    default(DataProtocol).WitnessTable(plaintextData),
                    default(DataProtocol).WitnessTable(aadData),
                    out SwiftError error);

                if (error.Value != null)
                {
                    CryptographicOperations.ZeroMemory(ciphertext);
                    CryptographicOperations.ZeroMemory(tag);
                    throw new CryptographicException();
                }

                GC.KeepAlive(plaintextData);
                GC.KeepAlive(aadData);

                var resultCiphertext = sealedBox.Ciphertext;
                var resultTag = sealedBox.Tag;

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
                var symmetricKeyData = new Data(keyPtr, key.Length);
                var swiftKey = new SymmetricKey(symmetricKeyData);

                var nonceData = new Data(noncePtr, nonce.Length);
                var swiftNonce = new AesGcm.Nonce(nonceData);

                var ciphertextData = new Data(ciphertextPtr, ciphertext.Length);
                var tagData = new Data(tagPtr, tag.Length);
                var aadData = new Data(aadPtr, aad.Length);

                var sealedBox = new AesGcm.SealedBox(swiftNonce, ciphertextData, tagData);

                void* authenticatedDataPtr = &aadData;

                var data = CryptoKit.PInvoke_AesGcm_Open(
                    sealedBox.payload,
                    swiftKey.payload,
                    authenticatedDataPtr,
                    aadData.Metadata(),
                    default(DataProtocol).WitnessTable(aadData),
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

                GC.KeepAlive(aadData);

                data.CopyBytes(plaintextPtr, data.Count);
            }
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvSwift) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static unsafe partial bool AppleCryptoNative_IsAuthenticationFailure(void* error);
    }
}
