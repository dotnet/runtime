// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Bit vector algebra for up to 64 bits that uses an ulong directly as the term representation, unlike the more
    /// general BVAlgebra that uses an array of them. This simplifies the operations making the algebra more efficient.
    /// </summary>
    internal sealed class BitVector64Algebra : BitVectorAlgebraBase, ICharAlgebra<ulong>
    {
        private readonly MintermGenerator<ulong> _mintermGenerator;
        private readonly ulong _true;

        /// <summary>
        /// Return the number of characters belonging to the minterms in the given set.
        /// </summary>
        public ulong ComputeDomainSize(ulong set)
        {
            ulong size = 0;
            for (int i = 0; i < _bitCount; i++)
            {
                // if the bit is set then include the corresponding minterm's cardinality
                if (IsSatisfiable(set & ((ulong)1 << i)))
                {
                    size += _cardinalities[i];
                }
            }

            return size;
        }

        public BitVector64Algebra(CharSetSolver solver, BDD[] minterms) :
            base(new MintermClassifier(solver, minterms), solver.ComputeDomainSizes(minterms), minterms)
        {
            Debug.Assert(minterms.Length <= 64);
            _mintermGenerator = new MintermGenerator<ulong>(this);
            _true = _bitCount == 64 ? ulong.MaxValue : ulong.MaxValue >> (64 - _bitCount);
        }

        ulong IBooleanAlgebra<ulong>.False => 0;

        ulong IBooleanAlgebra<ulong>.True => _true;

        public bool AreEquivalent(ulong predicate1, ulong predicate2) => predicate1 == predicate2;

        public List<ulong> GenerateMinterms(HashSet<ulong> constraints) => _mintermGenerator.GenerateMinterms(constraints);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiable(ulong predicate) => predicate != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong And(ulong predicate1, ulong predicate2) => predicate1 & predicate2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Not(ulong predicate) => _true & ~predicate; //NOTE: must filter off unused bits

        public ulong Or(ReadOnlySpan<ulong> predicates)
        {
            ulong res = 0;
            foreach (ulong p in predicates)
            {
                res |= p;

                // short circuit the evaluation on true, since 1|x=1
                if (res == _true)
                {
                    return _true;
                }
            }

            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Or(ulong predicate1, ulong predicate2) => predicate1 | predicate2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong CharConstraint(char c, bool caseInsensitive = false, string? culture = null)
        {
            Debug.Assert(!caseInsensitive);
            return ((ulong)1) << _classifier.GetMintermID(c);
        }

        /// <summary>
        /// Assumes that set is a union of some minterms (or empty).
        /// If null then 0 is returned.
        /// </summary>
        public ulong ConvertFromCharSet(BDDAlgebra alg, BDD? set)
        {
            ulong res = 0;

            if (set is not null)
            {
                for (int i = 0; i < _bitCount; i++)
                {
                    Debug.Assert(_partition is not null);

                    // set the i'th bit if the i'th minterm is in the set
                    if (alg.IsSatisfiable(alg.And(_partition[i], set)))
                    {
                        res |= (ulong)1 << i;
                    }
                }
            }

            return res;
        }

        public BDD ConvertToCharSet(ICharAlgebra<BDD> solver, ulong pred)
        {
            Debug.Assert(_partition is not null);

            // the result will be the union of all minterms in the set
            BDD res = BDD.False;
            if (pred != 0)
            {
                for (int i = 0; i < _bitCount; i++)
                {
                    // include the i'th minterm in the union if the i'th bit is set
                    if ((pred & ((ulong)1 << i)) != 0)
                    {
                        res = solver.Or(res, _partition[i]);
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Return an array of bitvectors representing each of the minterms.
        /// </summary>
        public ulong[] GetMinterms()
        {
            ulong[] minterms = new ulong[_bitCount];
            for (int i = 0; i < _bitCount; i++)
            {
                minterms[i] = (ulong)1 << i;
            }

            return minterms;
        }

        /// <summary>Pretty print the bitvector bv as the character set it represents.</summary>
        public string PrettyPrint(ulong bv)
        {
            CharSetSolver solver = CharSetSolver.Instance;
            return solver.PrettyPrint(ConvertToCharSet(solver, bv));
        }
    }
}
