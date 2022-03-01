// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Represents a bitvector of arbitrary length (i.e. number of bits).
    /// </summary>
    internal sealed class BV : IComparable
    {
        /// <summary>
        /// Stores the bits in an array of 64-bit integers. If Length is not evenly divisible by 64 then the remaining
        /// bits are in the least significant bits of the last element.
        /// </summary>
        private readonly ulong[] _blocks;

        /// <summary>
        /// Number of bits.
        /// </summary>
        internal readonly int Length;

        /// <summary>
        /// Cache for the lazily computed hash code.
        /// </summary>
        private int? _hashcode;

        /// <summary>
        /// Returns true iff the i'th bit is 1
        /// </summary>
        internal bool this[int i]
        {
            get
            {
                Debug.Assert(i >= 0 && i < Length);
                int k = i / 64;
                int j = i % 64;
                return (_blocks[k] & (1ul << j)) != 0;
            }
            private set
            {
                Debug.Assert(i >= 0 && i < Length);
                int k = i / 64;
                int j = i % 64;
                if (value)
                {
                    //set the j'th bit of the k'th block to 1
                    _blocks[k] |= 1ul << j;
                }
                else
                {
                    //set the j'th bit of the k'th block to 0
                    _blocks[k] &= ~(1ul << j);
                }
            }
        }

        private BV(int K)
        {
            Length = K;
            _blocks = new ulong[((K - 1) / 64) + 1];
        }

        private BV(int K, ulong[] blocks)
        {
            Length = K;
            _blocks = blocks;
        }

        /// <summary>
        /// Constructs a bitvector of K bits initially all 0.
        /// </summary>
        public static BV CreateFalse(int K) => new(K);

        /// <summary>
        /// Constructs a bitvector of K bits initially all 1.
        /// </summary>
        public static BV CreateTrue(int K) => ~CreateFalse(K);

        /// <summary>
        /// Returns the bitvector of length K with its i'th bit set to 1 all other bits are 0.
        /// </summary>
        public static BV CreateSingleBit(int K, int i)
        {
            BV bv = new BV(K);
            bv[i] = true;
            return bv;
        }

        /// <summary>
        /// Bitwise AND
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BV operator &(BV x, BV y)
        {
            Debug.Assert(x.Length == y.Length);

            var blocks = new ulong[x._blocks.Length];
            // produce new blocks as the bitwise AND of the arguments' blocks
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = x._blocks[i] & y._blocks[i];
            }
            return new BV(x.Length, blocks);
        }

        /// <summary>
        /// Bitwise OR
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BV operator |(BV x, BV y)
        {
            Debug.Assert(x.Length == y.Length);

            var blocks = new ulong[x._blocks.Length];
            // produce new blocks as the bitwise OR of the arguments' blocks
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = x._blocks[i] | y._blocks[i];
            }
            return new BV(x.Length, blocks);
        }

        /// <summary>
        /// Bitwise XOR
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BV operator ^(BV x, BV y)
        {
            Debug.Assert(x.Length == y.Length);

            var blocks = new ulong[x._blocks.Length];
            // produce new blocks as the bitwise XOR of the arguments' blocks
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = x._blocks[i] ^ y._blocks[i];
            }
            return new BV(x.Length, blocks);
        }

        /// <summary>
        /// Bitwise NOT
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BV operator ~(BV x)
        {
            var blocks = new ulong[x._blocks.Length];
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = ~x._blocks[i];
            }

            int j = x.Length % 64;
            if (j > 0)
            {
                // the number of bits is not a precise multiple of 64
                // reset the extra bits in the last block to 0
                int last = (x.Length - 1) / 64;
                blocks[last] &= (1ul << j) - 1;
            }

            return new BV(x.Length, blocks);
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => CompareTo(obj) == 0;

        public override int GetHashCode()
        {
            // if the hash code hasn't been calculated yet, do so before returning it
            if (_hashcode == null)
            {
                _hashcode = Length.GetHashCode();
                for (int i = 0; i < _blocks.Length; i++)
                {
                    _hashcode = HashCode.Combine(_hashcode, _blocks[i].GetHashCode());
                }
            }
            return (int)_hashcode;
        }

        public int CompareTo(object? obj)
        {
            if (obj is not BV that)
                return 1;

            if (Length != that.Length)
                return Length.CompareTo(that.Length);

            // compare all blocks starting from the last one (i.e. most significant)
            for (int i = _blocks.Length - 1; i >= 0; i--)
            {
                if (_blocks[i] < that._blocks[i])
                    return -1;

                if (_blocks[i] > that._blocks[i])
                    return 1;
            }

            //all blocks were equal
            return 0;
        }
    }
}
