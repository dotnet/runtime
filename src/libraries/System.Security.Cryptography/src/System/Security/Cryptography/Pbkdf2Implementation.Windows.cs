// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using BCryptAlgPseudoHandle = Interop.BCrypt.BCryptAlgPseudoHandle;
using BCryptBuffer = Interop.BCrypt.BCryptBuffer;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using CngBufferDescriptors = Interop.BCrypt.CngBufferDescriptors;
using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    internal static partial class Pbkdf2Implementation
    {
        // For Windows 7 we will use BCryptDeriveKeyPBKDF2. For Windows 8+ (seen as version 6.2.0) we will
        // use BCryptKeyDerivation since it has better performance.
        private static readonly bool s_useKeyDerivation = OperatingSystem.IsWindowsVersionAtLeast(6, 2);

        // A cached instance of PBKDF2 for Windows 8, where pseudo handles are not supported.
        private static SafeBCryptAlgorithmHandle? s_pbkdf2AlgorithmHandle;

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
            scoped Span<byte> clearSpan;
            scoped ReadOnlySpan<byte> symmetricKeyMaterial;
            int symmetricKeyMaterialLength;

            if (password.IsEmpty)
            {
                // CNG won't accept a null pointer for the password.
                symmetricKeyMaterial = stackalloc byte[1];
                symmetricKeyMaterialLength = 0;
                clearSpan = default;
            }
            else if (password.Length <= hashBlockSizeBytes)
            {
                // Password is small enough to use as-is.
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
                        Debug.Fail($"Unexpected hash algorithm '{hashAlgorithmName}'");
                        throw new CryptographicException();
                }

                clearSpan = hashBuffer.Slice(0, hashBufferSize);
                symmetricKeyMaterial = clearSpan;
                symmetricKeyMaterialLength = hashBufferSize;
            }

            Debug.Assert(symmetricKeyMaterial.Length > 0);

            NTSTATUS generateKeyStatus;

            if (Interop.BCrypt.PseudoHandlesSupported)
            {
                fixed (byte* pSymmetricKeyMaterial = symmetricKeyMaterial)
                {
                    generateKeyStatus = Interop.BCrypt.BCryptGenerateSymmetricKey(
                        (nuint)BCryptAlgPseudoHandle.BCRYPT_PBKDF2_ALG_HANDLE,
                        out keyHandle,
                        pbKeyObject: IntPtr.Zero,
                        cbKeyObject: 0,
                        pSymmetricKeyMaterial,
                        symmetricKeyMaterialLength,
                        dwFlags: 0);
                }
            }
            else
            {
                if (s_pbkdf2AlgorithmHandle is null)
                {
                    NTSTATUS openStatus = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                        out SafeBCryptAlgorithmHandle pbkdf2AlgorithmHandle,
                        Internal.NativeCrypto.BCryptNative.AlgorithmName.Pbkdf2,
                        null,
                        BCryptOpenAlgorithmProviderFlags.None);

                    if (openStatus != NTSTATUS.STATUS_SUCCESS)
                    {
                        pbkdf2AlgorithmHandle.Dispose();
                        CryptographicOperations.ZeroMemory(clearSpan);
                        throw Interop.BCrypt.CreateCryptographicException(openStatus);
                    }

                    // This might race, and that's okay. Worst case the algorithm is opened
                    // more than once, and the ones that lost will get cleaned up during collection.
                    Interlocked.CompareExchange(ref s_pbkdf2AlgorithmHandle, pbkdf2AlgorithmHandle, null);
                }

                fixed (byte* pSymmetricKeyMaterial = symmetricKeyMaterial)
                {
                    generateKeyStatus = Interop.BCrypt.BCryptGenerateSymmetricKey(
                        s_pbkdf2AlgorithmHandle,
                        out keyHandle,
                        pbKeyObject: IntPtr.Zero,
                        cbKeyObject: 0,
                        pSymmetricKeyMaterial,
                        symmetricKeyMaterialLength,
                        dwFlags: 0);
                }
            }

            CryptographicOperations.ZeroMemory(clearSpan);

            if (generateKeyStatus != NTSTATUS.STATUS_SUCCESS)
            {
                keyHandle.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(generateKeyStatus);
            }

            Debug.Assert(!keyHandle.IsInvalid);

            ulong kdfIterations = (ulong)iterations; // Previously asserted to be positive.

            using (keyHandle)
            fixed (char* pHashAlgorithmName = hashAlgorithmName)
            fixed (byte* pSalt = salt)
            fixed (byte* pDestination = destination)
            {
                Span<BCryptBuffer> buffers = stackalloc BCryptBuffer[3];
                buffers[0].BufferType = CngBufferDescriptors.KDF_ITERATION_COUNT;
                buffers[0].pvBuffer = (IntPtr)(&kdfIterations);
                buffers[0].cbBuffer = sizeof(ulong);

                buffers[1].BufferType = CngBufferDescriptors.KDF_SALT;
                buffers[1].pvBuffer = (IntPtr)pSalt;
                buffers[1].cbBuffer = salt.Length;

                buffers[2].BufferType = CngBufferDescriptors.KDF_HASH_ALGORITHM;
                buffers[2].pvBuffer = (IntPtr)pHashAlgorithmName;

                // C# spec: "A char* value produced by fixing a string instance always points to a null-terminated string"
                buffers[2].cbBuffer = checked((hashAlgorithmName.Length + 1) * sizeof(char)); // Add null terminator.

                fixed (BCryptBuffer* pBuffers = buffers)
                {
                    Interop.BCrypt.BCryptBufferDesc bufferDesc;
                    bufferDesc.ulVersion = Interop.BCrypt.BCRYPTBUFFER_VERSION;
                    bufferDesc.cBuffers = buffers.Length;
                    bufferDesc.pBuffers = (IntPtr)pBuffers;

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
                        Debug.Fail("PBKDF2 resultLength != destination.Length");
                        throw new CryptographicException();
                    }
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

        private static int GetHashBlockSize(string hashAlgorithmName)
        {
            // Block sizes per NIST FIPS pub 180-4.
            switch (hashAlgorithmName)
            {
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                    return 512 / 8;
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                    return 1024 / 8;
                default:
                    Debug.Fail($"Unexpected hash algorithm '{hashAlgorithmName}'");
                    throw new CryptographicException();
            }
        }
    }
}
