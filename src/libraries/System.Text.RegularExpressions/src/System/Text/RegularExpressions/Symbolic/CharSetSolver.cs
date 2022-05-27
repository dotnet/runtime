// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Provides functionality to build character sets represented as <see cref="BDD"/>s
    /// and to perform boolean operations over those sets.
    /// </summary>
    internal sealed class CharSetSolver : ISolver<BDD>
    {
        /// <summary>BDD for each ASCII character that returns true for that one character.</summary>
        /// <remarks>This cache is shared amongst all CharSetSolver instances and is accessed in a thread-safe manner.</remarks>
        private static readonly BDD?[] s_asciiCache = new BDD[128];
        /// <summary>BDD that returns true for every non-ASCII character.</summary>
        /// <remarks>This instance is shared amongst all CharSetSolver instances and is accessed in a thread-safe manner.</remarks>
        private static BDD? s_nonAscii;

        /// <summary>Generator for minterms, lazily initialized on first use.</summary>
        private MintermGenerator<BDD>? _mintermGenerator;
        /// <summary>Cache of BDD instances created by this solver.</summary>
        /// <remarks>
        /// Cache of BDD instances created by this solver: two BDDs with the same ordinal and identical children references
        /// will result in using the same BDD object.  BDD's Equals implementation does that shallow check, such that BDD
        /// instances can be looked up by the ordinal/one/zero tuples, and these internalized BDD instances are then able
        /// to be compared with == for reference equality.  The algorithms employed do not rely on this equality, but
        /// benefit from it; the more equal BDDs that are actually found to be equal, the better the algorithms will perform.
        /// </remarks>
        private readonly Dictionary<(int ordinal, BDD? one, BDD? zero), BDD> _bddCache = new();
        /// <summary>Cache of Boolean operations over BDDs and the BDDs they produce.</summary>
        /// <remarks>
        /// This cache is necessary for the recursive operation algorithms to be guaranteed linear time.
        /// A well-crafted character class could otherwise cause execution time to be exponential.
        /// </remarks>
        private readonly Dictionary<(BooleanOperation op, BDD a, BDD? b), BDD> _operationCache = new();

        /// <summary>Gets a BDD that contains every non-ASCII character.</summary>
        public BDD NonAscii =>
            s_nonAscii ??
            Interlocked.CompareExchange(ref s_nonAscii, CreateSetFromRange('\x80', '\uFFFF'), null) ??
            s_nonAscii;

        /// <summary>Creates a BDD that contains only the specified character.</summary>
        public BDD CreateFromChar(char c)
        {
            BDD?[] ascii = s_asciiCache;
            if (c < (uint)ascii.Length)
            {
                // ASCII: return a cached BDD.
                return
                    ascii[c] ??
                    Interlocked.CompareExchange(ref ascii[c], CreateBdd(c), null) ??
                    ascii[c]!;
            }

            // Non-ascii: just create a new BDD.
            return CreateBdd(c);

            BDD CreateBdd(ushort c)
            {
                BDD bdd = BDD.True;
                for (int k = 0; k < 16; k++)
                {
                    bdd = (c & (1 << k)) != 0 ?
                        GetOrCreateBDD(k, bdd, BDD.False) :
                        GetOrCreateBDD(k, BDD.False, bdd);
                }

                return bdd;
            }
        }

#if DEBUG
        /// <summary>Creates a BDD that contains all of the characters in each range.</summary>
        internal BDD CreateSetFromRanges(List<(char Lower, char Upper)> ranges)
        {
            BDD bdd = Empty;
            foreach ((char Lower, char Upper) range in ranges)
            {
                bdd = Or(bdd, CreateSetFromRange(range.Lower, range.Upper));
            }

            return bdd;
        }
#endif

        /// <summary>Identity function for <paramref name="set"/>, since <paramref name="set"/> is already a <see cref="BDD"/>.</summary>
        public BDD ConvertFromBDD(BDD set, CharSetSolver _) => set;

        /// <summary>Identity function for <paramref name="set"/>, since <paramref name="set"/> is already a <see cref="BDD"/>.</summary>
        public BDD ConvertToBDD(BDD set, CharSetSolver _) => set;

        /// <summary>Returns null, as minterms are not relevant to <see cref="CharSetSolver"/>.</summary>
        BDD[]? ISolver<BDD>.GetMinterms() => null;

        /// <summary>Formats the contents of the specified set for human consumption.</summary>
        string ISolver<BDD>.PrettyPrint(BDD characterClass, CharSetSolver solver) => PrettyPrint(characterClass);

        /// <summary>Formats the contents of the specified set for human consumption.</summary>
        public string PrettyPrint(BDD set)
        {
            // Provide simple representations for character classes that match nothing and everything.
            if (set.IsEmpty)
            {
                return "[]";
            }
            else if (set.IsFull)
            {
                return @"."; // technically this is only accurate with RegexOptions.Singleline, but it's cleaner than using [\s\S]
            }

            // Provide simple representations for the built-in word, string, and digit classes
            BDD w = UnicodeCategoryConditions.WordLetter(this);
            if (set == w)
            {
                return @"\w";
            }
            else if (set == Not(w))
            {
                return @"\W";
            }

            BDD s = UnicodeCategoryConditions.WhiteSpace;
            if (set == s)
            {
                return @"\s";
            }
            else if (set == Not(s))
            {
                return @"\S";
            }

            BDD d = UnicodeCategoryConditions.GetCategory(UnicodeCategory.DecimalDigitNumber);
            if (set == d)
            {
                return @"\d";
            }
            else if (set == Not(d))
            {
                return @"\D";
            }

            // For everything else, output a series of ranges.
            var rcc = new RegexCharClass();
            foreach ((uint, uint) range in BDDRangeConverter.ToRanges(set))
            {
                rcc.AddRange((char)range.Item1, (char)range.Item2);
            }
            return RegexCharClass.DescribeSet(rcc.ToStringClass());
        }

        /// <summary>Unions two <see cref="BDD"/>s to produce a new <see cref="BDD"/>.</summary>
        public BDD Or(BDD set1, BDD set2) => ApplyBinaryOp(BooleanOperation.Or, set1, set2);

        /// <summary>Unions <see cref="BDD"/>s to produce a new <see cref="BDD"/>.</summary>
        public BDD Or(ReadOnlySpan<BDD> sets)
        {
            if (sets.Length == 0)
            {
                return Empty;
            }

            BDD result = sets[0];
            for (int i = 1; i < sets.Length; i++)
            {
                result = Or(result, sets[i]);
            }
            return result;
        }

        /// <summary>Intersects two <see cref="BDD"/>s to produce a new <see cref="BDD"/>.</summary>
        public BDD And(BDD a, BDD b) => ApplyBinaryOp(BooleanOperation.And, a, b);

        /// <summary>Intersects <see cref="BDD"/>s to produce a new <see cref="BDD"/>.</summary>
        public BDD And(ReadOnlySpan<BDD> sets)
        {
            if (sets.Length == 0)
            {
                return Empty;
            }

            BDD result = sets[0];
            for (int i = 1; i < sets.Length; i++)
            {
                result = And(result, sets[i]);
            }
            return result;
        }

        /// <summary>Negates a <see cref="BDD"/> to produce a new complement <see cref="BDD"/>.</summary>
        public BDD Not(BDD set)
        {
            if (set == Empty)
            {
                return Full;
            }

            if (set == Full)
            {
                return Empty;
            }

            Debug.Assert(!set.IsLeaf, "Did not expect multi-terminal");
            if (!_operationCache.TryGetValue((BooleanOperation.Not, set, null), out BDD? result))
            {
                _operationCache[(BooleanOperation.Not, set, null)] = result = GetOrCreateBDD(set.Ordinal, Not(set.One), Not(set.Zero));
            }

            return result;
        }

        /// <summary>
        /// Applies the binary Boolean operation <paramref name="op"/> and constructs a new
        /// <see cref="BDD"/> recursively from <paramref name="set1"/> and <paramref name="set2"/>.
        /// </summary>
        private BDD ApplyBinaryOp(BooleanOperation op, BDD set1, BDD set2)
        {
            Debug.Assert(op is BooleanOperation.Or or BooleanOperation.And or BooleanOperation.Xor);

            // Handle base cases (when one of a or b is True or False or when a == b)
            switch (op)
            {
                case BooleanOperation.Or:
                    if (set1 == Empty) return set2;
                    if (set2 == Empty) return set1;
                    if (set1 == Full || set2 == Full) return Full;
                    if (set1 == set2) return set1;
                    break;

                case BooleanOperation.And:
                    if (set1 == Full) return set2;
                    if (set2 == Full) return set1;
                    if (set1 == Empty || set2 == Empty) return Empty;
                    if (set1 == set2) return set1;
                    break;

                case BooleanOperation.Xor:
                    if (set1 == Empty) return set2;
                    if (set2 == Empty) return set1;
                    if (set1 == set2) return Empty;
                    if (set1 == Full) return Not(set2);
                    if (set2 == Full) return Not(set1);
                    break;
            }

            // Order operands by hash code to increase cache hits
            if (set1.GetHashCode() > set2.GetHashCode())
            {
                (set2, set1) = (set1, set2);
            }

            Debug.Assert(!set1.IsLeaf || !set2.IsLeaf, "Did not expect multi-terminal case");
            if (!_operationCache.TryGetValue((op, set1, set2), out BDD? result))
            {
                BDD one, two;
                int ordinal;
                if (set1.IsLeaf || set2.Ordinal > set1.Ordinal)
                {
                    Debug.Assert(!set2.IsLeaf);
                    one = ApplyBinaryOp(op, set1, set2.One);
                    two = ApplyBinaryOp(op, set1, set2.Zero);
                    ordinal = set2.Ordinal;
                }
                else if (set2.IsLeaf || set1.Ordinal > set2.Ordinal)
                {
                    one = ApplyBinaryOp(op, set1.One, set2);
                    two = ApplyBinaryOp(op, set1.Zero, set2);
                    ordinal = set1.Ordinal;
                }
                else
                {
                    one = ApplyBinaryOp(op, set1.One, set2.One);
                    two = ApplyBinaryOp(op, set1.Zero, set2.Zero);
                    ordinal = set1.Ordinal;
                }

                _operationCache[(op, set1, set2)] = result = one == two ? one : GetOrCreateBDD(ordinal, one, two);
            }

            return result;
        }

        /// <summary>Gets the full set.</summary>
        public BDD Full => BDD.True;

        /// <summary>Gets the empty set.</summary>
        public BDD Empty => BDD.False;

        /// <summary>Gets whether the set contains every value.</summary>
        public bool IsFull(BDD set) => ApplyBinaryOp(BooleanOperation.Xor, set, Full) == Empty;

        /// <summary>Gets whether the set contains no values.</summary>
        public bool IsEmpty(BDD set) => set == Empty;

        /// <summary>Generate all non-overlapping Boolean combinations of a set of BDDs.</summary>
        public List<BDD> GenerateMinterms(HashSet<BDD> sets) =>
            (_mintermGenerator ??= new(this)).GenerateMinterms(sets);

        /// <summary>
        /// Create a <see cref="BDD"/> representing the set of values in the range
        /// from <paramref name="lower"/> to <paramref name="upper"/>, inclusive.
        /// </summary>
        public BDD CreateSetFromRange(char lower, char upper)
        {
            const int MaxBit = 15; // most significant bit of a 16-bit char
            return
                upper < lower ? Empty :
                upper == lower ? CreateFromChar(lower) :
                CreateSetFromRangeImpl(lower, upper, MaxBit);

            BDD CreateSetFromRangeImpl(uint lower, uint upper, int maxBit)
            {
                // Mask with 1 at position of maxBit
                uint mask = 1u << maxBit;

                if (mask == 1) // Base case for least significant bit
                {
                    return
                        upper == 0 ? GetOrCreateBDD(maxBit, Empty, Full) : // lower must also be 0
                        lower == 1 ? GetOrCreateBDD(maxBit, Full, Empty) : // upper must also be 1
                        Full; // Otherwise both 0 and 1 are included
                }

                // Check if range includes all numbers up to bit
                if (lower == 0 && upper == ((mask << 1) - 1))
                {
                    return Full;
                }

                // Mask out the highest bit for the first and last elements in the range
                uint lowerMasked = lower & mask;
                uint upperMasked = upper & mask;

                if (upperMasked == 0)
                {
                    // Highest value in range doesn't have maxBit set, so the one branch is empty
                    BDD zero = CreateSetFromRangeImpl(lower, upper, maxBit - 1);
                    return GetOrCreateBDD(maxBit, Empty, zero);
                }
                else if (lowerMasked == mask)
                {
                    // Lowest value in range has maxBit set, so the zero branch is empty
                    BDD one = CreateSetFromRangeImpl(lower & ~mask, upper & ~mask, maxBit - 1);
                    return GetOrCreateBDD(maxBit, one, Empty);
                }
                else // Otherwise the range straddles (1<<maxBit) and thus both cases need to be considered
                {
                    // If zero then less significant bits are from lower bound to maximum value with maxBit-1 bits
                    BDD zero = CreateSetFromRangeImpl(lower, mask - 1, maxBit - 1);
                    // If one then less significant bits are from zero to the upper bound with maxBit stripped away
                    BDD one = CreateSetFromRangeImpl(0, upper & ~mask, maxBit - 1);
                    return GetOrCreateBDD(maxBit, one, zero);
                }
            }
        }

        /// <summary>
        /// Replace the True node in the BDD by a non-Boolean terminal.
        /// Observe that the Ordinal of False is -1 and the Ordinal of True is -2.
        /// </summary>
        public BDD ReplaceTrue(BDD bdd, int terminal)
        {
            Debug.Assert(terminal >= 0);

            BDD leaf = GetOrCreateBDD(terminal, null, null);
            return ReplaceTrueImpl(bdd, leaf, new Dictionary<BDD, BDD>());

            BDD ReplaceTrueImpl(BDD bdd, BDD leaf, Dictionary<BDD, BDD> cache)
            {
                if (bdd == Full)
                    return leaf;

                if (bdd.IsLeaf)
                    return bdd;

                if (!cache.TryGetValue(bdd, out BDD? result))
                {
                    BDD one = ReplaceTrueImpl(bdd.One, leaf, cache);
                    BDD zero = ReplaceTrueImpl(bdd.Zero, leaf, cache);
                    cache[bdd] = result = GetOrCreateBDD(bdd.Ordinal, one, zero);
                }

                return result;
            }
        }

        private BDD GetOrCreateBDD(int ordinal, BDD? one, BDD? zero)
        {
            ref BDD? bdd = ref CollectionsMarshal.GetValueRefOrAddDefault(_bddCache, (ordinal, one, zero), out _);
            return bdd ??= new BDD(ordinal, one, zero);
        }

        /// <summary>Kinds of Boolean operations.</summary>
        private enum BooleanOperation
        {
            Or,
            And,
            Xor,
            Not,
        }
    }
}
