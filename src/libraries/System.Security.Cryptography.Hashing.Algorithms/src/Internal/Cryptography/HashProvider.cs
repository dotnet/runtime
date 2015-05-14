// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Internal.Cryptography
{
    //
    // This abstract class represents a reusable hash object and can wrap a CNG or WinRT hash object.
    //
    internal abstract class HashProvider
    {
        // Adds new data to be hashed. This can be called repeatedly in order to hash data from incontiguous sources.
        public abstract void AppendHashData(byte[] data, int offset, int count);

        // Compute the hash based on the appended data and resets the HashProvider for more hashing.
        public abstract byte[] FinalizeHashAndReset();

        // Returns the length of the byte array returned by FinalizeHashAndReset.
        public abstract int HashSizeInBytes { get; }

        // Releases any native resources and keys used by the HashProvider.
        public abstract void Dispose(bool disposing);
    }
}



