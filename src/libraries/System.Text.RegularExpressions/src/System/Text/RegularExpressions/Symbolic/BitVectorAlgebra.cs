// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Provides an <see cref="ICharAlgebra{BitVector}"/> over arbitrary-length bit vectors.</summary>
    internal sealed class BitVectorAlgebra : ICharAlgebra<BitVector>
    {
        private readonly BDD[] _minterms;
        private readonly MintermGenerator<BitVector> _mintermGenerator;
        internal readonly MintermClassifier _classifier;
        private readonly BitVector[] _mintermVectors;

        public BitVectorAlgebra(BDD[] minterms)
        {
            _minterms = minterms;

            _classifier = new MintermClassifier(minterms);
            _mintermGenerator = new MintermGenerator<BitVector>(this);

            var singleBitVectors = new BitVector[minterms.Length];
            for (int i = 0; i < singleBitVectors.Length; i++)
            {
                singleBitVectors[i] = BitVector.CreateSingleBit(minterms.Length, i);
            }
            _mintermVectors = singleBitVectors;

            False = BitVector.CreateFalse(minterms.Length);
            True = BitVector.CreateTrue(minterms.Length);
        }

        public BitVector False { get; }
        public BitVector True { get; }

        public bool AreEquivalent(BitVector predicate1, BitVector predicate2) => predicate1.Equals(predicate2);

        public List<BitVector> GenerateMinterms(HashSet<BitVector> constraints) => _mintermGenerator.GenerateMinterms(constraints);

        public bool IsSatisfiable(BitVector predicate) => !predicate.Equals(False);

        public BitVector And(BitVector predicate1, BitVector predicate2) => BitVector.And(predicate1, predicate2);

        public BitVector Not(BitVector predicate) => BitVector.Not(predicate);

        public BitVector Or(ReadOnlySpan<BitVector> predicates) => BitVector.Or(predicates);

        public BitVector Or(BitVector predicate1, BitVector predicate2) => BitVector.Or(predicate1, predicate2);

        public BitVector CharConstraint(char c, bool caseInsensitive = false, string? culture = null)
        {
            Debug.Assert(!caseInsensitive);
            int i = _classifier.GetMintermID(c);
            return _mintermVectors[i];
        }

        /// <summary>
        /// Assumes that set is a union of some minterms (or empty).
        /// If null then null is returned.
        /// </summary>
        public BitVector ConvertFromCharSet(BDDAlgebra alg, BDD set)
        {
            BDD[] partition = _minterms;

            BitVector res = False;
            for (int i = 0; i < partition.Length; i++)
            {
                if (alg.IsSatisfiable(alg.And(partition[i], set)))
                {
                    res = BitVector.Or(res, _mintermVectors[i]);
                }
            }

            return res;
        }

        public BDD ConvertToCharSet(BitVector pred)
        {
            BDD[] partition = _minterms;

            // the result will be the union of all minterms in the set
            BDD res = CharSetSolver.Instance.False;
            if (!pred.Equals(False))
            {
                for (int i = 0; i < partition.Length; i++)
                {
                    // include the i'th minterm in the union if the i'th bit is set
                    if (pred[i])
                    {
                        res = CharSetSolver.Instance.Or(res, partition[i]);
                    }
                }
            }

            return res;
        }

        public BitVector[] GetMinterms() => _mintermVectors;

        /// <summary>Pretty print the bitvector bv as the character set it represents.</summary>
        public string PrettyPrint(BitVector bv) => CharSetSolver.Instance.PrettyPrint(ConvertToCharSet(bv));
    }
}
