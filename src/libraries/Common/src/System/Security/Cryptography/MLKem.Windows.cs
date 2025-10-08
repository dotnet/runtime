// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
using BCRYPT_MLKEM_KEY_BLOB = Interop.BCrypt.BCRYPT_MLKEM_KEY_BLOB;

namespace System.Security.Cryptography
{
    public abstract partial class MLKem
    {
        private protected unsafe void ReadCngMLKemBlob(
            KeyBlobMagicNumber kind,
            ReadOnlySpan<byte> exportedSpan,
            Span<byte> destination)
        {
            fixed (byte* pExportedSpan = exportedSpan)
            {
                BCRYPT_MLKEM_KEY_BLOB* blob = (BCRYPT_MLKEM_KEY_BLOB*)pExportedSpan;

                if (blob->dwMagic != kind)
                {
                    Debug.Fail("dwMagic is not expected value");
                    throw new CryptographicException();
                }

                int blobHeaderSize = sizeof(BCRYPT_MLKEM_KEY_BLOB);
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
}
