// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    //
    // Common infrastructure for AsymmetricAlgorithm-derived classes that layer on OpenSSL.
    //
    internal static partial class AsymmetricAlgorithmHelpers
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is used when the user asks for it.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "MD5 is used when the user asks for it.")]
        public static byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            // The classes that call us are sealed and their base class has checked this already.
            Debug.Assert(data != null);
            Debug.Assert(count >= 0 && count <= data.Length);
            Debug.Assert(offset >= 0 && offset <= data.Length - count);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

#if NET5_0_OR_GREATER
            ReadOnlySpan<byte> source = data.AsSpan(offset, count);

            return
                hashAlgorithm == HashAlgorithmName.SHA256 ? SHA256.HashData(source) :
                hashAlgorithm == HashAlgorithmName.SHA1 ? SHA1.HashData(source) :
                hashAlgorithm == HashAlgorithmName.SHA512 ? SHA512.HashData(source) :
                hashAlgorithm == HashAlgorithmName.SHA384 ? SHA384.HashData(source) :
                hashAlgorithm == HashAlgorithmName.MD5 ? MD5.HashData(source) :
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
#else
            using (HashAlgorithm hasher = GetHashAlgorithm(hashAlgorithm))
            {
                return hasher.ComputeHash(data, offset, count);
            }
#endif
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is used when the user asks for it.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "MD5 is used when the user asks for it.")]
        public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            // The classes that call us are sealed and their base class has checked this already.
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

#if NET5_0_OR_GREATER
            return
                hashAlgorithm == HashAlgorithmName.SHA256 ? SHA256.TryHashData(source, destination, out bytesWritten) :
                hashAlgorithm == HashAlgorithmName.SHA1 ? SHA1.TryHashData(source, destination, out bytesWritten) :
                hashAlgorithm == HashAlgorithmName.SHA512 ? SHA512.TryHashData(source, destination, out bytesWritten) :
                hashAlgorithm == HashAlgorithmName.SHA384 ? SHA384.TryHashData(source, destination, out bytesWritten) :
                hashAlgorithm == HashAlgorithmName.MD5 ? MD5.TryHashData(source, destination, out bytesWritten) :
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
#else
            using (HashAlgorithm hasher = GetHashAlgorithm(hashAlgorithm))
            {
                return hasher.TryComputeHash(source, destination, out bytesWritten);
            }
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is used when the user asks for it.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "MD5 is used when the user asks for it.")]
        private static HashAlgorithm GetHashAlgorithm(HashAlgorithmName hashAlgorithmName) =>
            hashAlgorithmName == HashAlgorithmName.SHA256 ? SHA256.Create() :
            hashAlgorithmName == HashAlgorithmName.SHA1 ? SHA1.Create() :
            hashAlgorithmName == HashAlgorithmName.SHA512 ? SHA512.Create() :
            hashAlgorithmName == HashAlgorithmName.SHA384 ? SHA384.Create() :
            hashAlgorithmName == HashAlgorithmName.MD5 ? MD5.Create() :
            throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);
    }
}
