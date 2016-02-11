// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Microsoft.Win32.SafeHandles;

using Internal.Cryptography;

using ErrorCode = Interop.NCrypt.ErrorCode;
using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa
    {
        /// <summary>
        ///     Computes the signature of a hash that was produced by the hash algorithm specified by "hashAlgorithm."
        /// </summary>
        public override byte[] SignHash(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");

            int estimatedSize;
            switch (KeySize)
            {
                case 256: estimatedSize = 64; break;
                case 384: estimatedSize = 96; break;
                case 521: estimatedSize = 132; break;
                default:
                    // If we got here, the range of legal key sizes for ECDsaCng was expanded and someone didn't update this switch.
                    // Since it isn't a fatal error to miscalculate the estimatedSize, don't throw an exception. Just truck along.
                    estimatedSize = KeySize / 4;
                    break;
            }

            unsafe
            {
                byte[] signature = Key.Handle.SignHash(hash, AsymmetricPaddingMode.None, null, estimatedSize);
                return signature;
            }
        }

        /// <summary>
        ///     Verifies that alleged signature of a hash is, in fact, a valid signature of that hash.
        /// </summary>
        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            if (signature == null)
                throw new ArgumentNullException("signature");

            unsafe
            {
                bool verified = Key.Handle.VerifyHash(hash, signature, AsymmetricPaddingMode.None, null);
                return verified;
            }
        }
    }
}
