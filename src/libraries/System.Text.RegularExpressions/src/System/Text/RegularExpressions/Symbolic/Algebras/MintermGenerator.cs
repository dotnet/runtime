// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
        public List<TPredicate> GenerateMinterms(params TPredicate[] preds)
        {
            if (preds.Length == 0)
            {
                return new List<TPredicate> { _algebra.True };
            }

            // The minterms will be solved using non-equivalent predicates, i.e., the equivalence classes of preds. The
            // following code maps each predicate to an equivalence class and also stores for each equivalence class the
            // predicates belonging to it, so that a valuation for the original predicates may be reconstructed.

            var tree = new PartitionTree(_algebra);

            var seen = new HashSet<EquivalenceClass>();
            for (int i = 0; i < preds.Length; i++)
            {
                // Use a wrapper that overloads Equals to be logical equivalence as the key
                if (seen.Add(new EquivalenceClass(_algebra, preds[i])))
                {
                    // Push each equivalence class into the partition tree
                    tree.Refine(preds[i]);
                }
            }

            // Return all minterms as the leaves of the partition tree
            return tree.GetLeafPredicates();
        }

        /// <summary>Wraps a predicate as an equivalence class object whose Equals method is equivalence checking.</summary>
        private readonly struct EquivalenceClass
        {
            private readonly TPredicate _set;
            private readonly IBooleanAlgebra<TPredicate> _algebra;

            internal EquivalenceClass(IBooleanAlgebra<TPredicate> algebra, TPredicate set)
            {
                _set = set;
                _algebra = algebra;
            }

            public override int GetHashCode() => _set.GetHashCode();

            public override bool Equals([NotNullWhen(true)] object? obj) => obj is EquivalenceClass ec && _algebra.AreEquivalent(_set, ec._set);
        }

        /// <summary>A partition tree for efficiently solving minterms.</summary>
        /// <remarks>
        /// Predicates are pushed into the tree with Refine(), which creates leaves in the tree for all satisfiable
        /// and non-overlapping combinations with any previously pushed predicates. At the end of the process the
        /// minterms can be read from the paths to the leaves of the tree.
        ///
        /// The valuations of the predicates are represented as follows. Given a path a^-1, a^0, a^1, ..., a^n, predicate
        /// p^i is true in the corresponding minterm if and only if a^i is the left child of a^i-1.
        ///
        /// This class assumes that all predicates passed to Refine() are non-equivalent.
        /// </remarks>
        private sealed class PartitionTree
        {
            internal readonly TPredicate _pred;
            private readonly IBooleanAlgebra<TPredicate> _solver;
            private PartitionTree? _left;
            private PartitionTree? _right; // complement

            /// <summary>Create the root of the partition tree.</summary>
            /// <remarks>Nodes below this will be indexed starting from 0. The initial predicate is true.</remarks>
            internal PartitionTree(IBooleanAlgebra<TPredicate> solver) : this(solver, solver.True, null, null) { }

            private PartitionTree(IBooleanAlgebra<TPredicate> solver, TPredicate pred, PartitionTree? left, PartitionTree? right)
            {
                _solver = solver;
                _pred = pred;
                _left = left;
                _right = right;
            }

            internal void Refine(TPredicate other)
            {
                if (_left is null && _right is null)
                {
                    // If this is a leaf node create left and/or right children for the new predicate
                    TPredicate thisAndOther = _solver.And(_pred, other);
                    if (_solver.IsSatisfiable(thisAndOther))
                    {
                        // The predicates overlap, now check if this is contained in other
                        TPredicate thisMinusOther = _solver.And(_pred, _solver.Not(other));
                        if (_solver.IsSatisfiable(thisMinusOther))
                        {
                            // This is not contained in other, both children are needed
                            _left = new PartitionTree(_solver, thisAndOther, null, null);

                            // The right child corresponds to a conjunction with a negation, which matches thisMinusOther
                            _right = new PartitionTree(_solver, thisMinusOther, null, null);
                        }
                        else // [[this]] subset of [[other]]
                        {
                            // Other implies this, so populate the left child with this
                            _left = new PartitionTree(_solver, _pred, null, null);
                        }
                    }
                    else // [[this]] subset of [[not(other)]]
                    {
                        // negation of other implies this, so populate the right child with this
                        _right = new PartitionTree(_solver, _pred, null, null); //other must be false
                    }
                }
                else if (_left is null)
                {
                    // No choice has to be made here, refine the single child that exists
                    _right!.Refine(other);
                }
                else if (_right is null)
                {
                    // No choice has to be made here, refine the single child that exists
                    _left!.Refine(other);
                }
                else
                {
                    TPredicate thisAndOther = _solver.And(_pred, other);
                    if (_solver.IsSatisfiable(thisAndOther))
                    {
                        // Other is satisfiable in this subtree
                        TPredicate thisMinusOther = _solver.And(_pred, _solver.Not(other));
                        if (_solver.IsSatisfiable(thisMinusOther))
                        {
                            // But other does not imply this whole subtree, refine both children
                            _left.Refine(other);
                            _right.Refine(other);
                        }
                        else // [[this]] subset of [[other]]
                        {
                            // And other implies the whole subtree, include it in all minterms under here
                            _left.ExtendLeft();
                            _right.ExtendLeft();
                        }
                    }
                    else // [[this]] subset of [[not(other)]]
                    {
                        // Other is not satisfiable in this subtree, include its negation in all minterms under here
                        _left.ExtendRight();
                        _right.ExtendRight();
                    }
                }
            }

            /// <summary>
            /// Include the next predicate in all minterms under this node. Assumes the next predicate implies the predicate
            /// of this node.
            /// </summary>
            private void ExtendLeft()
            {
                if (_left is null && _right is null)
                {
                    _left = new PartitionTree(_solver, _pred, null, null);
                }
                else
                {
                    _left?.ExtendLeft();
                    _right?.ExtendLeft();
                }
            }

            /// <summary>
            /// Include the negation of next predicate in all minterms under this node. Assumes the negation of the next
            /// predicate implies the predicate of this node.
            /// </summary>
            private void ExtendRight()
            {
                if (_left is null && _right is null)
                {
                    _right = new PartitionTree(_solver, _pred, null, null);
                }
                else
                {
                    _left?.ExtendRight();
                    _right?.ExtendRight();
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
                        if (node._left is not null)
                        {
                            stack.Push(node._left);
                        }

                        if (node._right is not null)
                        {
                            stack.Push(node._right);
                        }
                    }
                }

                return leaves;
            }
        }
    }
}
