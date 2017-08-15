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
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDsaImplementation
    {
#endif
    public sealed partial class ECDsaCng : ECDsa
    {
        /// <summary>
        ///     Computes the signature of a hash that was produced by the hash algorithm specified by "hashAlgorithm."
        /// </summary>
        public override byte[] SignHash(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

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
                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    byte[] signature = keyHandle.SignHash(hash, AsymmetricPaddingMode.None, null, estimatedSize);
                    return signature;
                }
            }
        }

        public override unsafe bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                return keyHandle.TrySignHash(source, destination, AsymmetricPaddingMode.None, null, out bytesWritten);
            }
        }

        /// <summary>
        ///     Verifies that alleged signature of a hash is, in fact, a valid signature of that hash.
        /// </summary>
        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));

            return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature);
        }

        public override unsafe bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
        {
            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                return keyHandle.VerifyHash(hash, signature, AsymmetricPaddingMode.None, null);
            }
        }
    }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
