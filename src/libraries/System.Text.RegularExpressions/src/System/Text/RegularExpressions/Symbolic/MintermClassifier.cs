// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Classifies characters as their corresponding minterm IDs.</summary>
    /// <remarks>
    /// Minterms are a mechanism for compressing the input character space, or "alphabet",
    /// by creating an equivalence class for all characters treated the same.  For example,
    /// in the expression "[0-9]*", the 10 digits 0 through 9 are all treated the same as each
    /// other, and every other of the 65,526 characters are treated the same as each other,
    /// so there are two minterms, one for the digits, and one for everything else. Minterms
    /// are computed in such a way that every character maps to one and only one minterm.
    /// While in the limit there could be one minterm per character, in practice the number
    /// of minterms for any reasonable expression is way less, and in fact is typically
    /// less than 64.
    /// </remarks>
    internal sealed class MintermClassifier
    {
        /// <summary>An array used to map characters to minterms</summary>
        private readonly byte[]? _lookup;

        /// <summary>
        /// Fallback lookup if over 255 minterms. This is rarely used.
        /// </summary>
        private readonly int[]? _intLookup;

        /// <summary>
        /// Maximum ordinal character for a non-0 minterm, used to conserve memory
        /// Note: this is maximum index allowed for the lookup, the array size is _maxChar + 1
        /// </summary>
        private readonly int _maxChar;

        /// <summary>Create a classifier that maps a character to the ID of its associated minterm.</summary>
        /// <param name="minterms">A BDD for classifying all characters (ASCII and non-ASCII) to their corresponding minterm IDs.</param>
        public MintermClassifier(BDD[] minterms)
        {
            Debug.Assert(minterms.Length > 0, "Requires at least");


            if (minterms.Length == 1)
            {
                // With only a single minterm, the mapping is trivial: everything maps to it (ID 0).
                _lookup = Array.Empty<byte>();
                _maxChar = -1;
                return;
            }

            // attempt to save memory in common cases by allocating only up to the highest char code
            for (int mintermId = 1; mintermId < minterms.Length; mintermId++)
            {
                _maxChar = Math.Max(_maxChar, (int)BDDRangeConverter.ToRanges(minterms[mintermId])[^1].Item2);
            }
            // there is an opportunity to gain around 5% performance for allocating the
            // full 64K, past a certain threshold where maxChar is already large.
            // TODO: what should this threshold be?
            if (_maxChar > 32_000)
            {
                _maxChar = ushort.MaxValue;
            }

            // It's incredibly rare for a regex to use more than a hundred or two minterms,
            // but we need a fallback just in case.
            if (minterms.Length > 255)
            {
                // over 255 unique sets also means it's never ascii only
                int[] lookup = new int[_maxChar + 1];
                for (int mintermId = 1; mintermId < minterms.Length; mintermId++)
                {
                    // precompute all assigned minterm categories
                    (uint, uint)[] mintermRanges = BDDRangeConverter.ToRanges(minterms[mintermId]);
                    foreach ((uint start, uint end) in mintermRanges)
                    {
                        // assign character ranges in bulk
                        Span<int> slice = lookup.AsSpan((int)start, (int)(end + 1 - start));
                        slice.Fill(mintermId);
                    }
                }
                _intLookup = lookup;
            }
            else
            {
                byte[] lookup = new byte[_maxChar + 1];
                for (int mintermId = 1; mintermId < minterms.Length; mintermId++)
                {
                    // precompute all assigned minterm categories
                    (uint, uint)[] mintermRanges = BDDRangeConverter.ToRanges(minterms[mintermId]);
                    foreach ((uint start, uint end) in mintermRanges)
                    {
                        // assign character ranges in bulk
                        Span<byte> slice = lookup.AsSpan((int)start, (int)(end + 1 - start));
                        slice.Fill((byte)mintermId);
                    }
                }
                _lookup = lookup;
            }
        }

        /// <summary>Gets the ID of the minterm associated with the specified character. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetMintermID(int c)
        {
            if (c > _maxChar)
            {
                return 0;
            }

            // high performance inner-loop variant uses the array directly
            return _intLookup is null ? _lookup![c] : _intLookup[c];
        }
        /// <summary>
        /// Gets a quick mapping from char to minterm for the common case when there are &lt;= 255 minterms.
        /// Null if there are greater than 255 minterms.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[]? ByteLookup() => _lookup;

        /// <summary>
        /// Gets a mapping from char to minterm for the rare case when there are &gt;= 255 minterms.
        /// Null in the common case where there are fewer than 255 minterms.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[]? IntLookup() => _intLookup;

        /// <summary>
        /// Whether the full 64K char lookup is allocated.
        /// This accelerates the minterm mapping by removing an if-else case,
        /// and is only considered for the common &lt;= 255 minterms case
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFullLookup() => _lookup is not null && _lookup.Length == ushort.MaxValue + 1;

        /// <summary>
        /// Maximum ordinal character for a non-0 minterm, used to conserve memory
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MaxChar() => (_lookup?.Length ?? _intLookup!.Length) - 1;
    }
}
