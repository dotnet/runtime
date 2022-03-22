// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Provides an <see cref="ICharAlgebra{Int64}"/> over bit vectors up to 64 bits in length.</summary>
    internal sealed class UInt64Algebra : ICharAlgebra<ulong>
    {
        private readonly BDD[] _minterms;
        private readonly MintermGenerator<ulong> _mintermGenerator;
        internal readonly MintermClassifier _classifier;

        public UInt64Algebra(BDD[] minterms)
        {
            Debug.Assert(minterms.Length <= 64);

            _minterms = minterms;

            _mintermGenerator = new MintermGenerator<ulong>(this);
            _classifier = new MintermClassifier(minterms);

            True = minterms.Length == 64 ? ulong.MaxValue : ulong.MaxValue >> (64 - minterms.Length);
        }

        public ulong False => 0;

        public ulong True { get; }

        public bool AreEquivalent(ulong predicate1, ulong predicate2) => predicate1 == predicate2;

        public List<ulong> GenerateMinterms(HashSet<ulong> constraints) => _mintermGenerator.GenerateMinterms(constraints);

        public bool IsSatisfiable(ulong predicate) => predicate != 0;

        public ulong And(ulong predicate1, ulong predicate2) => predicate1 & predicate2;

        public ulong Not(ulong predicate) => True & ~predicate; //NOTE: must filter off unused bits

        public ulong Or(ReadOnlySpan<ulong> predicates)
        {
            ulong res = 0;
            foreach (ulong p in predicates)
            {
                res |= p;
                if (res == True)
                {
                    // Short circuit the evaluation once all bits are set, as nothing can change after this point.
                    break;
                }
            }

            return res;
        }

        public ulong Or(ulong predicate1, ulong predicate2) => predicate1 | predicate2;

        public ulong CharConstraint(char c, bool caseInsensitive = false, string? culture = null)
        {
            Debug.Assert(!caseInsensitive);
            return ((ulong)1) << _classifier.GetMintermID(c);
        }

        /// <summary>
        /// Assumes that set is a union of some minterms (or empty).
        /// If null then 0 is returned.
        /// </summary>
        public ulong ConvertFromCharSet(BDDAlgebra alg, BDD set)
        {
            BDD[] partition = _minterms;

            ulong res = 0;
            for (int i = 0; i < partition.Length; i++)
            {
                // Set the i'th bit if the i'th minterm is in the set.
                if (alg.IsSatisfiable(alg.And(partition[i], set)))
                {
                    res |= (ulong)1 << i;
                }
            }

            return res;
        }

        public BDD ConvertToCharSet(ulong pred)
        {
            BDD[] partition = _minterms;

            // the result will be the union of all minterms in the set
            BDD res = BDD.False;
            if (pred != 0)
            {
                for (int i = 0; i < partition.Length; i++)
                {
                    // include the i'th minterm in the union if the i'th bit is set
                    if ((pred & ((ulong)1 << i)) != 0)
                    {
                        res = CharSetSolver.Instance.Or(res, partition[i]);
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
            ulong[] minterms = new ulong[_minterms.Length];
            for (int i = 0; i < minterms.Length; i++)
            {
                minterms[i] = (ulong)1 << i;
            }

            return minterms;
        }

        /// <summary>Pretty print the bitvector bv as the character set it represents.</summary>
        public string PrettyPrint(ulong bv) => CharSetSolver.Instance.PrettyPrint(ConvertToCharSet(bv));
    }
}
