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
                byte[] keyArray = new byte[key.Length];

                fixed (byte* pKeyArray = keyArray)
                {
                    key.CopyTo(keyArray);
                    clearSpan = HashOneShot(hashAlgorithm, keyArray);
                    CryptographicOperations.ZeroMemory(keyArray);
                }

                symmetricKeyMaterial = clearSpan;
                symmetricKeyMaterialLength = symmetricKeyMaterial.Length;
            }
            else if (!key.IsEmpty)
            {
                symmetricKeyMaterial = key;
                symmetricKeyMaterialLength = key.Length;
            }
            else
            {
                // CNG requires a non-null pointer even when the length is zero.
                symmetricKeyMaterial = stackalloc byte[] { 0 };
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

        // For .NET Standard / .NET Framework, provide a byte overload so that we don't go from array->span->array
        // when we need to adjust keys that are too large.
        internal unsafe SP800108HmacCounterKdfImplementationCng(byte[] key, HashAlgorithmName hashAlgorithm)
        {
            Debug.Assert(hashAlgorithm.Name is not null);

            scoped ReadOnlySpan<byte> symmetricKeyMaterial;
            scoped Span<byte> clearSpan = default;
            int symmetricKeyMaterialLength;
            int hashAlgorithmBlockSize = GetHashBlockSize(hashAlgorithm.Name);

            if (key.Length > hashAlgorithmBlockSize)
            {
                clearSpan = HashOneShot(hashAlgorithm, key);
                symmetricKeyMaterial = clearSpan;
                symmetricKeyMaterialLength = symmetricKeyMaterial.Length;
            }
            else if (key.Length > 0)
            {
                symmetricKeyMaterial = key;
                symmetricKeyMaterialLength = key.Length;
            }
            else
            {
                // CNG requires a non-null pointer even when the length is zero.
                symmetricKeyMaterial = stackalloc byte[] { 0 };
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

        private static byte[] HashOneShot(HashAlgorithmName hashAlgorithm, byte[] data)
        {
            using (HashAlgorithm hash = CreateHash(hashAlgorithm))
            {
                return hash.ComputeHash(data);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "Weak algorithms are used as instructed by the caller")]
        private static HashAlgorithm CreateHash(HashAlgorithmName hashAlgorithm)
        {
            switch (hashAlgorithm.Name)
            {
                case HashAlgorithmNames.SHA1:
                    return SHA1.Create();
                case HashAlgorithmNames.SHA256:
                    return SHA256.Create();
                case HashAlgorithmNames.SHA384:
                    return SHA384.Create();
                case HashAlgorithmNames.SHA512:
                    return SHA512.Create();
                default:
                    Debug.Fail($"Unexpected hash algorithm '{hashAlgorithm.Name}'.");
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
            }
        }
    }
}
