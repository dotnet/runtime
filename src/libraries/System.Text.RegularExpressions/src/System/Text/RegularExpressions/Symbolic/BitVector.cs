// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents an immutable bit vector of an arbitrary number of bits.</summary>
    /// <remarks>
    /// This differs from <see cref="BitArray"/> in that it's a value type rather than a reference type
    /// and it exposes the ability to quickly compute equality and a comparison.
    /// </remarks>
    internal struct BitVector : IComparable<BitVector>, IEquatable<BitVector>
    {
        /// <summary>Stores the bits in an array of 64-bit integers.</summary>
        /// <remarks>
        /// If Length is not evenly divisible by 64 then the remainingbits are
        /// in the least significant bits of the last element.
        /// </remarks>
        private readonly ulong[] _blocks;
        /// <summary>The number of bits represented by the BitVector.</summary>
        public readonly int Length;
        /// <summary>Lazily-computed hash code value.</summary>
        private int? _hashcode;

        /// <summary>Initializes the <see cref="BitVector"/> to be of the specified length and contain all false bits.</summary>
        private BitVector(int length)
        {
            Debug.Assert(length > 0);
            Length = length;
            _blocks = new ulong[((length - 1) / 64) + 1];
            _hashcode = null;
        }

        /// <summary>Initializes the <see cref="BitVector"/> with the specified length and blocks.</summary>
        /// <remarks>The provided array is stored directly without copy.</remarks>
        private BitVector(int length, ulong[] blocks)
        {
            Debug.Assert(length > 0);
            Length = length;
            _blocks = blocks;
            _hashcode = null;
        }

        /// <summary>Creates a <see cref="BitVector"/> of the specified length containing all bits not set.</summary>
        public static BitVector CreateFalse(int length) => new BitVector(length);

        /// <summary>Creates a <see cref="BitVector"/> of the specified length containing all bits set.</summary>
        public static BitVector CreateTrue(int length)
        {
            var bv = new BitVector(length);
            Array.Fill(bv._blocks, ulong.MaxValue);
            bv.ClearRemainderBits();
            return bv;
        }

        /// <summary>Creates a <see cref="BitVector"/> of the specified length containing all bits not set except for the specified bit, which is set.</summary>
        public static BitVector CreateSingleBit(int length, int i)
        {
            var bv = new BitVector(length);
            bv.Set(i);
            return bv;
        }

        /// <summary>Gets the value of the i'th bit, true for 1 and false for 0.</summary>
        internal readonly bool this[int i]
        {
            get
            {
                Debug.Assert(i >= 0 && i < Length);
                (int block, int bit) = Math.DivRem(i, 64);
                return (_blocks[block] & (1ul << bit)) != 0;
            }
        }

        private void Set(int i)
        {
            Debug.Assert(i >= 0 && i < Length);
            (int block, int bit) = Math.DivRem(i, 64);
            _blocks[block] |= 1ul << bit;
        }

        /// <summary>Create a new <see cref="BitVector"/> that is the bitwise-and of the two input vectors.</summary>
        public static BitVector And(BitVector x, BitVector y)
        {
            Debug.Assert(x.Length == y.Length);

            ulong[] xBlocks = x._blocks;
            ulong[] yBlocks = y._blocks;

            var blocks = new ulong[xBlocks.Length];
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = xBlocks[i] & yBlocks[i];
            }

            return new BitVector(x.Length, blocks);
        }

        /// <summary>Create a new <see cref="BitVector"/> that is the bitwise-or of the two input vectors.</summary>
        public static BitVector Or(BitVector x, BitVector y)
        {
            Debug.Assert(x.Length == y.Length);

            ulong[] xBlocks = x._blocks;
            ulong[] yBlocks = y._blocks;

            var blocks = new ulong[xBlocks.Length];
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = xBlocks[i] | yBlocks[i];
            }

            return new BitVector(x.Length, blocks);
        }

        /// <summary>Create a new <see cref="BitVector"/> that is the bitwise-or of the input vectors.</summary>
        public static BitVector Or(ReadOnlySpan<BitVector> bitVectors)
        {
            Debug.Assert(!bitVectors.IsEmpty);

            BitVector firstOther = bitVectors[0];

            var blocks = new ulong[firstOther._blocks.Length];
            foreach (BitVector other in bitVectors)
            {
                ulong[] otherBlocks = other._blocks;
                for (int i = 0; i < blocks.Length; i++)
                {
                    blocks[i] |= otherBlocks[i];
                }
            }

            return new BitVector(firstOther.Length, blocks);
        }

        /// <summary>Create a new <see cref="BitVector"/> that is the bitwise-not of the input vector.</summary>
        public static BitVector Not(BitVector x)
        {
            ulong[] xBlocks = x._blocks;

            var blocks = new ulong[xBlocks.Length];
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = ~xBlocks[i];
            }

            var bv = new BitVector(x.Length, blocks);
            bv.ClearRemainderBits();
            return bv;
        }

        /// <summary>Clears any bits in <see cref="_blocks"/> not part of <see cref="Length"/>.</summary>
        /// <remarks>
        /// If the number of bits is not a precise multiple of 64, this resets the extra bits in the last block
        /// to 0. This enables comparison of BitVectors quickly, without having to special-case the remainder, as
        /// all remainders are normalized to contain 0.
        /// </remarks>
        private void ClearRemainderBits()
        {
            int remainder = Length % 64;
            if (remainder != 0)
            {
                int last = (Length - 1) / 64;
                _blocks[last] &= (1ul << remainder) - 1;
            }
        }

        public override int GetHashCode()
        {
            // Lazily compute and store the hash code if it hasn't yet been computed.
            if (_hashcode == null)
            {
                HashCode hc = default;
                hc.Add(Length);
                if (_blocks is ulong[] blocks) // may be null in case of a default struct
                {
                    hc.AddBytes(MemoryMarshal.AsBytes<ulong>(blocks));
                }

                _hashcode = hc.ToHashCode();
            }

            return _hashcode.GetValueOrDefault();
        }

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is BitVector other && Equals(other);

        public bool Equals(BitVector other) =>
            Length == other.Length &&
            MemoryExtensions.SequenceEqual(new ReadOnlySpan<ulong>(_blocks), new ReadOnlySpan<ulong>(other._blocks));

        public int CompareTo(BitVector other) =>
            Length != other.Length ? Length.CompareTo(other.Length) :
            MemoryExtensions.SequenceCompareTo(new ReadOnlySpan<ulong>(_blocks), new ReadOnlySpan<ulong>(other._blocks));
    }
}
