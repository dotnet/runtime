// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

using BCryptAlgPseudoHandle = Interop.BCrypt.BCryptAlgPseudoHandle;
using BCryptBuffer = Interop.BCrypt.BCryptBuffer;
using BCryptBufferDesc = Interop.BCrypt.BCryptBufferDesc;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using BCRYPT_KEY_DATA_BLOB_HEADER = Interop.BCrypt.BCRYPT_KEY_DATA_BLOB_HEADER;
using CngBufferDescriptors = Interop.BCrypt.CngBufferDescriptors;
using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    public static partial class HKDF
    {
        private static readonly bool s_hasCngImplementation = IsCngSupported();
        private const string BCRYPT_HKDF_SALT_AND_FINALIZE = "HkdfSaltAndFinalize";
        private const string BCRYPT_HKDF_PRK_AND_FINALIZE = "HkdfPrkAndFinalize";
        private const string BCRYPT_HKDF_HASH_ALGORITHM = "HkdfHashAlgorithm";

        private static void ExtractCore(
            HashAlgorithmName hashAlgorithmName,
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> salt,
            Span<byte> prk)
        {
            // Windows does not clearly have a way to perform just the Extact step from HKDF. So used managed, for now.
            HKDFManagedImplementation.Extract(hashAlgorithmName, ikm, salt, prk);
        }

        private static void Expand(
            HashAlgorithmName hashAlgorithmName,
            int hashLength,
            ReadOnlySpan<byte> prk,
            Span<byte> output,
            ReadOnlySpan<byte> info)
        {
            if (s_hasCngImplementation && !IsAlgorithmRequiringManagedFallback(hashAlgorithmName))
            {
                CngDeriveKey(
                    hashAlgorithmName,
                    prk,
                    info,
                    salt: default,
                    output,
                    keyObjectIsIkm: false);
            }
            else
            {
                HKDFManagedImplementation.Expand(hashAlgorithmName, hashLength, prk, output, info);
            }
        }

        private static void DeriveKeyCore(
            HashAlgorithmName hashAlgorithmName,
            int hashLength,
            ReadOnlySpan<byte> ikm,
            Span<byte> output,
            ReadOnlySpan<byte> salt,
            ReadOnlySpan<byte> info)
        {
            if (s_hasCngImplementation && !IsAlgorithmRequiringManagedFallback(hashAlgorithmName))
            {
                CngDeriveKey(
                    hashAlgorithmName,
                    ikm,
                    info,
                    salt,
                    output,
                    keyObjectIsIkm: true);
            }
            else
            {
                HKDFManagedImplementation.DeriveKey(hashAlgorithmName, hashLength, ikm, output, salt, info);
            }
        }

        private static bool IsAlgorithmRequiringManagedFallback(HashAlgorithmName hashAlgorithmName)
        {
            return hashAlgorithmName == HashAlgorithmName.MD5;
        }

        private static void ThrowIfAlgorithmNotSupported(HashAlgorithmName hashAlgorithmName)
        {
            if ((hashAlgorithmName == HashAlgorithmName.SHA3_256 && !SHA3_256.IsSupported) ||
                (hashAlgorithmName == HashAlgorithmName.SHA3_384 && !SHA3_384.IsSupported) ||
                (hashAlgorithmName == HashAlgorithmName.SHA3_512 && !SHA3_512.IsSupported))
            {
                throw new PlatformNotSupportedException();
            }
        }

        private static bool IsCngSupported()
        {
            NTSTATUS openStatus = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                out SafeBCryptAlgorithmHandle handle,
                Internal.NativeCrypto.BCryptNative.AlgorithmName.HKDF,
                null,
                BCryptOpenAlgorithmProviderFlags.None);

            handle.Dispose();

            // HKDF was added in Windows 10 1803.
            Debug.Assert(!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17134) || openStatus == NTSTATUS.STATUS_SUCCESS);
            return openStatus == NTSTATUS.STATUS_SUCCESS;
        }

        private static unsafe void CngDeriveKey(
            HashAlgorithmName hashAlgorithmName,
            ReadOnlySpan<byte> keyObject,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> salt,
            Span<byte> destination,
            bool keyObjectIsIkm)
        {
            Debug.Assert(Interop.BCrypt.PseudoHandlesSupported);
            Debug.Assert(hashAlgorithmName.Name is not null);

            ThrowIfAlgorithmNotSupported(hashAlgorithmName);

            byte[]? rented;
            ReadOnlySpan<byte> safeInfo;

            if (destination.Overlaps(info))
            {
                rented = CryptoPool.Rent(info.Length);
                info.CopyTo(rented);
                safeInfo = rented.AsSpan(0, info.Length);
            }
            else
            {
                rented = null;
                safeInfo = info;
            }

            SafeBCryptKeyHandle? keyHandle = null;
            NTSTATUS status;

            try
            {
                fixed (byte* pKeyObject = &Helpers.GetNonNullPinnableReference(keyObject))
                {
                    status = Interop.BCrypt.BCryptGenerateSymmetricKey(
                        (nuint)BCryptAlgPseudoHandle.BCRYPT_HKDF_ALG_HANDLE,
                        out keyHandle,
                        pbKeyObject: IntPtr.Zero,
                        cbKeyObject: 0,
                        pKeyObject,
                        keyObject.Length,
                        dwFlags: 0);

                    if (status != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw Interop.BCrypt.CreateCryptographicException(status);
                    }

                    Interop.BCrypt.BCryptSetSZProperty(keyHandle, BCRYPT_HKDF_HASH_ALGORITHM, hashAlgorithmName.Name);

                    if (keyObjectIsIkm)
                    {
                        fixed (byte* pSalt = &Helpers.GetNonNullPinnableReference(salt))
                        {
                            status = Interop.BCrypt.BCryptSetProperty(
                                keyHandle,
                                BCRYPT_HKDF_SALT_AND_FINALIZE,
                                pSalt,
                                (uint)salt.Length,
                                dwFlags: 0);
                        }
                    }
                    else
                    {
                        Debug.Assert(salt.IsEmpty);
                        status = Interop.BCrypt.BCryptSetProperty(
                            keyHandle,
                            BCRYPT_HKDF_PRK_AND_FINALIZE,
                            null,
                            0U,
                            dwFlags: 0);
                    }

                    if (status != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw Interop.BCrypt.CreateCryptographicException(status);
                    }
                }

                fixed (byte* pDestination = destination)
                fixed (byte* pSafeInfo = &Helpers.GetNonNullPinnableReference(safeInfo))
                {
                    BCryptBuffer infoBuffer = default;
                    infoBuffer.cbBuffer = safeInfo.Length;
                    infoBuffer.BufferType = CngBufferDescriptors.KDF_HKDF_INFO;
                    infoBuffer.pvBuffer = (IntPtr)pSafeInfo;

                    BCryptBufferDesc bufferDesc = default;
                    bufferDesc.ulVersion = Interop.BCrypt.BCRYPTBUFFER_VERSION;
                    bufferDesc.cBuffers = 1;
                    bufferDesc.pBuffers = (IntPtr)(&infoBuffer);

                    status = Interop.BCrypt.BCryptKeyDerivation(
                        keyHandle,
                        &bufferDesc,
                        pDestination,
                        destination.Length,
                        out uint resultLength,
                        dwFlags: 0);

                    if (status != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw Interop.BCrypt.CreateCryptographicException(status);
                    }

                    if (destination.Length != resultLength)
                    {
                        Debug.Fail("HKDF resultLength != destination.Length");
                        throw new CryptographicException();
                    }
                }
            }
            finally
            {
                if (rented is not null)
                {
                    CryptoPool.Return(rented, clearSize: 0); // Info is not considered secret.
                }

                keyHandle?.Dispose();
            }
        }
    }
}
