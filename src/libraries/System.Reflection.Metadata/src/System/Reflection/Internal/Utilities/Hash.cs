// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace System.Reflection.Internal
{
    internal static class Hash
    {
        internal static int Combine(int newKey, int currentKey)
        {
            return unchecked((currentKey * (int)0xA5555529) + newKey);
        }

        internal static int Combine(uint newKey, int currentKey)
        {
            return unchecked((currentKey * (int)0xA5555529) + (int)newKey);
        }

        internal static int Combine(bool newKeyPart, int currentKey)
        {
            return Combine(currentKey, newKeyPart ? 1 : 0);
        }

        /// <summary>
        /// The offset bias value used in the FNV-1a algorithm
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        internal const int FnvOffsetBias = unchecked((int)2166136261);

        /// <summary>
        /// The generative factor used in the FNV-1a algorithm
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        internal const int FnvPrime = 16777619;

        /// <summary>
        /// Compute the FNV-1a hash of a sequence of bytes
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <param name="data">The sequence of bytes</param>
        /// <returns>The FNV-1a hash of <paramref name="data"/></returns>
        internal static int GetFNVHashCode(byte[] data)
        {
            int hashCode = Hash.FnvOffsetBias;

            for (int i = 0; i < data.Length; i++)
            {
                hashCode = unchecked((hashCode ^ data[i]) * Hash.FnvPrime);
            }

            return hashCode;
        }

        /// <summary>
        /// Compute the FNV-1a hash of a sequence of bytes
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <param name="data">The sequence of bytes</param>
        /// <returns>The FNV-1a hash of <paramref name="data"/></returns>
        internal static int GetFNVHashCode(ImmutableArray<byte> data)
        {
            int hashCode = Hash.FnvOffsetBias;

            for (int i = 0; i < data.Length; i++)
            {
                hashCode = unchecked((hashCode ^ data[i]) * Hash.FnvPrime);
            }

            return hashCode;
        }
    }
}
