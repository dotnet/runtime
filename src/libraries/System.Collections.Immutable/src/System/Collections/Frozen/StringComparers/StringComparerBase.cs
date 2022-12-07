// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Collections.Frozen
{
    // We define this rather than using IEqualityComparer<string>, since virtual dispatch is faster than interface dispatch
    internal abstract class StringComparerBase : EqualityComparer<string>
    {
        // TODO https://github.com/dotnet/runtime/issues/77679:
        // Replace these once non-randomized implementations are available.

        protected static unsafe int GetHashCodeOrdinal(ReadOnlySpan<char> s)
        {
            int length = s.Length;
            fixed (char* src = &MemoryMarshal.GetReference(s))
            {
                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                uint* ptrUInt32 = (uint*)src;
                while (length > 3)
                {
                    hash1 = BitOperations.RotateLeft(hash1, 5) + hash1 ^ ptrUInt32[0];
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ ptrUInt32[1];
                    ptrUInt32 += 2;
                    length -= 4;
                }

                char* ptrChar = (char*)ptrUInt32;
                while (length-- > 0)
                {
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ *ptrChar++;
                }

                return (int)(hash1 + (hash2 * 1_566_083_941));
            }
        }

        // useful if the string only contains ASCII characterss
        protected static unsafe int GetHashCodeOrdinalIgnoreCaseAscii(ReadOnlySpan<char> s)
        {
            int length = s.Length;
            fixed (char* src = &MemoryMarshal.GetReference(s))
            {
                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                // We "normalize to lowercase" every char by ORing with 0x0020. This casts
                // a very wide net because it will change, e.g., '^' to '~'. But that should
                // be ok because we expect this to be very rare in practice.
                const uint NormalizeToLowercase = 0x0020_0020u; // valid both for big-endian and for little-endian

                uint* ptrUInt32 = (uint*)src;
                while (length > 3)
                {
                    hash1 = BitOperations.RotateLeft(hash1, 5) + hash1 ^ (ptrUInt32[0] | NormalizeToLowercase);
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ (ptrUInt32[1] | NormalizeToLowercase);
                    ptrUInt32 += 2;
                    length -= 4;
                }

                char* ptrChar = (char*)ptrUInt32;
                while (length-- > 0)
                {
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ (*ptrChar | NormalizeToLowercase);
                    ptrChar++;
                }

                return (int)(hash1 + (hash2 * 1_566_083_941));
            }
        }

        protected static unsafe int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> s)
        {
            int length = s.Length;

            char[]? rentedArray = null;
            Span<char> scratch = length <= 256 ?
                stackalloc char[256] :
                (rentedArray = ArrayPool<char>.Shared.Rent(length));

            length = s.ToUpperInvariant(scratch); // NOTE: this really should be the (non-existent) ToUpperOrdinal

            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            fixed (char* src = &MemoryMarshal.GetReference(scratch))
            {
                uint* ptrUInt32 = (uint*)src;
                while (length > 3)
                {
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptrUInt32[0];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptrUInt32[1];
                    ptrUInt32 += 2;
                    length -= 4;
                }

                char* ptrChar = (char*)ptrUInt32;
                while (length-- > 0)
                {
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ *ptrChar++;
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
