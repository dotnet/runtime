// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using BCryptBuffer = Interop.BCrypt.BCryptBuffer;
using CngBufferDescriptors = Interop.BCrypt.CngBufferDescriptors;
using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    internal sealed partial class SP800108HmacCounterKdfImplementationCng : SP800108HmacCounterKdfImplementationBase
    {
        private const string BCRYPT_SP800108_CTR_HMAC_ALGORITHM = "SP800_108_CTR_HMAC";
        private const nuint BCRYPT_SP800108_CTR_HMAC_ALG_HANDLE = 0x00000341;
        private const int CharToBytesStackBufferSize = 256;

        // A cached algorithm handle. On Windows 10 this is null if we are using a psuedo handle.
        private static readonly SafeBCryptAlgorithmHandle? s_sp800108CtrHmacAlgorithmHandle = OpenAlgorithmHandle();

        private readonly SafeBCryptKeyHandle _keyHandle;
        private readonly HashAlgorithmName _hashAlgorithm;

        public override void Dispose()
        {
            _keyHandle.Dispose();
        }

        internal override void DeriveBytes(byte[] label, byte[] context, Span<byte> destination)
        {
            DeriveBytes(new ReadOnlySpan<byte>(label), new ReadOnlySpan<byte>(context), destination);
        }

        internal override unsafe void DeriveBytes(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            if (destination.Length == 0)
            {
                return;
            }

            Debug.Assert(destination.Length <= 0x1FFFFFFF);
            Debug.Assert(_hashAlgorithm.Name is not null);

            fixed (byte* pLabel = label)
            fixed (byte* pContext = context)
            fixed (byte* pDestination = destination)
            fixed (char* pHashAlgorithm = _hashAlgorithm.Name)
            {
                const int BCryptBufferLength = 3;
                BCryptBuffer* buffers = stackalloc BCryptBuffer[BCryptBufferLength];

                buffers[0].BufferType = CngBufferDescriptors.KDF_LABEL;
                buffers[0].pvBuffer = (IntPtr)pLabel;
                buffers[0].cbBuffer = label.Length;
                buffers[1].BufferType = CngBufferDescriptors.KDF_CONTEXT;
                buffers[1].pvBuffer = (IntPtr)pContext;
                buffers[1].cbBuffer = context.Length;
                buffers[2].BufferType = CngBufferDescriptors.KDF_HASH_ALGORITHM;
                buffers[2].pvBuffer = (IntPtr)pHashAlgorithm;
                buffers[2].cbBuffer = (_hashAlgorithm.Name.Length + 1) * 2; // +1 for the null terminator.

                Interop.BCrypt.BCryptBufferDesc bufferDesc;
                bufferDesc.ulVersion = Interop.BCrypt.BCRYPTBUFFER_VERSION;
                bufferDesc.cBuffers = BCryptBufferLength;
                bufferDesc.pBuffers = (IntPtr)buffers;

                NTSTATUS deriveStatus = Interop.BCrypt.BCryptKeyDerivation(
                    _keyHandle,
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
                    Debug.Fail("BCryptKeyDerivation resultLength != destination.Length");
                    throw new CryptographicException();
                }
            }
        }

        internal override void DeriveBytes(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
        {
            using (Utf8DataEncoding labelData = new Utf8DataEncoding(label, stackalloc byte[CharToBytesStackBufferSize]))
            using (Utf8DataEncoding contextData = new Utf8DataEncoding(context, stackalloc byte[CharToBytesStackBufferSize]))
            {
                DeriveBytes(labelData.Utf8Bytes, contextData.Utf8Bytes, destination);
            }
        }

        internal static void DeriveBytesOneShot(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> context,
            Span<byte> destination)
        {
            Debug.Assert(destination.Length <= 0x1FFFFFFF);

            using (SP800108HmacCounterKdfImplementationCng kdf = new SP800108HmacCounterKdfImplementationCng(key, hashAlgorithm))
            {
                kdf.DeriveBytes(label, context, destination);
            }
        }

        internal static void DeriveBytesOneShot(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<char> label,
            ReadOnlySpan<char> context,
            Span<byte> destination)
        {
            if (destination.Length == 0)
            {
                return;
            }

            using (Utf8DataEncoding labelData = new Utf8DataEncoding(label, stackalloc byte[CharToBytesStackBufferSize]))
            using (Utf8DataEncoding contextData = new Utf8DataEncoding(context, stackalloc byte[CharToBytesStackBufferSize]))
            {
                DeriveBytesOneShot(key, hashAlgorithm, labelData.Utf8Bytes, contextData.Utf8Bytes, destination);
            }
        }

        private static unsafe SafeBCryptKeyHandle CreateSymmetricKey(byte* symmetricKey, int symmetricKeyLength)
        {
            NTSTATUS generateKeyStatus;
            SafeBCryptKeyHandle keyHandle;

            if (s_sp800108CtrHmacAlgorithmHandle is not null)
            {
                generateKeyStatus = Interop.BCrypt.BCryptGenerateSymmetricKey(
                    s_sp800108CtrHmacAlgorithmHandle,
                    out keyHandle,
                    pbKeyObject: IntPtr.Zero,
                    cbKeyObject: 0,
                    symmetricKey,
                    symmetricKeyLength,
                    dwFlags: 0);
            }
            else
            {
                generateKeyStatus = Interop.BCrypt.BCryptGenerateSymmetricKey(
                    BCRYPT_SP800108_CTR_HMAC_ALG_HANDLE,
                    out keyHandle,
                    pbKeyObject: IntPtr.Zero,
                    cbKeyObject: 0,
                    symmetricKey,
                    symmetricKeyLength,
                    dwFlags: 0);
            }

            if (generateKeyStatus != NTSTATUS.STATUS_SUCCESS)
            {
                keyHandle.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(generateKeyStatus);
            }

            Debug.Assert(!keyHandle.IsInvalid);

            return keyHandle;
        }

        // Returns null if the platform is Windows 10+ and psuedo handles should be used.
        private static SafeBCryptAlgorithmHandle? OpenAlgorithmHandle()
        {
            if (!Interop.BCrypt.PseudoHandlesSupported)
            {
                NTSTATUS openStatus = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                    out SafeBCryptAlgorithmHandle sp800108CtrHmacAlgorithmHandle,
                    BCRYPT_SP800108_CTR_HMAC_ALGORITHM,
                    null,
                    Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);

                if (openStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    sp800108CtrHmacAlgorithmHandle.Dispose();
                    throw Interop.BCrypt.CreateCryptographicException(openStatus);
                }

                return sp800108CtrHmacAlgorithmHandle;
            }

            return null;
        }

        private static int GetHashBlockSize(string hashAlgorithmName)
        {
            // Block sizes per NIST FIPS pub 180-4 and FIPS 202.
            switch (hashAlgorithmName)
            {
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                    return 512 / 8;
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                    return 1024 / 8;
                case HashAlgorithmNames.SHA3_256:
                    return 1088 / 8;
                case HashAlgorithmNames.SHA3_384:
                    return 832 / 8;
                case HashAlgorithmNames.SHA3_512:
                    return 576 / 8;
                default:
                    Debug.Fail($"Unexpected hash algorithm '{hashAlgorithmName}'");
                    throw new CryptographicException();
            }
        }
    }
}
