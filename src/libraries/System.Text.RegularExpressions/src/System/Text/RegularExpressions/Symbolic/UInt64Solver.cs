// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Provides an <see cref="ISolver{Int64}"/> over bit vectors up to 64 bits in length.</summary>
    internal sealed class UInt64Solver : ISolver<ulong>
    {
        private readonly BDD[] _minterms;
        private readonly MintermGenerator<ulong> _mintermGenerator;
        internal readonly MintermClassifier _classifier;

        public UInt64Solver(BDD[] minterms, CharSetSolver solver)
        {
            Debug.Assert(minterms.Length <= 64);

            _minterms = minterms;

            _mintermGenerator = new MintermGenerator<ulong>(this);
            _classifier = new MintermClassifier(minterms, solver);

            Full = minterms.Length == 64 ? ulong.MaxValue : ulong.MaxValue >> (64 - minterms.Length);
        }

        public ulong Empty => 0;

        public ulong Full { get; }

        public bool IsFull(ulong set) => set == Full;

        public bool IsEmpty(ulong set) => set == 0;

        public List<ulong> GenerateMinterms(HashSet<ulong> constraints) => _mintermGenerator.GenerateMinterms(constraints);

        public ulong And(ulong set1, ulong set2) => set1 & set2;

        public ulong Not(ulong set) => Full & ~set; //NOTE: must filter off unused bits

        public ulong Or(ReadOnlySpan<ulong> sets)
        {
            ulong result = 0;
            foreach (ulong p in sets)
            {
                result |= p;
                if (result == Full)
                {
                    // Short circuit the evaluation once all bits are set, as nothing can change after this point.
                    break;
                }
            }

            return result;
        }

        public ulong Or(ulong set1, ulong set2) => set1 | set2;

        public ulong CreateFromChar(char c) => ((ulong)1) << _classifier.GetMintermID(c);

        /// <summary>
        /// Assumes that set is a union of some minterms (or empty).
        /// If null then 0 is returned.
        /// </summary>
        public ulong ConvertFromBDD(BDD set, CharSetSolver solver)
        {
            BDD[] partition = _minterms;

            ulong result = 0;
            for (int i = 0; i < partition.Length; i++)
            {
                // Set the i'th bit if the i'th minterm is in the set.
                if (!solver.IsEmpty(solver.And(partition[i], set)))
                {
                    result |= (ulong)1 << i;
                }
            }

            return result;
        }

        public BDD ConvertToBDD(ulong set, CharSetSolver solver)
        {
            BDD[] partition = _minterms;

            // the result will be the union of all minterms in the set
            BDD result = BDD.False;
            if (set != 0)
            {
                for (int i = 0; i < partition.Length; i++)
                {
                    // include the i'th minterm in the union if the i'th bit is set
                    if ((set & ((ulong)1 << i)) != 0)
                    {
                        result = solver.Or(result, partition[i]);
                    }
                }
            }

            return result;
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
        public string PrettyPrint(ulong bv, CharSetSolver solver) => solver.PrettyPrint(ConvertToBDD(bv, solver));
    }
}
