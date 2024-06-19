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
        private static readonly byte[] s_emptyLookup = new byte[ushort.MaxValue + 1];
        /// <summary>An array used to map characters to minterms</summary>
        private readonly byte[]? _lookup;

        /// <summary>Conserve memory if pattern is ascii-only</summary>
        private readonly bool _isAsciiOnly;

        // /// <summary>
        // /// fallback lookup if over 255 minterms
        // /// this is almost never used
        // /// </summary>
        private readonly int[]? _intLookup;

        /// <summary>Create a classifier that maps a character to the ID of its associated minterm.</summary>
        /// <param name="minterms">A BDD for classifying all characters (ASCII and non-ASCII) to their corresponding minterm IDs.</param>
        public MintermClassifier(BDD[] minterms)
        {
            Debug.Assert(minterms.Length > 0, "Requires at least");


            if (minterms.Length == 1)
            {
                // With only a single minterm, the mapping is trivial: everything maps to it (ID 0).
                _lookup = s_emptyLookup;
                return;
            }

            // ascii-only array to save memory
            _isAsciiOnly = true;
            for (int mintermId = 1; mintermId < minterms.Length; mintermId++)
            {
                if (BDDRangeConverter.ToRanges(minterms[mintermId])[^1].Item2 >= 128)
                {
                    _isAsciiOnly = false;
                }
            }

            // It's incredibly rare for a regex to use more than a hundred or two minterms,
            // but we need a fallback just in case.
            if (minterms.Length > 255)
            {
                // over 255 unique sets also means it's never ascii only
                int[] lookup = new int[ushort.MaxValue + 1];
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
                byte[] lookup = new byte[_isAsciiOnly ? 128 : ushort.MaxValue + 1];
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
            if (_isAsciiOnly && (c >= 128))
            {
                return 0;
            }

            // high performance variant would use a span directly.
            // additional memory is saved by using a byte
            return _intLookup is null ? _lookup![c] : _intLookup[c];
        }

        /// <summary>
        /// Whether to use the low memory ascii-only hot loop or the full loop
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAsciiOnly() => _isAsciiOnly;

        /// <summary>
        /// Quick mapping from char to minterm,
        /// can be null if there is over 255 minterms
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[]? ByteLookup() => _lookup;

        /// <summary>
        /// Int lookup for rare cases
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[]? IntLookup() => _intLookup;
    }
}
