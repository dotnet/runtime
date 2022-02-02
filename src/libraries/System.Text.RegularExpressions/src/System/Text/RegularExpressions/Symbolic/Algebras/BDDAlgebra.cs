// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Boolean operations over BDDs.
    /// </summary>
    internal enum BoolOp
    {
        Or,
        And,
        Xor,
        Not
    }

    /// <summary>
    /// Boolean algebra for Binary Decision Diagrams. Boolean operations on BDDs are cached for efficiency. The
    /// IBooleanAlgebra interface implemented by this class is thread safe.
    /// TBD: policy for clearing/reducing the caches when they grow too large.
    /// Ultimately, the caches are crucial for efficiency, not for correctness.
    /// </summary>
    internal abstract class BDDAlgebra : IBooleanAlgebra<BDD>
    {
        /// <summary>
        /// Operation cache for Boolean operations over BDDs.
        /// </summary>
        private readonly ConcurrentDictionary<(BoolOp op, BDD a, BDD? b), BDD> _opCache = new();

        /// <summary>
        /// Internalize the creation of BDDs so that two BDDs with same ordinal and identical children are the same object.
        /// The algorithms do not rely on 100% internalization
        /// (they could but this would make it difficult (or near impossible) to clear caches.
        /// Allowing distinct but equivalent BDDs is also a tradeoff between efficiency and flexibility.
        /// </summary>
        private readonly ConcurrentDictionary<(int ordinal, BDD? one, BDD? zero), BDD> _bddCache = new();

        /// <summary>
        /// Generator for minterms.
        /// </summary>
        private readonly MintermGenerator<BDD> _mintermGen;

        /// <summary>
        /// Construct a solver for BDDs.
        /// </summary>
        public BDDAlgebra() => _mintermGen = new MintermGenerator<BDD>(this);

        /// <summary>
        /// Create a BDD with given ordinal and given one and zero child.
        /// Returns the BDD from the cache if it already exists.
        /// </summary>
        public BDD GetOrCreateBDD(int ordinal, BDD? one, BDD? zero) =>
            _bddCache.GetOrAdd((ordinal, one, zero), static key => new BDD(key.ordinal, key.one, key.zero));

        #region IBooleanAlgebra members

        /// <summary>
        /// Make the union of a and b
        /// </summary>
        public BDD Or(BDD a, BDD b) => ApplyBinaryOp(BoolOp.Or, a, b);

        /// <summary>
        /// Make the intersection of a and b
        /// </summary>
        public BDD And(BDD a, BDD b) => ApplyBinaryOp(BoolOp.And, a, b);

        /// <summary>
        /// Complement a
        /// </summary>
        public BDD Not(BDD a) =>
            a == False ? True :
            a == True ? False :
            _opCache.GetOrAdd((BoolOp.Not, a, null), static (key, algebra) => key.a.IsLeaf ?
                algebra.GetOrCreateBDD(algebra.CombineTerminals(BoolOp.Not, key.a.Ordinal, 0), null, null) : // multi-terminal case
                algebra.GetOrCreateBDD(key.a.Ordinal, algebra.Not(key.a.One), algebra.Not(key.a.Zero)),
                this);

        /// <summary>
        /// Applies the binary Boolean operation op and constructs the BDD recursively from a and b.
        /// </summary>
        /// <param name="op">given binary Boolean operation</param>
        /// <param name="a">first BDD</param>
        /// <param name="b">second BDD</param>
        /// <returns></returns>
        private BDD ApplyBinaryOp(BoolOp op, BDD a, BDD b)
        {
            // Handle base cases
            #region the cases when one of a or b is True or False or when a == b
            switch (op)
            {
                case BoolOp.Or:
                    if (a == False)
                        return b;
                    if (b == False)
                        return a;
                    if (a == True || b == True)
                        return True;
                    if (a == b)
                        return a;
                    break;

                case BoolOp.And:
                    if (a == True)
                        return b;
                    if (b == True)
                        return a;
                    if (a == False || b == False)
                        return False;
                    if (a == b)
                        return a;
                    break;

                case BoolOp.Xor:
                    if (a == False)
                        return b;
                    if (b == False)
                        return a;
                    if (a == b)
                        return False;
                    if (a == True)
                        return Not(b);
                    if (b == True)
                        return Not(a);
                    break;

                default:
                    Debug.Fail("Unhandled binary BoolOp case");
                    break;
            }
            #endregion

            // Order operands by hash code to increase cache hits
            if (a.GetHashCode() > b.GetHashCode())
            {
                BDD tmp = a;
                a = b;
                b = tmp;
            }

            return _opCache.GetOrAdd((op, a, b), static (key, algebra) =>
            {
                Debug.Assert(key.b is not null, "Validated it was non-null prior to calling GetOrAdd");

                if (key.a.IsLeaf && key.b.IsLeaf)
                {
                    // Multi-terminal case, we know here that a is neither True nor False
                    int ord = algebra.CombineTerminals(key.op, key.a.Ordinal, key.b.Ordinal);
                    return algebra.GetOrCreateBDD(ord, null, null);
                }

                if (key.a.IsLeaf || key.b!.Ordinal > key.a.Ordinal)
                {
                    Debug.Assert(!key.b.IsLeaf);
                    BDD t = algebra.ApplyBinaryOp(key.op, key.a, key.b.One);
                    BDD f = algebra.ApplyBinaryOp(key.op, key.a, key.b.Zero);
                    return t == f ? t : algebra.GetOrCreateBDD(key.b.Ordinal, t, f);
                }

                if (key.b.IsLeaf || key.a.Ordinal > key.b.Ordinal)
                {
                    Debug.Assert(!key.a.IsLeaf);
                    BDD t = algebra.ApplyBinaryOp(key.op, key.a.One, key.b);
                    BDD f = algebra.ApplyBinaryOp(key.op, key.a.Zero, key.b);
                    return t == f ? t : algebra.GetOrCreateBDD(key.a.Ordinal, t, f);
                }

                {
                    Debug.Assert(!key.a.IsLeaf);
                    Debug.Assert(!key.b.IsLeaf);
                    BDD t = algebra.ApplyBinaryOp(key.op, key.a.One, key.b.One);
                    BDD f = algebra.ApplyBinaryOp(key.op, key.a.Zero, key.b.Zero);
                    return t == f ? t : algebra.GetOrCreateBDD(key.a.Ordinal, t, f);
                }
            }, this);
        }

        /// <summary>
        /// Intersect all sets in the enumeration
        /// </summary>
        public BDD And(IEnumerable<BDD> sets)
        {
            BDD res = True;
            foreach (BDD bdd in sets)
            {
                res = And(res, bdd);
            }
            return res;
        }

        /// <summary>
        /// Take the union of all sets in the enumeration
        /// </summary>
        public BDD Or(IEnumerable<BDD> sets)
        {
            BDD res = False;
            foreach (BDD bdd in sets)
            {
                res = Or(res, bdd);
            }
            return res;
        }

        /// <summary>
        /// Gets the full set.
        /// </summary>
        public BDD True => BDD.True;

        /// <summary>
        /// Gets the empty set.
        /// </summary>
        public BDD False => BDD.False;

        /// <summary>
        /// Returns true if the set is nonempty.
        /// </summary>
        public bool IsSatisfiable(BDD set) => set != False;

        /// <summary>
        /// Returns true if a and b represent equivalent BDDs.
        /// </summary>
        public bool AreEquivalent(BDD a, BDD b) => Xor(a, b) == False;

        #endregion

        /// <summary>
        /// Make the XOR of a and b
        /// </summary>
        internal BDD Xor(BDD a, BDD b) => ApplyBinaryOp(BoolOp.Xor, a, b);

        #region bit-shift operations

        /// <summary>
        /// Shift all elements k bits to the right.
        /// For example if set denotes {*0000,*1110,*1111} then
        /// ShiftRight(set) denotes {*000,*111} where * denotes any prefix of 0's or 1's.
        /// </summary>
        public BDD ShiftRight(BDD set, int k)
        {
            Debug.Assert(k >= 0);
            return set.IsLeaf ? set : ShiftLeftImpl(new Dictionary<(BDD set, int k), BDD>(), set, 0 - k);
        }

        /// <summary>
        /// Shift all elements k bits to the left.
        /// For example if k=1 and set denotes {*0000,*1111} then
        /// ShiftLeft(set) denotes {*00000,*00001,*11110,*11111} where * denotes any prefix of 0's or 1's.
        /// </summary>
        public BDD ShiftLeft(BDD set, int k)
        {
            Debug.Assert(k >= 0);
            return set.IsLeaf ? set : ShiftLeftImpl(new Dictionary<(BDD set, int k), BDD>(), set, k);
        }

        /// <summary>
        /// Uses shiftCache to avoid recomputations in shared BDDs (which are DAGs).
        /// </summary>
        private BDD ShiftLeftImpl(Dictionary<(BDD set, int k), BDD> shiftCache, BDD set, int k)
        {
            if (set.IsLeaf || k == 0)
                return set;

            int ordinal = set.Ordinal + k;

            if (ordinal < 0)
                return True;  //this arises if k is negative

            if (!shiftCache.TryGetValue((set, k), out BDD? res))
            {
                BDD zero = ShiftLeftImpl(shiftCache, set.Zero, k);
                BDD one = ShiftLeftImpl(shiftCache, set.One, k);

                res = (zero == one) ?
                    zero :
                    GetOrCreateBDD((ushort)ordinal, one, zero);
                shiftCache[(set, k)] = res;
            }
            return res;
        }

        #endregion

        /// <summary>
        /// Generate all non-overlapping Boolean combinations of a set of BDDs.
        /// </summary>
        /// <param name="sets">the BDDs to create the minterms for</param>
        /// <returns>BDDs for the minterm</returns>
        public List<BDD> GenerateMinterms(IEnumerable<BDD> sets) => _mintermGen.GenerateMinterms(sets);

        /// <summary>
        /// Make a set containing all integers whose bits up to maxBit equal n.
        /// </summary>
        /// <param name="n">the given integer</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        /// <returns></returns>
        public BDD CreateSetFrom(uint n, int maxBit) => CreateSetFromRange(n, n, maxBit);

        /// <summary>
        /// Make the set containing all values greater than or equal to m and less than or equal to n when considering bits between 0 and maxBit.
        /// </summary>
        /// <param name="lower">lower bound</param>
        /// <param name="upper">upper bound</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        public BDD CreateSetFromRange(uint lower, uint upper, int maxBit)
        {
            Debug.Assert(0 <= maxBit && maxBit <= 31, "maxBit must be between 0 and 31");

            if (upper < lower)
                return False;

            // Filter out bits greater than maxBit
            if (maxBit < 31)
            {
                uint filter = (1u << (maxBit + 1)) - 1;
                lower &= filter;
                upper &= filter;
            }

            return CreateSetFromRangeImpl(lower, upper, maxBit);
        }

        private BDD CreateSetFromRangeImpl(uint lower, uint upper, int maxBit)
        {
            // Mask with 1 at position of maxBit
            uint mask = 1u << maxBit;

            if (mask == 1) // Base case for least significant bit
            {
                return
                    upper == 0 ? GetOrCreateBDD(maxBit, False, True) : // lower must also be 0
                    lower == 1 ? GetOrCreateBDD(maxBit, True, False) : // upper must also be 1
                    True; // Otherwise both 0 and 1 are included
            }

            // Check if range includes all numbers up to bit
            if (lower == 0 && upper == ((mask << 1) - 1))
            {
                return True;
            }

            // Mask out the highest bit for the first and last elements in the range
            uint lowerMasked = lower & mask;
            uint upperMasked = upper & mask;

            if (upperMasked == 0)
            {
                // Highest value in range doesn't have maxBit set, so the one branch is empty
                BDD zero = CreateSetFromRangeImpl(lower, upper, maxBit - 1);
                return GetOrCreateBDD(maxBit, False, zero);
            }
            else if (lowerMasked == mask)
            {
                // Lowest value in range has maxBit set, so the zero branch is empty
                BDD one = CreateSetFromRangeImpl(lower & ~mask, upper & ~mask, maxBit - 1);
                return GetOrCreateBDD(maxBit, one, False);
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

        /// <summary>
        /// Convert the set into an equivalent array of uint ranges.
        /// Bits above maxBit are ignored.
        /// The ranges are nonoverlapping and ordered.
        /// </summary>
        public static (uint, uint)[] ToRanges(BDD set, int maxBit) => BDDRangeConverter.ToRanges(set, maxBit);

        #region domain size and min computation

        /// <summary>
        /// Calculate the number of elements in the set. Returns 0 when set is full and maxBit is 63.
        /// </summary>
        /// <param name="set">the given set</param>
        /// <param name="maxBit">bits above maxBit are ignored</param>
        /// <returns>the cardinality of the set</returns>
        public virtual ulong ComputeDomainSize(BDD set, int maxBit)
        {
            if (maxBit < set.Ordinal)
                throw new ArgumentOutOfRangeException(nameof(maxBit));

            if (set == False)
                return 0UL;

            if (set == True)
                return 1UL << maxBit << 1; // e.g. if maxBit is 15 then the return value is 1 << 16, i.e., 2^16

            if (set.IsLeaf)
                throw new NotSupportedException(); // multi-terminal case is not supported

            ulong res = ComputeDomainSizeImpl(new Dictionary<BDD, ulong>(), set);
            if (maxBit > set.Ordinal)
            {
                res = (1UL << (maxBit - set.Ordinal)) * res;
            }

            return res;
        }

        /// <summary>
        /// Caches previously calculated values in sizeCache so that computations are not repeated inside a BDD for the same sub-BDD.
        /// Thus the number of internal calls is propotional to the number of nodes of the BDD, that could otherwise be exponential in the worst case.
        /// </summary>
        /// <param name="sizeCache">previously computed sizes</param>
        /// <param name="set">given set to compute size of</param>
        /// <returns></returns>
        private ulong ComputeDomainSizeImpl(Dictionary<BDD, ulong> sizeCache, BDD set)
        {
            if (!sizeCache.TryGetValue(set, out ulong size))
            {
                if (set.IsLeaf)
                    throw new NotSupportedException(); //multi-terminal case is not supported

                ulong sizeL;
                ulong sizeR;
                if (set.Zero.IsEmpty)
                {
                    sizeL = 0;
                    sizeR = set.One.IsFull ?
                        (uint)1 << set.Ordinal :
                        ((uint)1 << (set.Ordinal - 1 - set.One.Ordinal)) * ComputeDomainSizeImpl(sizeCache, set.One);
                }
                else if (set.Zero.IsFull)
                {
                    sizeL = 1UL << set.Ordinal;
                    sizeR = set.One.IsEmpty ?
                        0UL :
                        (1UL << (set.Ordinal - 1 - set.One.Ordinal)) * ComputeDomainSizeImpl(sizeCache, set.One);
                }
                else
                {
                    sizeL = (1UL << (set.Ordinal - 1 - set.Zero.Ordinal)) * ComputeDomainSizeImpl(sizeCache, set.Zero);
                    sizeR =
                        set.One == False ? 0UL :
                        set.One == True ? 1UL << set.Ordinal :
                        (1UL << (set.Ordinal - 1 - set.One.Ordinal)) * ComputeDomainSizeImpl(sizeCache, set.One);
                }

                size = sizeL + sizeR;
                sizeCache[set] = size;
            }
            return size;
        }

        /// <summary>
        /// Get the lexicographically minimum bitvector in the set as a ulong.
        /// Assumes that the set is nonempty and that the ordinal of the BDD is at most 63.
        /// </summary>
        /// <param name="set">the given nonempty set</param>
        /// <returns>the lexicographically smallest bitvector in the set</returns>
        public ulong GetMin(BDD set) => set.GetMin();

        #endregion

        /// <summary>
        /// Any two BDDs that are equivalent are isomorphic and have the same hashcode.
        /// </summary>
        public bool HashCodesRespectEquivalence => true;

        /// <summary>
        /// Two equivalent BDDs need not be identical
        /// </summary>
        public bool IsExtensional => false;

        /// <summary>
        /// The returned integer must be nonegative
        /// and will act as the combined terminal in a multi-terminal BDD.
        /// May throw NotSupportedException.
        /// </summary>
        public abstract int CombineTerminals(BoolOp op, int terminal1, int terminal2);

        /// <summary>
        /// Replace the True node in the BDD by a non-Boolean terminal.
        /// Locks the algebra for single threaded use.
        /// Observe that the Ordinal of False is -1 and the Ordinal of True is -2.
        /// </summary>
        public BDD ReplaceTrue(BDD bdd, int terminal)
        {
            Debug.Assert(terminal >= 0);

            BDD leaf = GetOrCreateBDD(terminal, null, null);
            return ReplaceTrueImpl(bdd, leaf, new Dictionary<BDD, BDD>());
        }

        private BDD ReplaceTrueImpl(BDD bdd, BDD leaf, Dictionary<BDD, BDD> cache)
        {
            if (bdd == True)
                return leaf;

            if (bdd.IsLeaf)
                return bdd;

            if (!cache.TryGetValue(bdd, out BDD? res))
            {
                BDD one = ReplaceTrueImpl(bdd.One, leaf, cache);
                BDD zero = ReplaceTrueImpl(bdd.Zero, leaf, cache);
                res = GetOrCreateBDD(bdd.Ordinal, one, zero);
                cache[bdd] = res;
            }
            return res;
        }
    }
}
