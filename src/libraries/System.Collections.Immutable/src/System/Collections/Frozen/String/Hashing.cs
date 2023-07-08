// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Collections.Frozen
{
    internal static class Hashing
    {
        // TODO https://github.com/dotnet/runtime/issues/77679:
        // Replace these once non-randomized implementations are available.

        // Lengths 0 to 4 are unrolled manually due to their commonality, especially
        // with the substring-based dictionary/sets that use substrings with length <= 4.

        private const uint Hash1Start = (5381 << 16) + 5381;
        private const uint Factor = 1_566_083_941;

        public static unsafe int GetHashCodeOrdinal(ReadOnlySpan<char> s)
        {
            int length = s.Length;
            fixed (char* src = &MemoryMarshal.GetReference(s))
            {
                uint hash1, hash2;
                switch (length)
                {
                    case 0:
                        return (int)(Hash1Start + unchecked(Hash1Start * Factor));

                    case 1:
                        hash2 = (BitOperations.RotateLeft(Hash1Start, 5) + Hash1Start) ^ src[0];
                        return (int)(Hash1Start + (hash2 * Factor));

                    case 2:
                        hash2 = (BitOperations.RotateLeft(Hash1Start, 5) + Hash1Start) ^ src[0];
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ src[1];
                        return (int)(Hash1Start + (hash2 * Factor));

                    case 3:
                        hash2 = (BitOperations.RotateLeft(Hash1Start, 5) + Hash1Start) ^ src[0];
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ src[1];
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ src[2];
                        return (int)(Hash1Start + (hash2 * Factor));

                    case 4:
                        hash1 = (BitOperations.RotateLeft(Hash1Start, 5) + Hash1Start) ^ ((uint*)src)[0];
                        hash2 = (BitOperations.RotateLeft(Hash1Start, 5) + Hash1Start) ^ ((uint*)src)[1];
                        return (int)(hash1 + (hash2 * Factor));

                    default:
                        hash1 = Hash1Start;
                        hash2 = hash1;

                        uint* ptrUInt32 = (uint*)src;
                        while (length >= 4)
                        {
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptrUInt32[0];
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptrUInt32[1];
                            ptrUInt32 += 2;
                            length -= 4;
                        }

                        char* ptrChar = (char*)ptrUInt32;
                        while (length-- > 0)
                        {
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ *ptrChar++;
                        }

                        return (int)(hash1 + (hash2 * Factor));
                }
            }
        }

        // useful if the string only contains ASCII characters
        public static unsafe int GetHashCodeOrdinalIgnoreCaseAscii(ReadOnlySpan<char> s)
        {
            // We "normalize to lowercase" every char by ORing with 0x20. This casts
            // a very wide net because it will change, e.g., '^' to '~'. But that should
            // be ok because we expect this to be very rare in practice.
            const uint LowercaseChar = 0x20u;
            const uint LowercaseUInt32 = 0x0020_0020u;

            int length = s.Length;
            fixed (char* src = &MemoryMarshal.GetReference(s))
            {
                uint hash1, hash2;
                switch (length)
                {
                    case 0:
                        return (int)(Hash1Start + unchecked(Hash1Start * Factor));

                    case 1:
                        hash2 = (BitOperations.RotateLeft(Hash1Start, 5) + Hash1Start) ^ (src[0] | LowercaseChar);
                        return (int)(Hash1Start + (hash2 * Factor));

                    case 2:
                        hash2 = (BitOperations.RotateLeft(Hash1Start, 5) + Hash1Start) ^ (src[0] | LowercaseChar);
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (src[1] | LowercaseChar);
                        return (int)(Hash1Start + (hash2 * Factor));

                    case 3:
                        hash2 = (BitOperations.RotateLeft(Hash1Start, 5) + Hash1Start) ^ (src[0] | LowercaseChar);
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (src[1] | LowercaseChar);
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (src[2] | LowercaseChar);
                        return (int)(Hash1Start + (hash2 * Factor));

                    case 4:
                        hash1 = (BitOperations.RotateLeft(Hash1Start, 5) + Hash1Start) ^ (((uint*)src)[0] | LowercaseUInt32);
                        hash2 = (BitOperations.RotateLeft(Hash1Start, 5) + Hash1Start) ^ (((uint*)src)[1] | LowercaseUInt32);
                        return (int)(hash1 + (hash2 * Factor));

                    default:
                        hash1 = Hash1Start;
                        hash2 = hash1;

                        uint* ptrUInt32 = (uint*)src;
                        while (length >= 4)
                        {
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (ptrUInt32[0] | LowercaseUInt32);
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (ptrUInt32[1] | LowercaseUInt32);
                            ptrUInt32 += 2;
                            length -= 4;
                        }

                        char* ptrChar = (char*)ptrUInt32;
                        while (length-- > 0)
                        {
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (*ptrChar | LowercaseUInt32);
                            ptrChar++;
                        }

                        return (int)(hash1 + (hash2 * Factor));
                }
            }
        }

        public static unsafe int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> s)
        {
            int length = s.Length;

            char[]? rentedArray = null;
            Span<char> scratch = length <= 256 ?
                stackalloc char[256] :
                (rentedArray = ArrayPool<char>.Shared.Rent(length));

            length = s.ToUpperInvariant(scratch); // NOTE: this really should be the (non-existent) ToUpperOrdinal
            int hash = GetHashCodeOrdinal(scratch.Slice(0, length));

            if (rentedArray is not null)
            {
                ArrayPool<char>.Shared.Return(rentedArray);
            }

            return hash;
        }
    }
}
