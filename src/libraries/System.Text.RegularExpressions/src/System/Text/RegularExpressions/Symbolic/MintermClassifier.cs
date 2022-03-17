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
        private static readonly int[] AllAsciiIsZeroMintermArray = new int[128];

        /// <summary>Array providing fast mapping from an ASCII character (the array index) to its corresponding minterm ID.</summary>
        private readonly int[] _ascii;
        /// <summary>A multi-terminal BDD for mapping any non-ASCII character to its associated minterm ID.</summary>
        /// <remarks>
        /// The use of a multi-terminal BDD here is an implementation detail.  Should we decide its important to optimize non-ASCII inputs further,
        /// or to consolidate the mechanism with the other engines, an alternatie lookup algorithm / data structure could be employed.
        /// </remarks>
        private readonly BDD _nonAscii;

        /// <summary>Create a classifier that maps a character to the ID of its associated minterm.</summary>
        /// <param name="minterms">A BDD for classifying all characters (ASCII and non-ASCII) to their corresponding minterm IDs.</param>
        public MintermClassifier(BDD[] minterms)
        {
            Debug.Assert(minterms.Length > 0, "Requires at least");

            CharSetSolver solver = CharSetSolver.Instance;

            if (minterms.Length == 1)
            {
                // With only a single minterm, the mapping is trivial: everything maps to it (ID 0).
                // For ASCII, use an array containing all zeros.  For non-ASCII, use a BDD that maps everything to 0.
                _ascii = AllAsciiIsZeroMintermArray;
                _nonAscii = solver.ReplaceTrue(BDD.True, 0);
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

            // Now that we have our mapping that supports any input character, we want to optimize for
            // ASCII inputs.  Rather than forcing every input ASCII character to consult the BDD at match
            // time, we precompute a lookup table, where each ASCII character can be used to index into the
            // array to determine the ID for its corresponding minterm.
            var ascii = new int[128];
            for (int i = 0; i < ascii.Length; i++)
            {
                ascii[i] = anyCharacterToMintermId.Find(i);
            }
            _ascii = ascii;

            // We can also further optimize the BDD in two ways:
            // 1. We can now remove the ASCII characters from it, as we'll always consult the lookup table first
            //    for ASCII inputs and thus will never use the BDD for them.  While optional (skipping this step will not
            //    affect correctness), removing the ASCII values from the BDD reduces the size of the multi-terminal BDD.
            // 2. We can check if every character now maps to the same minterm ID (the same terminal in the
            //    multi-terminal BDD).  This can be relatively common after (1) above is applied, as many
            //    patterns don't distinguish between any non-ASCII characters (e.g. "[0-9]*").  If every character
            //    in the BDD now maps to the same minterm, we can replace the BDD with a much simpler/faster/smaller one.
            BDD nonAsciiBDD = solver.And(anyCharacterToMintermId, solver._nonAscii);
            nonAsciiBDD = nonAsciiBDD.IsEssentiallyBoolean(out BDD? singleTerminalBDD) ? singleTerminalBDD : nonAsciiBDD;
            _nonAscii = nonAsciiBDD;
        }

        /// <summary>Gets the ID of the minterm associated with the specified character.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetMintermID(int c)
        {
            int[] ascii = _ascii;
            return (uint)c < (uint)ascii.Length ? ascii[c] : _nonAscii.Find(c);
        }
    }
}
