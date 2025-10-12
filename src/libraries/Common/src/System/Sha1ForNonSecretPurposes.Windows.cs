// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System
{
    /// <summary>
    /// Implements the SHA1 hashing algorithm. Note that
    /// implementation is for hashing public information. Do not
    /// use code to hash private data, as implementation does
    /// not take any steps to avoid information disclosure.
    /// </summary>
    internal struct Sha1ForNonSecretPurposes
    {
        /// <summary>
        /// Computes the SHA1 hash of the provided data.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        public static void HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            Debug.Assert(destination.Length == 20);

            unsafe
            {
                fixed (byte* pSrc = &MemoryMarshal.GetReference(source))
                fixed (byte* pDest = &MemoryMarshal.GetReference(destination))
                {
                    Interop.BCrypt.NTSTATUS ntStatus = Interop.BCrypt.BCryptHash(
                        (uint)Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA1_ALG_HANDLE,
                        null,
                        0,
                        pSrc,
                        source.Length,
                        pDest,
                        destination.Length);

                    if (ntStatus != Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
                    {
                        if (ntStatus == Interop.BCrypt.NTSTATUS.STATUS_NO_MEMORY)
                        {
                            throw new OutOfMemoryException();
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }                        
                    }
                }
            }
        }
    }
}
