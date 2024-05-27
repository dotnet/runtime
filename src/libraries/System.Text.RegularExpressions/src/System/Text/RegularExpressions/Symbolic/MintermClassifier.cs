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
        /// <summary>An array used when there's a single minterm, in order to map every ASCII character to it trivially.</summary>
        // private static readonly int[] AllAsciiIsZeroMintermArray = new int[128];
        private readonly int[] _lookup;

        // /// <summary>A multi-terminal BDD for mapping any non-ASCII character to its associated minterm ID.</summary>
        // /// <remarks>
        // /// The use of a multi-terminal BDD here is an implementation detail.  Should we decide its important to optimize non-ASCII inputs further,
        // /// or to consolidate the mechanism with the other engines, an alternatie lookup algorithm / data structure could be employed.
        // /// </remarks>
        // private readonly BDD _nonAscii;

        /// <summary>Create a classifier that maps a character to the ID of its associated minterm.</summary>
        /// <param name="minterms">A BDD for classifying all characters (ASCII and non-ASCII) to their corresponding minterm IDs.</param>
        /// <param name="solver">The character set solver to use.</param>
        public MintermClassifier(BDD[] minterms, CharSetSolver solver)
        {
            Debug.Assert(minterms.Length > 0, "Requires at least");

            var lookup = new int[ushort.MaxValue + 1];
            if (minterms.Length == 1)
            {
                // With only a single minterm, the mapping is trivial: everything maps to it (ID 0).
                // For ASCII, use an array containing all zeros.  For non-ASCII, use a BDD that maps everything to 0.
                _lookup = lookup;
                // _nonAscii = solver.ReplaceTrue(BDD.True, 0);
                return;
            }

            // Create a multi-terminal BDD for mapping any character to its associated minterm.
            BDD anyCharacterToMintermId = BDD.False;
            for (int i = 0; i < minterms.Length; i++)
            {
                // Each supplied minterm BDD decides whether a given character maps to it or not.
                // We need to combine all of those into a multi-terminal BDD that decides which
                // minterm a character maps to.  To do that, we take each minterm BDD and replace
                // its True result with the ID of the minterm, such that a character that would
                // have returned True for that BDD now returns the minterm ID.
                BDD charToTargetMintermId = solver.ReplaceTrue(minterms[i], i);

                // Now union this BDD with the multi-terminal BDD we've built up thus far. Unioning
                // is valid because every character belongs to exactly one minterm and thus will
                // only map to an ID instead of False in exactly one of the input BDDs.
                anyCharacterToMintermId = solver.Or(anyCharacterToMintermId, charToTargetMintermId);
            }

            // assign minterm category for every char
            // unused characters in minterm 0 get mapped to zero
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
            _lookup = lookup;
        }

        /// <summary>Gets the ID of the minterm associated with the specified character.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetMintermID(int c)
        {
            return _lookup[c];
        }
        public int[] Lookup => _lookup;
    }
}
