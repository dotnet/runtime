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
    internal sealed class BV64Algebra : BVAlgebraBase, ICharAlgebra<ulong>
    {
        private readonly MintermGenerator<ulong> _mintermGenerator;
        private readonly ulong _false;
        private readonly ulong _true;

        /// <summary>
        /// Return the number of characters belonging to the minterms in the given set.
        /// </summary>
        public ulong ComputeDomainSize(ulong set)
        {
            ulong size = 0;
            for (int i = 0; i < _bits; i++)
            {
                // if the bit is set then include the corresponding minterm's cardinality
                if (IsSatisfiable(set & ((ulong)1 << i)))
                {
                    size += _cardinalities[i];
                }
            }

            return size;
        }

        public BV64Algebra(CharSetSolver solver, BDD[] minterms) :
            base(new MintermClassifier(solver, minterms), solver.ComputeDomainSizes(minterms), minterms)
        {
            Debug.Assert(minterms.Length <= 64);
            _mintermGenerator = new MintermGenerator<ulong>(this);
            _false = 0;
            _true = _bits == 64 ? ulong.MaxValue : ulong.MaxValue >> (64 - _bits);
        }

        public bool IsExtensional => true;
        public bool HashCodesRespectEquivalence => true;

        public CharSetSolver CharSetProvider => throw new NotSupportedException();

        ulong IBooleanAlgebra<ulong>.False => _false;

        ulong IBooleanAlgebra<ulong>.True => _true;

        public bool AreEquivalent(ulong predicate1, ulong predicate2) => predicate1 == predicate2;

        public List<ulong> GenerateMinterms(IEnumerable<ulong> constraints) => _mintermGenerator.GenerateMinterms(constraints);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiable(ulong predicate) => predicate != _false;

        public ulong And(IEnumerable<ulong> predicates) => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong And(ulong predicate1, ulong predicate2) => predicate1 & predicate2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Not(ulong predicate) => _true & ~predicate; //NOTE: must filter off unused bits

        public ulong Or(IEnumerable<ulong> predicates)
        {
            ulong res = _false;
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

        public ulong RangeConstraint(char lower, char upper, bool caseInsensitive = false, string? culture = null) => throw new NotSupportedException();

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
            ulong res = _false;

            if (set is not null)
            {
                for (int i = 0; i < _bits; i++)
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
            if (pred != _false)
            {
                for (int i = 0; i < _bits; i++)
                {
                    // include the i'th minterm in the union if the i'th bit is set
                    if ((pred & ((ulong)1 << i)) != _false)
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
            ulong[] minterms = new ulong[_bits];
            for (int i = 0; i < _bits; i++)
            {
                minterms[i] = (ulong)1 << i;
            }

            return minterms;
        }

        public IEnumerable<char> GenerateAllCharacters(ulong set) => throw new NotSupportedException();

        /// <summary>Pretty print the bitvector bv as the character set it represents.</summary>
        public string PrettyPrint(ulong bv)
        {
            ICharAlgebra<BDD> bddalgebra = SymbolicRegexRunnerFactory.s_unicode._solver;
            Debug.Assert(_partition is not null && bddalgebra is not null);

            return bddalgebra.PrettyPrint(ConvertToCharSet(bddalgebra, bv));
        }
    }
}
