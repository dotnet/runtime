// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        internal static unsafe int AesKeyWrapEncrypt(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext)
        {
            fixed (byte* keyPtr = key)
            fixed (byte* plaintextPtr = plaintext)
            fixed (byte* ciphertextPtr = ciphertext)
            {
                int written = AppleCryptoNative_AesKeyWrapEncrypt(
                    new UnsafeBufferPointer<byte>(keyPtr, key.Length),
                    new UnsafeBufferPointer<byte>(plaintextPtr, plaintext.Length),
                    new UnsafeMutableBufferPointer<byte>(ciphertextPtr, ciphertext.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
                    CryptographicOperations.ZeroMemory(ciphertext);
                    throw new CryptographicException();
                }

                return written;
            }
        }

        internal static unsafe int AesKeyWrapDecrypt(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext)
        {
            fixed (byte* keyPtr = key)
            fixed (byte* ciphertextPtr = ciphertext)
            fixed (byte* plaintextPtr = plaintext)
            {
                int written = AppleCryptoNative_AesKeyWrapDecrypt(
                    new UnsafeBufferPointer<byte>(keyPtr, key.Length),
                    new UnsafeBufferPointer<byte>(ciphertextPtr, ciphertext.Length),
                    new UnsafeMutableBufferPointer<byte>(plaintextPtr, plaintext.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                    throw new CryptographicException();
                }

                return written;
            }
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial int AppleCryptoNative_AesKeyWrapEncrypt(
            UnsafeBufferPointer<byte> key,
            UnsafeBufferPointer<byte> plaintext,
            UnsafeMutableBufferPointer<byte> ciphertext,
            out SwiftError error);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial int AppleCryptoNative_AesKeyWrapDecrypt(
            UnsafeBufferPointer<byte> key,
            UnsafeBufferPointer<byte> ciphertext,
            UnsafeMutableBufferPointer<byte> plaintext,
            out SwiftError error);
    }
}
