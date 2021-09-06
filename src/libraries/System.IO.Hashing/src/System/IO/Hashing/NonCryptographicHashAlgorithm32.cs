// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Hashing
{
    /// <summary>
    ///   Represents a non-cryptographic hash algorithm whose hash size is 32 bits.
    /// </summary>
    public abstract class NonCryptographicHashAlgorithm32 : NonCryptographicHashAlgorithm
    {
        protected const int Size = sizeof(uint);

        protected NonCryptographicHashAlgorithm32() : base(Size)
        {
        }

        /// <summary>
        /// Gets the computed 32 bits hash value without modifying accumulated state.
        /// </summary>
        /// <returns>The computed hash value.</returns>
        protected abstract int GetHash();
    }
}
