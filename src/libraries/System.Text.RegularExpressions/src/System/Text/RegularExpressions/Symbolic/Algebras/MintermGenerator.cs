// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Provides a generic implementation for minterm generation over a given Boolean Algebra.</summary>
    /// <typeparam name="TPredicate">type of predicates</typeparam>
    /// <remarks>
    /// The minterms for a set of predicates are all their non-overlapping, satisfiable Boolean combinations. For example,
    /// if the predicates are [0-9] and [0-4], then there are three minterms: [0-4], [5-9] and [^0-9]. Notably, there is no
    /// minterm corresponding to "[0-9] and not [0-4]", since that is unsatisfiable.
    /// </remarks>
    internal sealed class MintermGenerator<TPredicate> where TPredicate : notnull
    {
        private readonly IBooleanAlgebra<TPredicate> _algebra;

        /// <summary>Constructs a minterm generator for a given Boolean Algebra.</summary>
        /// <param name="algebra">given Boolean Algebra</param>
        public MintermGenerator(IBooleanAlgebra<TPredicate> algebra)
        {
            // check that we can rely on equivalent predicates having the same hashcode, which EquivClass assumes
            Debug.Assert(algebra.HashCodesRespectEquivalence);

            _algebra = algebra;
        }

        /// <summary>
        /// Given an array of predidates {p_1, p_2, ..., p_n} where n>=0,
        /// enumerate all satisfiable Boolean combinations Tuple({b_1, b_2, ..., b_n}, p)
        /// where p is satisfiable and equivalent to p'_1 &amp; p'_2 &amp; ... &amp; p'_n,
        /// where p'_i = p_i if b_i = true and p'_i is Not(p_i). Otherwise, if n=0
        /// return Tuple({},True).
        /// </summary>
        /// <param name="preds">array of predicates</param>
        /// <returns>all minterms of the given predicate sequence</returns>
        public List<TPredicate> GenerateMinterms(IEnumerable<TPredicate> preds)
        {
            var tree = new PartitionTree(_algebra.True);
            foreach (TPredicate pred in preds)
            {
                // Push each predicate into the partition tree
                tree.Refine(_algebra, pred);
            }
            // Return all minterms as the leaves of the partition tree
            return tree.GetLeafPredicates();
        }

        /// <summary>A partition tree for efficiently solving minterms.</summary>
        /// <remarks>
        /// Predicates are pushed into the tree with Refine(), which creates leaves in the tree for all satisfiable
        /// and non-overlapping combinations with any previously pushed predicates. At the end of the process the
        /// minterms can be read from the leaves of the tree.
        /// </remarks>
        private sealed class PartitionTree
        {
            internal readonly TPredicate _pred;
            private PartitionTree? _left;
            private PartitionTree? _right;

            internal PartitionTree(TPredicate pred)
            {
                _pred = pred;
            }

            internal void Refine(IBooleanAlgebra<TPredicate> solver, TPredicate other)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    StackHelper.CallOnEmptyStack(Refine, solver, other);
                    return;
                }

                TPredicate thisAndOther = solver.And(_pred, other);
                if (solver.IsSatisfiable(thisAndOther))
                {
                    // The predicates overlap, now check if this is contained in other
                    TPredicate thisMinusOther = solver.And(_pred, solver.Not(other));
                    if (solver.IsSatisfiable(thisMinusOther))
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

            /// <summary>Get the predicates from all of the leaves in the tree.</summary>
            internal List<TPredicate> GetLeafPredicates()
            {
                var leaves = new List<TPredicate>();

                var stack = new Stack<PartitionTree>();
                stack.Push(this);
                while (stack.TryPop(out PartitionTree? node))
                {
                    if (node._left is null && node._right is null)
                    {
                        leaves.Add(node._pred);
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
