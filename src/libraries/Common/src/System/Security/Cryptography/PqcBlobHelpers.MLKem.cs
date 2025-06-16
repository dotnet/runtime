// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    }
}
