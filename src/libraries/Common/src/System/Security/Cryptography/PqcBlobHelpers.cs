// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
using BCRYPT_PQDSA_KEY_BLOB = Interop.BCrypt.BCRYPT_PQDSA_KEY_BLOB;

namespace System.Security.Cryptography
{
    internal static partial class PqcBlobHelpers
    {
        internal const string BCRYPT_MLDSA_PARAMETER_SET_44 = "44";
        internal const string BCRYPT_MLDSA_PARAMETER_SET_65 = "65";
        internal const string BCRYPT_MLDSA_PARAMETER_SET_87 = "87";

        internal static string GetMLDsaParameterSet(MLDsaAlgorithm algorithm)
        {
            if (algorithm == MLDsaAlgorithm.MLDsa44)
            {
                return BCRYPT_MLDSA_PARAMETER_SET_44;
            }
            else if (algorithm == MLDsaAlgorithm.MLDsa65)
            {
                return BCRYPT_MLDSA_PARAMETER_SET_65;
            }
            else if (algorithm == MLDsaAlgorithm.MLDsa87)
            {
                return BCRYPT_MLDSA_PARAMETER_SET_87;
            }

            Debug.Fail($"Unknown MLDsaAlgorithm: {algorithm}");
            throw new PlatformNotSupportedException();
        }

        internal delegate TResult EncodeBlobFunc<TResult>(ReadOnlySpan<byte> blob);

        internal static TResult EncodeMLDsaBlob<TResult>(
            ReadOnlySpan<char> parameterSet,
            ReadOnlySpan<byte> data,
            string blobType,
            EncodeBlobFunc<TResult> callback)
        {
            KeyBlobMagicNumber magic;

            switch (blobType)
            {
                case Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB:
                    magic = KeyBlobMagicNumber.BCRYPT_MLDSA_PUBLIC_MAGIC;
                    break;
                case Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB:
                    magic = KeyBlobMagicNumber.BCRYPT_MLDSA_PRIVATE_MAGIC;
                    break;
                case Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB:
                    magic = KeyBlobMagicNumber.BCRYPT_MLDSA_PRIVATE_SEED_MAGIC;
                    break;
                default:
                    Debug.Fail("Unknown blob type.");
                    throw new CryptographicException();
            }

            return EncodePQDsaBlob(magic, parameterSet, data, callback);
        }

        internal static ReadOnlySpan<byte> DecodeMLDsaBlob(
            ReadOnlySpan<byte> blob,
            out ReadOnlySpan<char> parameterSet,
            out string blobType)
        {
            ReadOnlySpan<byte> data = DecodePQDsaBlob(blob, out KeyBlobMagicNumber magic, out parameterSet);

            switch (magic)
            {
                case KeyBlobMagicNumber.BCRYPT_MLDSA_PUBLIC_MAGIC:
                    blobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB;
                    break;
                case KeyBlobMagicNumber.BCRYPT_MLDSA_PRIVATE_MAGIC:
                    blobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB;
                    break;
                case KeyBlobMagicNumber.BCRYPT_MLDSA_PRIVATE_SEED_MAGIC:
                    blobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB;
                    break;
                default:
                    Debug.Fail("Unknown blob type.");
                    throw new CryptographicException();
            }

            return data;
        }

        private static TResult EncodePQDsaBlob<TResult>(
            KeyBlobMagicNumber magic,
            ReadOnlySpan<char> parameterSet,
            ReadOnlySpan<byte> data,
            EncodeBlobFunc<TResult> callback)
        {
            int blobHeaderSize = Unsafe.SizeOf<BCRYPT_PQDSA_KEY_BLOB>();
            int parameterSetLengthWithNullTerminator = checked(sizeof(char) * (parameterSet.Length + 1));

            int blobSize = checked(blobHeaderSize +
                                   parameterSetLengthWithNullTerminator +   // Parameter set, '\0' terminated
                                   data.Length);                            // Key

            // For ML-DSA we need 12 bytes for header, 6 bytes for parameter set, and 32 bytes for seed. Round up to 64.
            const int StackAllocThreshold = 64;

            // If there are new algorithms that require more than 64 bytes, we should increase the threshold.
            Debug.Assert(blobSize is > 256 or <= StackAllocThreshold, "Increase stackalloc threshold");

            byte[]? rented = null;
            Span<byte> blobBytes =
                blobSize <= StackAllocThreshold
                    ? stackalloc byte[StackAllocThreshold]
                    : (rented = CryptoPool.Rent(blobSize));

            blobBytes = blobBytes.Slice(0, blobSize);

            try
            {
                int index = 0;

                // Write header
                ref BCRYPT_PQDSA_KEY_BLOB blobHeader = ref MemoryMarshal.Cast<byte, BCRYPT_PQDSA_KEY_BLOB>(blobBytes)[0];
                blobHeader.Magic = magic;
                blobHeader.cbParameterSet = parameterSetLengthWithNullTerminator;
                blobHeader.cbKey = data.Length;
                index += blobHeaderSize;

                // Write parameter set
                Span<char> blobBodyChars = MemoryMarshal.Cast<byte, char>(blobBytes.Slice(index));
                parameterSet.CopyTo(blobBodyChars);
                blobBodyChars[parameterSet.Length] = '\0';
                index += parameterSetLengthWithNullTerminator;

                // Write key
                data.CopyTo(blobBytes.Slice(index));
                index += data.Length;

                Debug.Assert(index == blobBytes.Length);
                return callback(blobBytes);
            }
            finally
            {
                if (rented is not null)
                {
                    CryptoPool.Return(rented, blobSize);
                }
            }
        }

        private static ReadOnlySpan<byte> DecodePQDsaBlob(
            ReadOnlySpan<byte> blobBytes,
            out KeyBlobMagicNumber magic,
            out ReadOnlySpan<char> parameterSet)
        {
            int index = 0;

            ref readonly BCRYPT_PQDSA_KEY_BLOB blob = ref MemoryMarshal.Cast<byte, BCRYPT_PQDSA_KEY_BLOB>(blobBytes)[0];
            magic = blob.Magic;
            int parameterSetLength = blob.cbParameterSet - 2; // Null terminator char, '\0'
            int keyLength = blob.cbKey;
            index += Unsafe.SizeOf<BCRYPT_PQDSA_KEY_BLOB>();

            parameterSet = MemoryMarshal.Cast<byte, char>(blobBytes.Slice(index, parameterSetLength));
            index += blob.cbParameterSet;

            return blobBytes.Slice(index, keyLength);
        }
    }
}
