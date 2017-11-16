// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Internal.Cryptography
{
    //
    // This abstract class represents a reusable hash object and can wrap a CNG or WinRT hash object.
    //
    internal abstract class HashProvider : IDisposable
    {
        // Adds new data to be hashed. This can be called repeatedly in order to hash data from noncontiguous sources.
        public void AppendHashData(byte[] data, int offset, int count)
        {
            // AppendHashData can be called via exposed APIs (e.g. a type that derives from
            // HMACSHA1 and calls HashCore) and could be passed bad data from there.  It could
            // also receive a bad count from HashAlgorithm reading from a Stream that returns
            // an invalid number of bytes read.  Since our implementations of AppendHashDataCore
            // end up using unsafe code, we want to be sure the arguments are valid.
            if (data == null)
                throw new ArgumentNullException(nameof(data), SR.ArgumentNull_Buffer);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (data.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            AppendHashData(new ReadOnlySpan<byte>(data, offset, count));
        }

        public abstract void AppendHashData(ReadOnlySpan<byte> data);
        
        // Compute the hash based on the appended data and resets the HashProvider for more hashing.
        public abstract byte[] FinalizeHashAndReset();

        public abstract bool TryFinalizeHashAndReset(Span<byte> destination, out int bytesWritten);

        // Returns the length of the byte array returned by FinalizeHashAndReset.
        public abstract int HashSizeInBytes { get; }

        // Releases any native resources and keys used by the HashProvider.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Releases any native resources and keys used by the HashProvider.
        public abstract void Dispose(bool disposing);
    }
}
