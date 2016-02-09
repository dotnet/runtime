// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;


namespace Internal.Cryptography
{
    //
    // Common infrastructure for AsymmetricAlgorithm-derived classes that layer on OpenSSL.
    //
    internal struct OpenSslAsymmetricAlgorithmCore
    {
        public static byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            // The classes that call us are sealed and their base class has checked this already.
            Debug.Assert(data != null);
            Debug.Assert(count >= 0 && count <= data.Length);
            Debug.Assert(offset >= 0 && offset <= data.Length - count);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            using (HashAlgorithm hasher = GetHashAlgorithm(hashAlgorithm))
            {
                return hasher.ComputeHash(data, offset, count);
            }
        }

        public static byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            // The classes that call us are sealed and their base class has checked this already.
            Debug.Assert(data != null);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            using (HashAlgorithm hasher = GetHashAlgorithm(hashAlgorithm))
            {
                return hasher.ComputeHash(data);
            }
        }

        private static HashAlgorithm GetHashAlgorithm(HashAlgorithmName hashAlgorithmName)
        {
            HashAlgorithm hasher;

            if (hashAlgorithmName == HashAlgorithmName.MD5)
            {
                hasher = MD5.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA1)
            {
                hasher = SHA1.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA256)
            {
                hasher = SHA256.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA384)
            {
                hasher = SHA384.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA512)
            {
                hasher = SHA512.Create();
            }
            else
            {
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);
            }

            return hasher;
        }
    }
}


