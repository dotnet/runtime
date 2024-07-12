// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
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
        /// <summary>Mapping for characters to minterms, used in the vast majority case when there are less than 256 minterms.</summary>
        /// <remarks>_lookup[char] provides the minterm ID. If char &gt;= _lookup.Length, its minterm is 0.</remarks>
        private readonly byte[]? _lookup;

        /// <summary>Mapping for characters to minterms, used when there are at least 256 minterms. This is rarely used.</summary>
        /// <remarks>_intLookup[char] provides the minterm ID. If char &gt;= _intLookup.Length, its minterm is 0.</remarks>
        private readonly int[]? _intLookup;

        /// <summary>Create a classifier that maps a character to the ID of its associated minterm.</summary>
        /// <param name="minterms">A BDD for classifying all characters (ASCII and non-ASCII) to their corresponding minterm IDs.</param>
        public MintermClassifier(BDD[] minterms)
        {
            Debug.Assert(minterms.Length > 0, "Requires at least");

            if (minterms.Length == 1)
            {
                // With only a single minterm, the mapping is trivial: everything maps to it (ID 0).
                _lookup = [];
                return;
            }

            // Compute all minterm ranges. We do this here in order to determine the maximum character value
            // in order to size the lookup array to minimize steady-state memory consumption of the potentially
            // large lookup array. We prefer to use the byte[] _lookup when possible, in order to keep memory
            // consumption to a minimum; doing so accomodates up to 255 minterms, which is the vast majority case.
            // However, when there are more than 255 minterms, we need to use int[] _intLookup. We rent an object[]
            // rather than a (uint,uint)[][] to avoid the extra type pressure on the ArrayPool (object[]s are common,
            // (uint,uint)[][]s much less so).
            object[] arrayPoolArray = ArrayPool<object>.Shared.Rent(minterms.Length);
            Span<object> charRangesPerMinterm = arrayPoolArray.AsSpan(0, minterms.Length);

            int maxChar = -1;
            for (int mintermId = 1; mintermId < minterms.Length; mintermId++)
            {
                (uint, uint)[] ranges = BDDRangeConverter.ToRanges(minterms[mintermId]);
                charRangesPerMinterm[mintermId] = ranges;
                maxChar = Math.Max(maxChar, (int)ranges[^1].Item2);
            }

            // It's incredibly rare for a regex to use more than a couple hundred minterms,
            // but we need a fallback just in case. (Over 128 unique sets also means it's never ASCII only.)
            if (minterms.Length > 255)
            {
                _intLookup = CreateLookup<int>(minterms, charRangesPerMinterm, maxChar);
            }
            else
            {
                _lookup = CreateLookup<byte>(minterms, charRangesPerMinterm, maxChar);
            }

            // Return the rented array. We clear it before returning it in order to avoid all the ranges arrays being kept alive.
            charRangesPerMinterm.Clear();
            ArrayPool<object>.Shared.Return(arrayPoolArray);

            // Creates the lookup array. charRangesPerMinterm needs to have already been populated with (uint, uint)[] instances.
            static T[] CreateLookup<T>(BDD[] minterms, ReadOnlySpan<object> charRangesPerMinterm, int _maxChar) where T : IBinaryInteger<T>
            {
                T[] lookup = new T[_maxChar + 1];
                for (int mintermId = 1; mintermId < minterms.Length; mintermId++)
                {
                    // Each minterm maps to a range of characters. Set each of the characters in those ranges to the corresponding minterm.
                    foreach ((uint start, uint end) in ((uint, uint)[])charRangesPerMinterm[mintermId])
                    {
                        lookup.AsSpan((int)start, (int)(end + 1 - start)).Fill(T.CreateTruncating(mintermId));
                    }
                }

                return lookup;
            }
        }

        /// <summary>Gets the ID of the minterm associated with the specified character. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetMintermID(int c)
        {
            if (_lookup is not null)
            {
                byte[] lookup = _lookup;
                return (uint)c < (uint)lookup.Length ? lookup[c] : 0;
            }
            else
            {
                Debug.Assert(_intLookup is not null);

                int[] lookup = _intLookup;
                return (uint)c < (uint)lookup.Length ? lookup[c] : 0;
            }
        }
        /// <summary>
        /// Gets a quick mapping from char to minterm for the common case when there are &lt;= 255 minterms.
        /// Null if there are greater than 255 minterms.
        /// </summary>
        public byte[]? ByteLookup => _lookup;

        /// <summary>
        /// Maximum ordinal character for a non-0 minterm, used to conserve memory
        /// </summary>
        public int MaxChar => (_lookup?.Length ?? _intLookup!.Length) - 1;
    }
}
