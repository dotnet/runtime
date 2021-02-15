// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using BCryptAlgPseudoHandle = Interop.BCrypt.BCryptAlgPseudoHandle;
using BCryptBuffer = Interop.BCrypt.BCryptBuffer;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using NCryptBCryptBufferDescriptors = Interop.BCrypt.NCryptBCryptBufferDescriptors;
using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace Internal.Cryptography
{
    internal partial class Pbkdf2Implementation
    {
        private static readonly bool s_usePseudoHandles = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 0);

        // For Windows 7 we will use BCryptDeriveKeyPBKDF2. For Windows 8 we will use BCryptDeriveKey
        // since it has better performance.
        private static readonly bool s_useKeyDerivation = OperatingSystem.IsWindowsVersionAtLeast(8, 0, 0);

        private static volatile SafeBCryptAlgorithmHandle? s_pbkdf2AlgorithmHandle;

        public static unsafe void Fill(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithmName,
            Span<byte> destination)
        {
            Debug.Assert(!destination.IsEmpty);
            Debug.Assert(iterations >= 0);
            Debug.Assert(hashAlgorithmName.Name is not null);

            if (s_useKeyDerivation)
            {
                FillKeyDerivation(password, salt, iterations, hashAlgorithmName.Name, destination);
            }
            else
            {
                FillDeriveKeyPBKDF2(password, salt, iterations, hashAlgorithmName.Name, destination);
            }
        }

        private static unsafe void FillKeyDerivation(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            string hashAlgorithmName,
            Span<byte> destination)
        {
            SafeBCryptKeyHandle keyHandle;
            int hashBlockSizeBytes = GetHashBlockSize(hashAlgorithmName);

            // stackalloc 0 to let compiler know this cannot escape.
            ReadOnlySpan<byte> symmetricKeyMaterial = stackalloc byte[0];
            int symmetricKeyMaterialLength;
            Span<byte> clearSpan = stackalloc byte[0];

            if (password.IsEmpty)
            {
                symmetricKeyMaterial = stackalloc byte[1];
                symmetricKeyMaterialLength = 0;
                clearSpan = default;
            }
            else if (password.Length <= hashBlockSizeBytes)
            {
                symmetricKeyMaterial = password;
                symmetricKeyMaterialLength = password.Length;
                clearSpan = default;
            }
            else
            {
                // RFC 2104: "The key for HMAC can be of any length (keys longer than B bytes are
                //     first hashed using H).
                //     We denote by B the byte-length of such
                //     blocks (B=64 for all the above mentioned examples of hash functions)
                //
                // Windows' PBKDF2 will do this up to a point. To ensure we accept arbitrary inputs for
                // PBKDF2, we do the hashing ourselves.
                Span<byte> hashBuffer = stackalloc byte[512 / 8]; // 64 bytes is SHA512, the largest digest handled.
                int hashBufferSize;

                switch (hashAlgorithmName)
                {
                    case HashAlgorithmNames.SHA1:
                        hashBufferSize = SHA1.HashData(password, hashBuffer);
                        break;
                    case HashAlgorithmNames.SHA256:
                        hashBufferSize = SHA256.HashData(password, hashBuffer);
                        break;
                    case HashAlgorithmNames.SHA384:
                        hashBufferSize = SHA384.HashData(password, hashBuffer);
                        break;
                    case HashAlgorithmNames.SHA512:
                        hashBufferSize = SHA512.HashData(password, hashBuffer);
                        break;
                    default:
                        throw new CryptographicException();
                }

                clearSpan = hashBuffer.Slice(0, hashBufferSize);
                symmetricKeyMaterial = clearSpan;
                symmetricKeyMaterialLength = symmetricKeyMaterial.Length;
            }

            if (s_usePseudoHandles)
            {
                fixed (byte* pSymmetricKeyMaterial = symmetricKeyMaterial)
                {
                    NTSTATUS generateKeyStatus = Interop.BCrypt.BCryptGenerateSymmetricKey(
                        (nuint)BCryptAlgPseudoHandle.BCRYPT_PBKDF2_ALG_HANDLE,
                        out keyHandle,
                        pbKeyObject: IntPtr.Zero,
                        cbKeyObject: 0,
                        pSymmetricKeyMaterial,
                        symmetricKeyMaterialLength,
                        dwFlags: 0);

                    CryptographicOperations.ZeroMemory(clearSpan);

                    if (generateKeyStatus != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw Interop.BCrypt.CreateCryptographicException(generateKeyStatus);
                    }
                }
            }
            else
            {
                if (s_pbkdf2AlgorithmHandle is null)
                {
                    NTSTATUS openStatus = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                        out SafeBCryptAlgorithmHandle pbkdf2AlgorithmHandle,
                        "PBKDF2",
                        null,
                        BCryptOpenAlgorithmProviderFlags.None);

                    if (openStatus != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw Interop.BCrypt.CreateCryptographicException(openStatus);
                    }

                    // This might race on the null check above, and that's okay. Worst
                    // case the algorithm is opened more than once, and they will get
                    // cleaned up during collection.
                    s_pbkdf2AlgorithmHandle = pbkdf2AlgorithmHandle;
                }

                fixed (byte* pSymmetricKeyMaterial = symmetricKeyMaterial)
                {
                    NTSTATUS generateKeyStatus = Interop.BCrypt.BCryptGenerateSymmetricKey(
                        s_pbkdf2AlgorithmHandle,
                        out keyHandle,
                        pbKeyObject: IntPtr.Zero,
                        cbKeyObject: 0,
                        pSymmetricKeyMaterial,
                        symmetricKeyMaterialLength,
                        dwFlags: 0);

                    CryptographicOperations.ZeroMemory(clearSpan);

                    if (generateKeyStatus != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw Interop.BCrypt.CreateCryptographicException(generateKeyStatus);
                    }
                }
            }

            Debug.Assert(!keyHandle.IsInvalid);

            ulong kdfIterations = (ulong)iterations; // Previously asserted to be positive.

            using (keyHandle)
            fixed (char* pHashAlgorithmName = hashAlgorithmName)
            fixed (byte* pSalt = salt)
            fixed (byte* pDestination = destination)
            {
                BCryptBuffer* buffers = stackalloc BCryptBuffer[3];
                buffers[0].BufferType = NCryptBCryptBufferDescriptors.KDF_ITERATION_COUNT;
                buffers[0].pvBuffer = (IntPtr)(&kdfIterations);
                buffers[0].cbBuffer = sizeof(ulong);

                buffers[1].BufferType = NCryptBCryptBufferDescriptors.KDF_SALT;
                buffers[1].pvBuffer = (IntPtr)pSalt;
                buffers[1].cbBuffer = salt.Length;

                buffers[2].BufferType = NCryptBCryptBufferDescriptors.KDF_HASH_ALGORITHM;
                buffers[2].pvBuffer = (IntPtr)pHashAlgorithmName;

                // C# spec: "A char* value produced by fixing a string instance always points to a null-terminated string"
                buffers[2].cbBuffer = checked((hashAlgorithmName.Length + 1) * sizeof(char)); // Add null terminator.

                Interop.BCrypt.BCryptBufferDesc bufferDesc;
                bufferDesc.ulVersion = Interop.BCrypt.BCRYPTBUFFER_VERSION;
                bufferDesc.cBuffers = 3;
                bufferDesc.pBuffers = (IntPtr)buffers;

                NTSTATUS deriveStatus = Interop.BCrypt.BCryptKeyDerivation(
                    keyHandle,
                    &bufferDesc,
                    pDestination,
                    destination.Length,
                    out uint resultLength,
                    dwFlags: 0);

                if (deriveStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(deriveStatus);
                }

                if (destination.Length != resultLength)
                {
                    throw new CryptographicException();
                }
            }
        }

        private static unsafe void FillDeriveKeyPBKDF2(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            string hashAlgorithmName,
            Span<byte> destination)
        {
            const BCryptOpenAlgorithmProviderFlags OpenAlgorithmFlags = BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG;

            // This code path will only be taken on Windows 7, so we can assume pseudo handles are not supported.
            // Do not dispose handle since it is shared and cached.
            SafeBCryptAlgorithmHandle handle =
                Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgorithmName, OpenAlgorithmFlags, out _);

            fixed (byte* pPassword = password)
            fixed (byte* pSalt = salt)
            fixed (byte* pDestination = destination)
            {
                NTSTATUS status = Interop.BCrypt.BCryptDeriveKeyPBKDF2(
                    handle,
                    pPassword,
                    password.Length,
                    pSalt,
                    salt.Length,
                    (ulong)iterations,
                    pDestination,
                    destination.Length,
                    dwFlags: 0);

                if (status != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(status);
                }
            }
        }

        private static int GetHashBlockSize(string hashAlgorithmName) => hashAlgorithmName switch {
            // Block sizes per NIST FIPS pub 180-4.
            HashAlgorithmNames.SHA1 => 512,
            HashAlgorithmNames.SHA256 => 512,
            HashAlgorithmNames.SHA384 => 1024,
            HashAlgorithmNames.SHA512 => 1024,
            _ => throw new CryptographicException(), // Should have been validated before getting here.
        };
    }
}
