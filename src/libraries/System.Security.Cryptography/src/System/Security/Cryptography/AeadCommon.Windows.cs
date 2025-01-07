// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;
using Internal.NativeCrypto;
using static Interop.BCrypt;

namespace System.Security.Cryptography
{
    internal static partial class AeadCommon
    {
        public static unsafe void Encrypt(
            SafeKeyHandle keyHandle,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag)
        {
            // bcrypt sometimes misbehaves when given nullptr buffers; ensure non-nullptr
            fixed (byte* plaintextBytes = &Helpers.GetNonNullPinnableReference(plaintext))
            fixed (byte* nonceBytes = &Helpers.GetNonNullPinnableReference(nonce))
            fixed (byte* ciphertextBytes = &Helpers.GetNonNullPinnableReference(ciphertext))
            fixed (byte* tagBytes = &Helpers.GetNonNullPinnableReference(tag))
            fixed (byte* associatedDataBytes = &Helpers.GetNonNullPinnableReference(associatedData))
            {
                BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO authInfo = BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Create();
                authInfo.pbNonce = nonceBytes;
                authInfo.cbNonce = nonce.Length;
                authInfo.pbTag = tagBytes;
                authInfo.cbTag = tag.Length;
                authInfo.pbAuthData = associatedDataBytes;
                authInfo.cbAuthData = associatedData.Length;

                NTSTATUS ntStatus = BCryptEncrypt(
                    keyHandle,
                    plaintextBytes,
                    plaintext.Length,
                    new IntPtr(&authInfo),
                    null,
                    0,
                    ciphertextBytes,
                    ciphertext.Length,
                    out int ciphertextBytesWritten,
                    0);

                Debug.Assert(plaintext.Length == ciphertextBytesWritten);

                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw CreateCryptographicException(ntStatus);
                }
            }
        }

        public static unsafe void Decrypt(
            SafeKeyHandle keyHandle,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            bool clearPlaintextOnFailure)
        {
            // bcrypt sometimes misbehaves when given nullptr buffers; ensure non-nullptr
            fixed (byte* plaintextBytes = &Helpers.GetNonNullPinnableReference(plaintext))
            fixed (byte* nonceBytes = &Helpers.GetNonNullPinnableReference(nonce))
            fixed (byte* ciphertextBytes = &Helpers.GetNonNullPinnableReference(ciphertext))
            fixed (byte* tagBytes = &Helpers.GetNonNullPinnableReference(tag))
            fixed (byte* associatedDataBytes = &Helpers.GetNonNullPinnableReference(associatedData))
            {
                BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO authInfo = BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Create();
                authInfo.pbNonce = nonceBytes;
                authInfo.cbNonce = nonce.Length;
                authInfo.pbTag = tagBytes;
                authInfo.cbTag = tag.Length;
                authInfo.pbAuthData = associatedDataBytes;
                authInfo.cbAuthData = associatedData.Length;

                NTSTATUS ntStatus = BCryptDecrypt(
                    keyHandle,
                    ciphertextBytes,
                    ciphertext.Length,
                    new IntPtr(&authInfo),
                    null,
                    0,
                    plaintextBytes,
                    plaintext.Length,
                    out int plaintextBytesWritten,
                    0);

                Debug.Assert(ciphertext.Length == plaintextBytesWritten);

                switch (ntStatus)
                {
                    case NTSTATUS.STATUS_SUCCESS:
                        return;
                    case NTSTATUS.STATUS_AUTH_TAG_MISMATCH:
                        if (clearPlaintextOnFailure)
                        {
                            CryptographicOperations.ZeroMemory(plaintext);
                        }

                        throw new AuthenticationTagMismatchException();
                    default:
                        throw CreateCryptographicException(ntStatus);
                }
            }
        }
    }
}
