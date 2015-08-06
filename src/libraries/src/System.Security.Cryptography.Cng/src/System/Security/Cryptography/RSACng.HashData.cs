// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            // we're sealed and the base should have checked this already.
            Debug.Assert(data != null);
            Debug.Assert(offset >= 0 && offset <= data.Length);
            Debug.Assert(count >= 0 && count <= data.Length);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            HashAlgorithm hasher = GetHasher(hashAlgorithm);
            byte[] hash = hasher.ComputeHash(data, offset, count);
            return hash;
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            // We're sealed and the base should have checked these already.
            Debug.Assert(data != null);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            HashAlgorithm hasher = GetHasher(hashAlgorithm);
            byte[] hash = hasher.ComputeHash(data);
            return hash;
        }

        private static HashAlgorithm GetHasher(HashAlgorithmName hashAlgorithm)
        {
            // @todo B#1208349:  This is a temporary implementation that should nevertheless handle most real-world cases. To fully implement this method,
            //   it needs to be able to handle arbitrary hash algorithms on the local CNG primitive provider's menu. There are some interop-cleanup/layering decisions
            //   that need to made first.

            if (hashAlgorithm == HashAlgorithmName.MD5)
                return MD5.Create();

            if (hashAlgorithm == HashAlgorithmName.SHA1)
                return SHA1.Create();

            if (hashAlgorithm == HashAlgorithmName.SHA256)
                return SHA256.Create();

            if (hashAlgorithm == HashAlgorithmName.SHA384)
                return SHA384.Create();

            if (hashAlgorithm == HashAlgorithmName.SHA512)
                return SHA512.Create();

            throw new NotImplementedException(SR.WorkInProgress_UnsupportedHash);   // Can't handle arbitrary CNG hash algorithms yet.
        }
    }
}

