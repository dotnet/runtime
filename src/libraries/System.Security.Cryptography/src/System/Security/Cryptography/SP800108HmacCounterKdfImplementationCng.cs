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
                symmetricKeyMaterialLength = HashOneShot(hashAlgorithm, key, buffer);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "Weak algorithms are used as instructed by the caller")]
        private static int HashOneShot(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> data, Span<byte> destination)
        {
            Debug.Assert(hashAlgorithm.Name is not null);

            switch (hashAlgorithm.Name)
            {
                case HashAlgorithmNames.SHA1:
                    return SHA1.HashData(data, destination);
                case HashAlgorithmNames.SHA256:
                    return SHA256.HashData(data, destination);
                case HashAlgorithmNames.SHA384:
                    return SHA384.HashData(data, destination);
                case HashAlgorithmNames.SHA512:
                    return SHA512.HashData(data, destination);
                case HashAlgorithmNames.SHA3_256:
                    return SHA3_256.IsSupported ? SHA3_256.HashData(data, destination) : throw new PlatformNotSupportedException();
                case HashAlgorithmNames.SHA3_384:
                    return SHA3_384.IsSupported ? SHA3_384.HashData(data, destination) : throw new PlatformNotSupportedException();
                case HashAlgorithmNames.SHA3_512:
                    return SHA3_512.IsSupported ? SHA3_512.HashData(data, destination) : throw new PlatformNotSupportedException();
                default:
                    Debug.Fail($"Unexpected hash algorithm '{hashAlgorithm.Name}'");
                    throw new CryptographicException();
            }
        }
    }
}
