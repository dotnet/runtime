// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;

namespace System.Collections.Immutable
{
    internal static class Hashing
    {
        public static unsafe int GetHashCode(ReadOnlySpan<char> s)
        {
            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            fixed (char* c = s)
            {
                uint* ptr = (uint*)c;
                int length = s.Length;

                while (length > 3)
                {
                    hash1 = BitOperations.RotateLeft(hash1, 5) + hash1 ^ *ptr++;
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ *ptr++;
                    length -= 4;
                }

                char* tail = (char*)ptr;
                while (length-- > 0)
                {
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ *tail++;
                }

                return (int)(hash1 + (hash2 * 1_566_083_941));
            }
        }

        // useful if the string only contains ASCII characterss
        public static unsafe int GetCaseInsensitiveAsciiHashCode(ReadOnlySpan<char> s)
        {
            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            fixed (char* src = s)
            {
                uint* ptr = (uint*)src;
                int length = s.Length;

                // We "normalize to lowercase" every char by ORing with 0x0020. This casts
                // a very wide net because it will change, e.g., '^' to '~'. But that should
                // be ok because we expect this to be very rare in practice.
                const uint NormalizeToLowercase = 0x0020_0020u; // valid both for big-endian and for little-endian

                while (length > 3)
                {
                    hash1 = BitOperations.RotateLeft(hash1, 5) + hash1 ^ (*ptr++ | NormalizeToLowercase);
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ (*ptr++ | NormalizeToLowercase);
                    length -= 4;
                }

                char* tail = (char*)ptr;
                while (length-- > 0)
                {
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ (*tail++ | NormalizeToLowercase);
                }
            }

            return (int)(hash1 + (hash2 * 1_566_083_941));
        }

        public static unsafe int GetCaseInsensitiveHashCode(ReadOnlySpan<char> s)
        {
            int length = s.Length;

            char[]? rentedArray = null;
            Span<char> scratch = length <= 256 ?
                stackalloc char[256] :
                (rentedArray = ArrayPool<char>.Shared.Rent(length));

            length = s.ToUpperInvariant(scratch);   // WARNING: this really should be ToUpperOrdinal, but .NET doesn't offer this as a primitive

            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            fixed (char* src = scratch)
            {
                uint* ptr = (uint*)src;
                while (length > 3)
                {
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ *ptr++;
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ *ptr++;
                    length -= 4;
                }

                char* tail = (char*)ptr;
                while (length-- > 0)
                {
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ *tail++;
                }
            }

            if (rentedArray is not null)
            {
                ArrayPool<char>.Shared.Return(rentedArray);
            }

            return (int)(hash1 + (hash2 * 1_566_083_941));
        }
    }
}
