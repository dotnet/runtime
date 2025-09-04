// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Represents a ZStandard compression dictionary.</summary>
    public sealed class ZStandardDictionary : IDisposable
    {
        private ZStandardDictionary()
        {
        }

        /// <summary>Creates a ZStandard dictionary from the specified buffer.</summary>
        /// <param name="buffer">The buffer containing the dictionary data.</param>
        /// <returns>A new <see cref="ZStandardDictionary"/> instance.</returns>
        /// <exception cref="ArgumentException">The buffer is empty.</exception>
        public static ZStandardDictionary Create(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.IsEmpty)
                throw new ArgumentException("Buffer cannot be empty.", nameof(buffer));

            throw new NotImplementedException();
        }

        /// <summary>Creates a ZStandard dictionary from the specified buffer with the specified quality level.</summary>
        /// <param name="buffer">The buffer containing the dictionary data.</param>
        /// <param name="quality">The quality level for dictionary creation.</param>
        /// <returns>A new <see cref="ZStandardDictionary"/> instance.</returns>
        /// <exception cref="ArgumentException">The buffer is empty.</exception>
        public static ZStandardDictionary Create(ReadOnlyMemory<byte> buffer, int quality)
        {
            if (buffer.IsEmpty)
                throw new ArgumentException("Buffer cannot be empty.", nameof(buffer));

            throw new NotImplementedException();
        }

        /// <summary>Releases all resources used by the <see cref="ZStandardDictionary"/>.</summary>
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
