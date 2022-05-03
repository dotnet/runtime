// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Provides a generic implementation for minterm generation over a given Boolean Algebra.</summary>
    /// <typeparam name="TSet">The type of set of elements (typically <see cref="BDD"/>, <see cref="BitVector"/> for more than 64 elements, or <see cref="ulong"/> for 64 or fewer elements).</typeparam>
    /// <remarks>
    /// The minterms for a collection of sets are all their non-overlapping, satisfiable Boolean combinations. For example,
    /// if the sets are [0-9] and [0-4], then there are three minterms: [0-4], [5-9] and [^0-9]. Notably, there is no
    /// minterm corresponding to "[0-9] and not [0-4]", since that is unsatisfiable (empty).
    /// </remarks>
    internal sealed class MintermGenerator<TSet> where TSet : IComparable<TSet>
    {
        private readonly ISolver<TSet> _solver;

        /// <summary>Constructs a minterm generator for a given solver.</summary>
        /// <param name="solver">The solver for operating over sets.</param>
        public MintermGenerator(ISolver<TSet> solver) => _solver = solver;

        /// <summary>
        /// Given an array of sets {p_1, p_2, ..., p_n} where n>=0,
        /// enumerate all satisfiable (non-empty) Boolean combinations Tuple({b_1, b_2, ..., b_n}, p)
        /// where p is satisfiable and equivalent to p'_1 &amp; p'_2 &amp; ... &amp; p'_n,
        /// where p'_i = p_i if b_i = true and p'_i is Not(p_i). Otherwise, if n=0 return Tuple({},True).
        /// </summary>
        /// <param name="sets">The sets from which to generate the minterms.</param>
        /// <returns>All minterms of the given set sequence</returns>
        public List<TSet> GenerateMinterms(HashSet<TSet> sets)
        {
            var tree = new PartitionTree(_solver.Full);
            foreach (TSet set in sets)
            {
                // Push each set into the partition tree
                tree.Refine(_solver, set);
            }

            // Return all minterms as the leaves of the partition tree
            return tree.GetLeafSets();
        }

        /// <summary>A partition tree for efficiently solving minterms.</summary>
        /// <remarks>
        /// Sets are pushed into the tree with Refine(), which creates leaves in the tree for all satisfiable (non-empty)
        /// and non-overlapping combinations with any previously pushed sets. At the end of the process the
        /// minterms can be read from the leaves of the tree.
        /// </remarks>
        private sealed class PartitionTree
        {
            internal readonly TSet _set;
            private PartitionTree? _left;
            private PartitionTree? _right;

            internal PartitionTree(TSet set) => _set = set;

            internal void Refine(ISolver<TSet> solver, TSet other)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    StackHelper.CallOnEmptyStack(Refine, solver, other);
                    return;
                }

                TSet thisAndOther = solver.And(_set, other);
                if (!solver.IsEmpty(thisAndOther))
                {
                    // The sets overlap, now check if this is contained in other
                    TSet thisMinusOther = solver.And(_set, solver.Not(other));
                    if (!solver.IsEmpty(thisMinusOther))
                    {
                        // This is not contained in other, minterms may need to be split
                        if (_left is null)
                        {
                            Debug.Assert(_right is null);
                            _left = new PartitionTree(thisAndOther);
                            _right = new PartitionTree(thisMinusOther);
                        }
                        else
                        {
                            Debug.Assert(_right is not null);
                            _left.Refine(solver, other);
                            _right.Refine(solver, other);
                        }
                    }
                }
            }

            /// <summary>Get the sets from all of the leaves in the tree.</summary>
            internal List<TSet> GetLeafSets()
            {
                var leaves = new List<TSet>();

                var stack = new Stack<PartitionTree>();
                stack.Push(this);
                while (stack.TryPop(out PartitionTree? node))
                {
                    if (node._left is null && node._right is null)
                    {
                        leaves.Add(node._set);
                    }
                    else
                    {
                        Debug.Assert(node._left is not null && node._right is not null);
                        stack.Push(node._left);
                        stack.Push(node._right);
                    }
                }

                return leaves;
            }
        }
    }
}
