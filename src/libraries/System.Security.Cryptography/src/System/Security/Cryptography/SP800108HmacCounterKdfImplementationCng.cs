// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class SP800108HmacCounterKdfImplementationCng
    {
        internal unsafe SP800108HmacCounterKdfImplementationCng(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm)
        {
            Debug.Assert(hashAlgorithm.Name is not null);

            scoped ReadOnlySpan<byte> symmetricKeyMaterial;
            scoped Span<byte> clearSpan = default;
            int symmetricKeyMaterialLength;
            int hashAlgorithmBlockSize = GetHashBlockSize(hashAlgorithm.Name);

            if (key.Length > hashAlgorithmBlockSize)
            {
                Span<byte> buffer = stackalloc byte[512 / 8]; // Largest supported digest is SHA512.
                symmetricKeyMaterialLength = CryptographicOperations.HashData(hashAlgorithm, key, buffer);
                clearSpan = buffer.Slice(0, symmetricKeyMaterialLength);
                symmetricKeyMaterial = clearSpan;
            }
            else if (!key.IsEmpty)
            {
                symmetricKeyMaterial = key;
                symmetricKeyMaterialLength = key.Length;
            }
            else
            {
                // CNG requires a non-null pointer even when the length is zero.
                symmetricKeyMaterial = [0];
                symmetricKeyMaterialLength = 0;
            }

            try
            {
                fixed (byte* pSymmetricKeyMaterial = symmetricKeyMaterial)
                {
                    _keyHandle = CreateSymmetricKey(pSymmetricKeyMaterial, symmetricKeyMaterialLength);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(clearSpan);
            }

            _hashAlgorithm = hashAlgorithm;
        }
    }
}
