// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
using KeyBlobType = Interop.BCrypt.KeyBlobType;
using BCRYPT_COMPOSITE_MLKEM_KEY_BLOB = Interop.BCrypt.BCRYPT_COMPOSITE_MLKEM_KEY_BLOB;

namespace System.Security.Cryptography
{
    internal static partial class PqcBlobHelpers
    {
        internal const string BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_P256 = "768-P256";
        internal const string BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_X25519 = "768-X25519";
        internal const string BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_1024_P384 = "1024-P384";

        internal static bool TryGetCompositeMLKemParameterSet(
            CompositeMLKemAlgorithm algorithm,
            [NotNullWhen(true)] out string? parameterSet)
        {
            if (algorithm == CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256)
            {
                parameterSet = BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_P256;
                return true;
            }
            else if (algorithm == CompositeMLKemAlgorithm.MLKem768WithX25519)
            {
                parameterSet = BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_X25519;
                return true;
            }
            else if (algorithm == CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384)
            {
                parameterSet = BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_1024_P384;
                return true;
            }

            parameterSet = null;
            return false;
        }

        internal static TResult EncodeCompositeMLKemBlob<TState, TResult>(
            ReadOnlySpan<char> parameterSet,
            ReadOnlySpan<byte> data,
            string blobType,
            TState state,
            EncodeMLKemBlobCallback<TState, TResult> callback)
        {
            KeyBlobMagicNumber magic = blobType switch
            {
                KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PUBLIC_BLOB =>
                    KeyBlobMagicNumber.BCRYPT_COMPOSITE_MLKEM_PUBLIC_MAGIC,
                KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PRIVATE_BLOB =>
                    KeyBlobMagicNumber.BCRYPT_COMPOSITE_MLKEM_PRIVATE_MAGIC,
                KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PRIVATE_IRTF_SEED_BLOB =>
                    KeyBlobMagicNumber.BCRYPT_COMPOSITE_MLKEM_PRIVATE_IRTF_SEED_MAGIC,
                _ => throw Fail(),
            };

            int headerSize = sizeof(BCRYPT_COMPOSITE_MLKEM_KEY_BLOB);
            int parameterSetLength = checked(sizeof(char) * (parameterSet.Length + 1));
            int blobSize = checked(headerSize + parameterSetLength + data.Length);

            byte[] rented = CryptoPool.Rent(blobSize);

            try
            {
                Span<byte> blobBytes = rented.AsSpan(0, blobSize);
                blobBytes.Clear();

                ref BCRYPT_COMPOSITE_MLKEM_KEY_BLOB blob = ref MemoryMarshal.Cast<byte, BCRYPT_COMPOSITE_MLKEM_KEY_BLOB>(blobBytes)[0];
                blob.dwMagic = magic;
                blob.cbParameterSet = (uint)parameterSetLength;
                blob.cbKey = (uint)data.Length;

                MemoryMarshal.AsBytes(parameterSet).CopyTo(blobBytes.Slice(headerSize));
                data.CopyTo(blobBytes.Slice(headerSize + parameterSetLength));
                return callback(state, blobType, blobBytes);
            }
            finally
            {
                CryptoPool.Return(rented, blobSize);
            }

            static CryptographicException Fail()
            {
                Debug.Fail("Unknown Composite ML-KEM blob type.");
                return new CryptographicException();
            }
        }

        internal static ReadOnlySpan<byte> DecodeCompositeMLKemBlob(
            ReadOnlySpan<byte> blobBytes,
            out ReadOnlySpan<char> parameterSet,
            out string blobType)
        {
            int headerSize = sizeof(BCRYPT_COMPOSITE_MLKEM_KEY_BLOB);

            if (blobBytes.Length < headerSize)
            {
                throw new CryptographicException();
            }

            ref readonly BCRYPT_COMPOSITE_MLKEM_KEY_BLOB blob =
                ref MemoryMarshal.Cast<byte, BCRYPT_COMPOSITE_MLKEM_KEY_BLOB>(blobBytes)[0];

            int parameterSetLength = checked((int)blob.cbParameterSet);
            int keyLength = checked((int)blob.cbKey);
            int expectedLength = checked(headerSize + parameterSetLength + keyLength);

            if (parameterSetLength < sizeof(char) ||
                (parameterSetLength & 1) != 0 ||
                expectedLength != blobBytes.Length)
            {
                throw new CryptographicException();
            }

            blobType = blob.dwMagic switch
            {
                KeyBlobMagicNumber.BCRYPT_COMPOSITE_MLKEM_PUBLIC_MAGIC =>
                    KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PUBLIC_BLOB,
                KeyBlobMagicNumber.BCRYPT_COMPOSITE_MLKEM_PRIVATE_MAGIC =>
                    KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PRIVATE_BLOB,
                KeyBlobMagicNumber.BCRYPT_COMPOSITE_MLKEM_PRIVATE_IRTF_SEED_MAGIC =>
                    KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PRIVATE_IRTF_SEED_BLOB,
                _ => throw Fail(blob.dwMagic),
            };

            parameterSet = MemoryMarshal.Cast<byte, char>(blobBytes.Slice(headerSize, parameterSetLength));

            if (parameterSet[^1] != '\0')
            {
                throw new CryptographicException();
            }

            parameterSet = parameterSet[..^1];
            return blobBytes.Slice(headerSize + parameterSetLength, keyLength);

            static CryptographicException Fail(KeyBlobMagicNumber magic)
            {
                Debug.Fail($"Unknown Composite ML-KEM blob magic '{magic}'.");
                return new CryptographicException();
            }
        }
    }
}
