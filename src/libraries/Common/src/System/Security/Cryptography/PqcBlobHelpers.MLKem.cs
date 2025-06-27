// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using BCRYPT_MLKEM_KEY_BLOB = Interop.BCrypt.BCRYPT_MLKEM_KEY_BLOB;
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
using KeyBlobType = Interop.BCrypt.KeyBlobType;

namespace System.Security.Cryptography
{
    internal static partial class PqcBlobHelpers
    {
        internal const string BCRYPT_MLKEM_PARAMETER_SET_512 = "512";
        internal const string BCRYPT_MLKEM_PARAMETER_SET_768 = "768";
        internal const string BCRYPT_MLKEM_PARAMETER_SET_1024 = "1024";

        internal static string GetMLKemParameterSet(MLKemAlgorithm algorithm)
        {
            if (algorithm == MLKemAlgorithm.MLKem512)
            {
                return BCRYPT_MLKEM_PARAMETER_SET_512;
            }
            else if (algorithm == MLKemAlgorithm.MLKem768)
            {
                return BCRYPT_MLKEM_PARAMETER_SET_768;
            }
            else if (algorithm == MLKemAlgorithm.MLKem1024)
            {
                return BCRYPT_MLKEM_PARAMETER_SET_1024;
            }

            Debug.Fail($"Unknown MLKemAlgorithm: {algorithm.Name}");
            throw new PlatformNotSupportedException();
        }

        internal static string MLKemBlobMagicToBlobType(KeyBlobMagicNumber magicNumber)
        {
            return magicNumber switch
            {
                KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_SEED_MAGIC => KeyBlobType.BCRYPT_MLKEM_PRIVATE_SEED_BLOB,
                KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_MAGIC => KeyBlobType.BCRYPT_MLKEM_PRIVATE_BLOB,
                KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC => KeyBlobType.BCRYPT_MLKEM_PUBLIC_BLOB,
                KeyBlobMagicNumber other => throw Fail(other),
            };

            static CryptographicException Fail(KeyBlobMagicNumber other)
            {
                Debug.Fail($"Unknown blob type '{other}'.");
                return new CryptographicException();
            }
        }

        internal delegate TReturn EncodeMLKemBlobCallback<TState, TReturn>(
            TState state,
            string blobKind,
            ReadOnlySpan<byte> blob);

        internal static TReturn EncodeMLKemBlob<TState, TReturn>(
            KeyBlobMagicNumber kind,
            MLKemAlgorithm algorithm,
            ReadOnlySpan<byte> key,
            TState state,
            EncodeMLKemBlobCallback<TState, TReturn> callback)
        {
            checked
            {
                // ML-KEM 1024 seeds are 86 byte blobs. Round it off to 128.
                // Other keys like encapsulation or decapsulation keys will never fit in a stack buffer, so don't
                // try to accommodate them.
                const int MaxKeyStackSize = 128;
                string parameterSet = GetMLKemParameterSet(algorithm);
                int blobHeaderSize = Marshal.SizeOf<BCRYPT_MLKEM_KEY_BLOB>();
                int parameterSetMarshalLength = (parameterSet.Length + 1) * 2;
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
                    string blobKind = MLKemBlobMagicToBlobType(kind);
                    return callback(state, blobKind, buffer.Slice(0, blobSize));
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
    }
}
