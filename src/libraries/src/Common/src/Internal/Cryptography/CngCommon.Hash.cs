// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class CngCommon
    {
        public static byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            // The classes that call us are sealed and their base class has checked this already.
            Debug.Assert(data != null);
            Debug.Assert(offset >= 0 && offset <= data.Length);
            Debug.Assert(count >= 0 && count <= data.Length);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            using (var hashProvider = new HashProviderCng(hashAlgorithm.Name, null))
            {
                hashProvider.AppendHashData(data, offset, count);
                return hashProvider.FinalizeHashAndReset();
            }
        }

        public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            using (var hashProvider = new HashProviderCng(hashAlgorithm.Name, null))
            {
                if (destination.Length < hashProvider.HashSizeInBytes)
                {
                    bytesWritten = 0;
                    return false;
                }

                hashProvider.AppendHashData(source);
                return hashProvider.TryFinalizeHashAndReset(destination, out bytesWritten);
            }
        }

        public static byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            // The classes that call us are sealed and their base class has checked this already.
            Debug.Assert(data != null);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            using (var hashProvider = new HashProviderCng(hashAlgorithm.Name, null))
            {
                // Default the buffer size to 4K.
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = data.Read(buffer, 0, buffer.Length)) > 0)
                {
                    hashProvider.AppendHashData(buffer, 0, bytesRead);
                }
                byte[] hash = hashProvider.FinalizeHashAndReset();
                return hash;
            }
        }
    }
}
