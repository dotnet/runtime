// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Hashing
{
    /// <summary>
    ///   Represents a non-cryptographic hash algorithm whose hash size is 64 bits.
    /// </summary>
    public abstract class NonCryptographicHashAlgorithm64 : NonCryptographicHashAlgorithm
    {
        protected const int Size = sizeof(ulong);

        protected NonCryptographicHashAlgorithm64() : base(Size)
        {
        }

        /// <summary>
        /// Gets the computed 64 bits hash value without modifying accumulated state.
        /// </summary>
        /// <returns>The computed hash value.</returns>
        protected abstract long GetHash();
    }
}
