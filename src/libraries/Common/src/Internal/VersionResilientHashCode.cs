// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Internal
{
    /// <summary>
    /// Managed implementation of the version-resilient hash code algorithm.
    /// </summary>
    internal static partial class VersionResilientHashCode
    {
        public struct HashCodeBuilder
        {
            private int _hash1;
            private int _hash2;
            private int _numCharactersHashed;

            public HashCodeBuilder(ReadOnlySpan<byte> seed)
            {
                _hash1 = 0x6DA3B944;
                _hash2 = 0;
                _numCharactersHashed = 0;
            }

            public void Append(ReadOnlySpan<byte> src)
            {
                if (src.Length == 0)
                    return;

                int startIndex = 0;
                if ((_numCharactersHashed & 1) == 1)
                {
                    _hash2 = unchecked(_hash2 + RotateLeft(_hash2, 5)) ^ (int)unchecked((sbyte)src[0]);
                    startIndex = 1;
                }

                for (int i = startIndex; i < src.Length; i += 2)
                {
                    _hash1 = unchecked(_hash1 + RotateLeft(_hash1, 5)) ^ (int)unchecked((sbyte)src[i]);
                    if (i + 1 < src.Length)
                    {
                        _hash2 = unchecked(_hash2 + RotateLeft(_hash2, 5)) ^ (int)unchecked((sbyte)src[i + 1]);
                    }
                    else
                    {
                        break;
                    }
                }

                _numCharactersHashed += src.Length;
            }

            public int ToHashCode()
            {
                if (_numCharactersHashed == 0)
                    return 0;

                int hash1 = unchecked(_hash1 + RotateLeft(_hash1, 8));
                int hash2 = unchecked(_hash2 + RotateLeft(_hash2, 8));

                return unchecked((int)(hash1 ^ hash2));
            }
        }

        /// <summary>
        /// CoreCLR <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/typehashingalgorithms.h#L14">ComputeNameHashCode</a>
        /// </summary>
        /// <param name="src">Name string to hash</param>
        public static int NameHashCode(ReadOnlySpan<byte> src)
        {
            if (src.Length == 0)
            {
                return 0;
            }

            int hash1 = 0x6DA3B944;
            int hash2 = 0;

            for (int i = 0; i < src.Length; i += 2)
            {
                hash1 = unchecked(hash1 + RotateLeft(hash1, 5)) ^ (int)unchecked((sbyte)src[i]);
                if (i + 1 < src.Length)
                {
                    hash2 = unchecked(hash2 + RotateLeft(hash2, 5)) ^ (int)unchecked((sbyte)src[i + 1]);
                }
                else
                {
                    break;
                }
            }

            hash1 = unchecked(hash1 + RotateLeft(hash1, 8));
            hash2 = unchecked(hash2 + RotateLeft(hash2, 8));

            return unchecked((int)(hash1 ^ hash2));
        }

        /// <summary>
        /// Calculate hash code for a namespace - name combination.
        /// CoreCLR 2-parameter <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/typehashingalgorithms.h#L41">ComputeNameHashCode</a>
        /// </summary>
        /// <param name="namespacePart">Namespace name</param>
        /// <param name="namePart">Type name within the namespace</param>
        public static int NameHashCode(ReadOnlySpan<byte> namespacePart, ReadOnlySpan<byte> namePart)
        {
            return NameHashCode(NameHashCode(namespacePart), NameHashCode(namePart));
        }

        public static int NameHashCode(int namespaceHashCode, int nameHashCode)
        {
            return namespaceHashCode ^ nameHashCode;
        }

        /// <summary>
        /// CoreCLR <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/typehashingalgorithms.h#L79">ComputeNestedTypeHashCode</a>
        /// </summary>
        /// <param name="enclosingTypeHashcode">Hash code of the enclosing type</param>
        /// <param name="nestedTypeNameHash">Hash code of the nested type name</param>
        public static int NestedTypeHashCode(int enclosingTypeHashcode, int nestedTypeNameHash)
        {
            return unchecked(enclosingTypeHashcode + RotateLeft(enclosingTypeHashcode, 11)) ^ nestedTypeNameHash;
        }

        /// <summary>
        /// CoreCLR <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/typehashingalgorithms.h#L51">ComputeArrayTypeHashCode</a>
        /// </summary>
        /// <param name="elementTypeHashcode">Hash code representing the array element type</param>
        /// <param name="rank">Array rank</param>
        public static int ArrayTypeHashCode(int elementTypeHashcode, int rank)
        {
            int hashCode = unchecked((int)0xd5313556 + rank);
            if (rank == 1)
            {
                Debug.Assert(hashCode == NameHashCode("System.Array`1"u8));
            }
            hashCode = unchecked(hashCode + RotateLeft(hashCode, 13)) ^ elementTypeHashcode;
            return unchecked(hashCode + RotateLeft(hashCode, 15));
        }

        /// <summary>
        /// CoreCLR <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/typehashingalgorithms.h#L65">ComputePointerTypeHashCode</a>
        /// </summary>
        /// <param name="pointeeTypeHashcode">Hash code of the pointee type</param>
        public static int PointerTypeHashCode(int pointeeTypeHashcode)
        {
            return unchecked(pointeeTypeHashcode + RotateLeft(pointeeTypeHashcode, 5)) ^ 0x12D0;
        }

        /// <summary>
        /// CoreCLR <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/typehashingalgorithms.h#L72">ComputeByrefTypeHashCode</a>
        /// </summary>
        /// <param name="parameterTypeHashcode">Hash code representing the parameter type</param>
        public static int ByrefTypeHashCode(int parameterTypeHashcode)
        {
            return unchecked(parameterTypeHashcode + RotateLeft(parameterTypeHashcode, 7)) ^ 0x4C85;
        }

        /// <summary>
        /// Bitwise left 32-bit rotation with wraparound.
        /// </summary>
        /// <param name="value">Value to rotate</param>
        /// <param name="bitCount">Number of bits</param>
        private static int RotateLeft(int value, int bitCount)
        {
            return (int)BitOperations.RotateLeft((uint)value, bitCount);
        }

        private static uint XXHash32_MixEmptyState()
        {
            // Unlike System.HashCode, these hash values are required to be stable, so don't
            // mixin a random process specific value
            return 374761393U; // Prime5
        }

        private static uint XXHash32_QueueRound(uint hash, uint queuedValue)
        {
            return (BitOperations.RotateLeft((hash + queuedValue * 3266489917U/*Prime3*/), 17)) * 668265263U/*Prime4*/;
        }

        private static uint XXHash32_MixFinal(uint hash)
        {
            hash ^= hash >> 15;
            hash *= 2246822519U/*Prime2*/;
            hash ^= hash >> 13;
            hash *= 3266489917U/*Prime3*/;
            hash ^= hash >> 16;
            return hash;
        }

        public static uint CombineTwoValuesIntoHash(uint value1, uint value2)
        {
            // This matches the behavior of System.HashCode.Combine(value1, value2) as of the time of authoring
            uint hash = XXHash32_MixEmptyState();
            hash += 8;
            hash = XXHash32_QueueRound(hash, value1);
            hash = XXHash32_QueueRound(hash, value2);
            hash = XXHash32_MixFinal(hash);
            return hash;
        }
    }
}

namespace System.Numerics
{
#if !NET
    internal static class BitOperations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateLeft(uint value, int offset) => (value << offset) | (value >> (32 - offset));
    }
#endif
}
