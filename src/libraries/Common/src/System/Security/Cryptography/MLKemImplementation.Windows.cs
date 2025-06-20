// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

using NTSTATUS = Interop.BCrypt.NTSTATUS;
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
using KeyBlobType = Interop.BCrypt.KeyBlobType;
using BCRYPT_MLKEM_KEY_BLOB = Interop.BCrypt.BCRYPT_MLKEM_KEY_BLOB;

namespace System.Security.Cryptography
{
    internal sealed partial class MLKemImplementation : MLKem
    {
        private static readonly SafeBCryptAlgorithmHandle? s_algHandle = OpenAlgorithmHandle();
        private readonly bool _hasSeed;
        private readonly bool _hasDecapsulationKey;
        private SafeBCryptKeyHandle _key;

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static new bool IsSupported => s_algHandle is not null;

        private MLKemImplementation(MLKemAlgorithm algorithm, SafeBCryptKeyHandle key, bool hasSeed, bool hasDecapsulationKey)
            : base(algorithm)
        {
            _key = key;
            _hasSeed = hasSeed;
            _hasDecapsulationKey = hasDecapsulationKey;
        }

        internal static MLKemImplementation GenerateKeyImpl(MLKemAlgorithm algorithm)
        {
            Debug.Assert(IsSupported);
            string parameterSet = PqcBlobHelpers.GetMLKemParameterSet(algorithm);
            SafeBCryptKeyHandle keyHandle = Interop.BCrypt.BCryptGenerateKeyPair(s_algHandle, keyLength: 0);

            try
            {
                Interop.BCrypt.BCryptSetSZProperty(keyHandle, Interop.BCrypt.BCryptPropertyStrings.BCRYPT_PARAMETER_SET_NAME, parameterSet);
                Interop.BCrypt.BCryptFinalizeKeyPair(keyHandle);
            }
            catch
            {
                keyHandle.Dispose();
                throw;
            }

            return new MLKemImplementation(algorithm, keyHandle, hasSeed: true, hasDecapsulationKey: true);
        }

        internal static MLKemImplementation ImportPrivateSeedImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.PrivateSeedSizeInBytes);
            SafeBCryptKeyHandle key = ImportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_SEED_MAGIC, algorithm, source);
            return new MLKemImplementation(algorithm, key, hasSeed: true, hasDecapsulationKey: true);
        }

        internal static MLKemImplementation ImportDecapsulationKeyImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.DecapsulationKeySizeInBytes);
            SafeBCryptKeyHandle key = ImportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_MAGIC, algorithm, source);
            return new MLKemImplementation(algorithm, key, hasSeed: false, hasDecapsulationKey: true);
        }

        internal static MLKemImplementation ImportEncapsulationKeyImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.EncapsulationKeySizeInBytes);
            SafeBCryptKeyHandle key = ImportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC, algorithm, source);
            return new MLKemImplementation(algorithm, key, hasSeed: false, hasDecapsulationKey: false);
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(ciphertext.Length == Algorithm.CiphertextSizeInBytes);
            Debug.Assert(sharedSecret.Length == Algorithm.SharedSecretSizeInBytes);

            ThrowIfNoDecapsulationKey(_hasDecapsulationKey);

            uint written = Interop.BCrypt.BCryptDecapsulate(_key, ciphertext, sharedSecret, 0);
            Debug.Assert(written == (uint)sharedSecret.Length);
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(ciphertext.Length == Algorithm.CiphertextSizeInBytes);
            Debug.Assert(sharedSecret.Length == Algorithm.SharedSecretSizeInBytes);
            Interop.BCrypt.BCryptEncapsulate(
                _key,
                sharedSecret,
                ciphertext,
                out uint sharedSecretWritten,
                out uint ciphertextWritten,
                0);
            Debug.Assert(sharedSecretWritten == (uint)sharedSecret.Length);
            Debug.Assert(ciphertextWritten == (uint)ciphertext.Length);
        }

        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == Algorithm.PrivateSeedSizeInBytes);

            ThrowIfNoSeed(_hasSeed);
            ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_SEED_MAGIC, destination);
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == Algorithm.DecapsulationKeySizeInBytes);

            ThrowIfNoDecapsulationKey(_hasDecapsulationKey);
            ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_MAGIC, destination);
        }

        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == Algorithm.EncapsulationKeySizeInBytes);
            ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC, destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            return MLKemPkcs8.TryExportPkcs8PrivateKey(
                this,
                _hasSeed,
                _hasDecapsulationKey,
                destination,
                out bytesWritten);
        }

        private static SafeBCryptAlgorithmHandle? OpenAlgorithmHandle()
        {
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }
#endif

            NTSTATUS status = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                out SafeBCryptAlgorithmHandle hAlgorithm,
                BCryptNative.AlgorithmName.MLKem,
                pszImplementation: null,
                Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                hAlgorithm.Dispose();
                return null;
            }
            else
            {
                return hAlgorithm;
            }
        }

        private static SafeBCryptKeyHandle ImportKey(KeyBlobMagicNumber kind, MLKemAlgorithm algorithm, ReadOnlySpan<byte> key)
        {
            checked
            {
                Debug.Assert(IsSupported);
                // ML-KEM 1024 seeds are 86 byte blobs. Round it off to 128.
                // Other keys like encapsulation or decapsulation keys will never fit in a stack buffer, so don't
                // try to accommodate them.
                const int MaxKeyStackSize = 128;
                string parameterSet = PqcBlobHelpers.GetMLKemParameterSet(algorithm);
                int blobHeaderSize = Marshal.SizeOf<BCRYPT_MLKEM_KEY_BLOB>();
                int parameterSetMarshalLength = ((parameterSet.Length + 1) * 2);
                int blobSize =
                    blobHeaderSize +
                    parameterSetMarshalLength +
                    key.Length;

                byte[]? rented = null;
                Span<byte> buffer = (uint)blobSize <= MaxKeyStackSize ?
                    stackalloc byte[MaxKeyStackSize] :
                    (rented = CryptoPool.Rent(blobSize));

                try
                {
                    buffer.Clear();

                    unsafe
                    {
                        fixed (byte* pBuffer = buffer)
                        {
                            BCRYPT_MLKEM_KEY_BLOB* blob = (BCRYPT_MLKEM_KEY_BLOB*)pBuffer;
                            blob->dwMagic = kind;
                            blob->cbParameterSet = (uint)parameterSetMarshalLength;
                            blob->cbKey = (uint)key.Length;
                        }
                    }

                    // This won't write the null byte, but we zeroed the whole buffer earlier.
                    Encoding.Unicode.GetBytes(parameterSet, buffer.Slice(blobHeaderSize));
                    key.CopyTo(buffer.Slice(blobHeaderSize + parameterSetMarshalLength));
                    string blobKind = PqcBlobHelpers.MLKemBlobMagicToBlobType(kind);
                    return Interop.BCrypt.BCryptImportKeyPair(
                        s_algHandle,
                        blobKind,
                        buffer.Slice(0, blobSize));
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(buffer.Slice(0, blobSize));

                    if (rented is not null)
                    {
                        // buffer is a slice of rented which was zeroed, since it needs to be zeroed regardless of being
                        // a rent or a stack buffer.
                        CryptoPool.Return(rented, 0);
                    }
                }
            }
        }

        private void ExportKey(KeyBlobMagicNumber kind, Span<byte> destination)
        {
            string blobKind = PqcBlobHelpers.MLKemBlobMagicToBlobType(kind);
            ArraySegment<byte> exported = Interop.BCrypt.BCryptExportKey(_key, blobKind);

            try
            {
                Span<byte> exportedSpan = exported;

                unsafe
                {
                    fixed (byte* pExportedSpan = exportedSpan)
                    {
                        BCRYPT_MLKEM_KEY_BLOB* blob = (BCRYPT_MLKEM_KEY_BLOB*)pExportedSpan;

                        if (blob->dwMagic != kind)
                        {
                            Debug.Fail("dwMagic is not expected value");
                            throw new CryptographicException();
                        }

                        int blobHeaderSize = Marshal.SizeOf<BCRYPT_MLKEM_KEY_BLOB>();
                        int keySize = checked((int)blob->cbKey);

                        if (keySize != destination.Length)
                        {
                            throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                        }

                        int paramSetSize = checked((int)blob->cbParameterSet);
                        ReadOnlySpan<char> paramSetWithNull = new(pExportedSpan + blobHeaderSize, paramSetSize / sizeof(char));
                        ReadOnlySpan<char> paramSet = paramSetWithNull[0..^1];
                        ReadOnlySpan<char> expectedParamSet = PqcBlobHelpers.GetMLKemParameterSet(Algorithm);

                        if (!paramSet.SequenceEqual(expectedParamSet) || paramSetWithNull[^1] != '\0')
                        {
                            throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                        }

                        exportedSpan.Slice(blobHeaderSize + paramSetSize, keySize).CopyTo(destination);
                    }
                }
            }
            finally
            {
                CryptoPool.Return(exported);
            }
        }
    }
}
