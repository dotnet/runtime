// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Base class for bitvector algebras, which represent sets as bitvectors indexed by the elements. An element is in
    /// the set if the corresponding bit is set.
    ///
    /// These bitvector algebras are used to represent sets of minterms, and thus represent sets of characters
    /// indirectly. However, the bitvector algebras are aware of this indirection in that the cardinalities of sets
    /// count the characters rather than the minterms. For example, the cardinality of a bitvector "110" where the bits
    /// correspond to minterms [a-c], [0-9] and [^a-c0-9] is 13 rather than 2.
    /// </summary>
    internal abstract class BitVectorAlgebraBase
    {
        internal readonly MintermClassifier _classifier;
        protected readonly ulong[] _cardinalities;
        protected readonly int _bitCount;
        protected readonly BDD[]? _partition;

        internal BitVectorAlgebraBase(MintermClassifier classifier, ulong[] cardinalities, BDD[]? partition)
        {
            _classifier = classifier;
            _cardinalities = cardinalities;
            _bitCount = cardinalities.Length;
            _partition = partition;
        }
    }

    /// <summary>
    /// Bit vector algebra
    /// </summary>
    internal sealed class BitVectorAlgebra : BitVectorAlgebraBase, ICharAlgebra<BitVector>
    {
        private readonly MintermGenerator<BitVector> _mintermGenerator;
        internal BitVector[] _minterms;

        public ulong ComputeDomainSize(BitVector set)
        {
            ulong size = 0;
            for (int i = 0; i < _bitCount; i++)
            {
                // if the bit is set then add the minterm's size
                if (set[i])
                {
                    size += _cardinalities[i];
                }
            }

            return size;
        }

        public BitVectorAlgebra(CharSetSolver solver, BDD[] minterms) :
            base(new MintermClassifier(solver, minterms), solver.ComputeDomainSizes(minterms), minterms)
        {
            _mintermGenerator = new MintermGenerator<BitVector>(this);
            False = BitVector.CreateFalse(_bitCount);
            True = BitVector.CreateTrue(_bitCount);

            var singleBitVectors = new BitVector[_bitCount];
            for (int i = 0; i < singleBitVectors.Length; i++)
            {
                singleBitVectors[i] = BitVector.CreateSingleBit(_bitCount, i);
            }
            _minterms = singleBitVectors;
        }

        public BitVector False { get; }
        public BitVector True { get; }

        public bool AreEquivalent(BitVector predicate1, BitVector predicate2) => predicate1.Equals(predicate2);

        public List<BitVector> GenerateMinterms(IEnumerable<BitVector> constraints) => _mintermGenerator.GenerateMinterms(constraints);

        public bool IsSatisfiable(BitVector predicate) => !predicate.Equals(False);

        public BitVector And(BitVector predicate1, BitVector predicate2) => BitVector.And(predicate1, predicate2);

        public BitVector Not(BitVector predicate) => BitVector.Not(predicate);

        public BitVector Or(ReadOnlySpan<BitVector> predicates) => BitVector.Or(predicates);

        public BitVector Or(BitVector predicate1, BitVector predicate2) => BitVector.Or(predicate1, predicate2);

        public BitVector CharConstraint(char c, bool caseInsensitive = false, string? culture = null)
        {
            Debug.Assert(!caseInsensitive);
            int i = _classifier.GetMintermID(c);
            return _minterms[i];
        }

        /// <summary>
        /// Assumes that set is a union of some minterms (or empty).
        /// If null then null is returned.
        /// </summary>
        public BitVector ConvertFromCharSet(BDDAlgebra alg, BDD set)
        {
            Debug.Assert(_partition is not null);

            BitVector res = False;
            for (int i = 0; i < _bitCount; i++)
            {
                BDD bdd_i = _partition[i];
                BDD conj = alg.And(bdd_i, set);
                if (alg.IsSatisfiable(conj))
                {
                    res = BitVector.Or(res, _minterms[i]);
                }
            }

            return res;
        }

        public BDD ConvertToCharSet(ICharAlgebra<BDD> solver, BitVector pred)
        {
            Debug.Assert(_partition is not null);

            // the result will be the union of all minterms in the set
            BDD res = solver.False;
            if (!pred.Equals(False))
            {
                for (int i = 0; i < _bitCount; i++)
                {
                    // include the i'th minterm in the union if the i'th bit is set
                    if (pred[i])
                    {
                        res = solver.Or(res, _partition[i]);
                    }
                }
            }

            return res;
        }

        public BitVector[] GetMinterms() => _minterms;

        /// <summary>Pretty print the bitvector bv as the character set it represents.</summary>
        public string PrettyPrint(BitVector bv)
        {
            CharSetSolver solver = CharSetSolver.Instance;
            return solver.PrettyPrint(ConvertToCharSet(solver, bv));
        }
    }
}
