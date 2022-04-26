// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents a symbolic derivative created from a symbolic regex without using minterms</summary>
    internal sealed class TransitionRegex<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        public readonly SymbolicRegexBuilder<TSet> _builder;
        public readonly TransitionRegexKind _kind;
        public readonly TSet? _test;
        public readonly TransitionRegex<TSet>? _first;
        public readonly TransitionRegex<TSet>? _second;
        public readonly SymbolicRegexNode<TSet>? _node;
        public readonly DerivativeEffect? _effect;

        private TransitionRegex(SymbolicRegexBuilder<TSet> builder, TransitionRegexKind kind, TSet? test, TransitionRegex<TSet>? first, TransitionRegex<TSet>? second, SymbolicRegexNode<TSet>? node, DerivativeEffect? effect)
        {
            Debug.Assert(builder is not null);
            Debug.Assert(
                (kind is TransitionRegexKind.Leaf && node is not null && Equals(test, default(TSet)) && first is null && second is null && effect is null) ||
                (kind is TransitionRegexKind.Conditional && test is not null && first is not null && second is not null && node is null && effect is null) ||
                (kind is TransitionRegexKind.Union && Equals(test, default(TSet)) && first is not null && second is not null && node is null && effect is null) ||
                (kind is TransitionRegexKind.Lookaround && Equals(test, default(TSet)) && first is not null && second is not null && node is not null && effect is null) ||
                (kind is TransitionRegexKind.Effect && Equals(test, default(TSet)) && first is not null && second is null && node is null && effect is not null));

            _builder = builder;
            _kind = kind;
            _test = test;
            _first = first;
            _second = second;
            _node = node;
            _effect = effect;
        }

        private static TransitionRegex<TSet> GetOrCreate(SymbolicRegexBuilder<TSet> builder, TransitionRegexKind kind, TSet? test, TransitionRegex<TSet>? one, TransitionRegex<TSet>? two, SymbolicRegexNode<TSet>? node, DerivativeEffect? effect = null)
        {
            // Keep transition regexes internalized using the builder
            ref TransitionRegex<TSet>? tr = ref CollectionsMarshal.GetValueRefOrAddDefault(builder._trCache, (kind, test, one, two, node, effect), out _);
            return tr ??= new TransitionRegex<TSet>(builder, kind, test, one, two, node, effect);
        }

        public bool IsNothing
        {
            get
            {
                if (_kind == TransitionRegexKind.Leaf)
                {
                    Debug.Assert(_node != null);
                    return _node.IsNothing;
                }

                return false;
            }
        }

        public bool IsAnyStar
        {
            get
            {
                if (_kind == TransitionRegexKind.Leaf)
                {
                    Debug.Assert(_node != null);
                    return _node.IsAnyStar;
                }

                return false;
            }
        }

        /// <summary>Complement of transition regex</summary>
        public TransitionRegex<TSet> Complement()
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Complement);
            }

            switch (_kind)
            {
                case TransitionRegexKind.Leaf:
                    // Complement is propagated to the leaf
                    Debug.Assert(_node is not null);
                    return GetOrCreate(_builder, _kind, default(TSet), null, null, _node._builder.Not(_node));

                case TransitionRegexKind.Union:
                    // Apply deMorgan's laws
                    Debug.Assert(_first is not null && _second is not null);
                    return Intersect(_first.Complement(), _second.Complement());

                default:
                    // Both Conditional and Nullability obey the same laws of propagation of complement
                    Debug.Assert(_first is not null && _second is not null);
                    return GetOrCreate(_builder, _kind, _test, _first.Complement(), _second.Complement(), _node);
            }
        }

        public static TransitionRegex<TSet> Leaf(SymbolicRegexNode<TSet> node) =>
            GetOrCreate(node._builder, TransitionRegexKind.Leaf, default(TSet), null, null, node);

        /// <summary>Concatenate a node at the end of this transition regex</summary>
        public TransitionRegex<TSet> Concat(SymbolicRegexNode<TSet> node)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Concat, node);
            }

            switch (_kind)
            {
                case TransitionRegexKind.Leaf:
                    Debug.Assert(_node is not null);
                    return GetOrCreate(_builder, _kind, default(TSet), null, null, _node._builder.CreateConcat(_node, node));

                case TransitionRegexKind.Effect:
                    Debug.Assert(_first is not null);
                    return GetOrCreate(_builder, _kind, default(TSet), _first.Concat(node), null, null, _effect);

                default:
                    // All other three cases are disjunctive and obey the same laws of propagation of complement
                    Debug.Assert(_first is not null && _second is not null);
                    return GetOrCreate(_builder, _kind, _test, _first.Concat(node), _second.Concat(node), _node);
            }
        }

        /// <summary>Intersection of transition regexes</summary>
        public static TransitionRegex<TSet> Intersect(TransitionRegex<TSet> one, TransitionRegex<TSet> two)
        {
            // Apply standard simplifications

            // [] & t = [], t & .* = t
            if (one.IsNothing || two.IsAnyStar || one == two)
            {
                return one;
            }

            // t & [] = [], .* & t = t
            if (two.IsNothing || one.IsAnyStar)
            {
                return two;
            }

            return one.IntersectWith(two, one._builder._solver.Full);
        }

        private TransitionRegex<TSet> IntersectWith(TransitionRegex<TSet> that, TSet pathIn)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(IntersectWith, that, pathIn);
            }

            Debug.Assert(!_builder._solver.IsEmpty(pathIn));

#region Conditional
            // Intersect when this is a Conditional
            if (_kind == TransitionRegexKind.Conditional)
            {
                Debug.Assert(_test is not null && _first is not null && _second is not null);
                TSet thenPath = _builder._solver.And(pathIn, _test);
                TSet elsePath = _builder._solver.And(pathIn, _builder._solver.Not(_test));

                if (_builder._solver.IsEmpty(thenPath))
                {
                    // then case being infeasible implies that elsePath must be satisfiable
                    return _second.IntersectWith(that, elsePath);
                }

                if (_builder._solver.IsEmpty(elsePath))
                {
                    // else case is infeasible
                    return _first.IntersectWith(that, thenPath);
                }

                TransitionRegex<TSet> thencase = _first.IntersectWith(that, thenPath);
                TransitionRegex<TSet> elsecase = _second.IntersectWith(that, elsePath);
                if (thencase == elsecase)
                {
                    // Both branches result in the same thing, so the test can be omitted
                    return thencase;
                }

                return GetOrCreate(_builder, TransitionRegexKind.Conditional, _test, thencase, elsecase, null);
            }

            // Swap the order of this and that if that is a Conditional
            if (that._kind == TransitionRegexKind.Conditional)
            {
                return that.IntersectWith(this, pathIn);
            }
#endregion

#region Union
            // Intersect when this is a Union
            // Use the following law of distributivity: (A|B)&C = A&C|B&C
            if (_kind == TransitionRegexKind.Union)
            {
                Debug.Assert(_first is not null && _second is not null);
                return Union(_first.IntersectWith(that, pathIn), _second.IntersectWith(that, pathIn));
            }

            // Swap the order of this and that if that is a Union
            if (that._kind == TransitionRegexKind.Union)
            {
                return that.IntersectWith(this, pathIn);
            }
#endregion

#region Nullability
            if (_kind == TransitionRegexKind.Lookaround)
            {
                Debug.Assert(_node is not null && _first is not null && _second is not null);
                return Lookaround(_node, _first.IntersectWith(that, pathIn), _second.IntersectWith(that, pathIn));
            }

            if (that._kind == TransitionRegexKind.Lookaround)
            {
                Debug.Assert(that._node is not null && that._first is not null && that._second is not null);
                return Lookaround(that._node, that._first.IntersectWith(this, pathIn), that._second.IntersectWith(this, pathIn));
            }
#endregion

            // Propagate intersection to the leaves
            Debug.Assert(_kind is TransitionRegexKind.Leaf && that._kind is TransitionRegexKind.Leaf && _node is not null && that._node is not null);
            return Leaf(_builder.And(_node, that._node));
        }

        /// <summary>Union of transition regexes</summary>
        public static TransitionRegex<TSet> Union(TransitionRegex<TSet> one, TransitionRegex<TSet> two)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Union, one, two);
            }

            // Apply common simplifications, always trying to push the operations into the leaves or to eliminate redundant branches
            if (one.IsNothing || two.IsAnyStar || one == two)
            {
                return two;
            }

            if (two.IsNothing || one.IsAnyStar)
            {
                return one;
            }

            if (one._kind == TransitionRegexKind.Conditional && two._kind == TransitionRegexKind.Conditional)
            {
                Debug.Assert(one._test is not null && one._first is not null && one._second is not null);
                Debug.Assert(two._test is not null && two._first is not null && two._second is not null);

                // if (psi, t1, t2) | if(psi, s1, s2) = if(psi, t1|s1, t2|s2)
                if (one._test.Equals(two._test))
                {
                    return Conditional(one._test, Union(one._first, two._first), Union(one._second, two._second));
                }

                // if (psi, t, []) | if(phi, t, []) = if(psi or phi, t, [])
                if (one._second.IsNothing && two._second.IsNothing && one._first.Equals(two._first))
                {
                    return Conditional(one._builder._solver.Or(one._test, two._test), one._first, one._second);
                }
            }

            return GetOrCreate(one._builder, TransitionRegexKind.Union, default(TSet), one, two, null);
        }

        public static TransitionRegex<TSet> Conditional(TSet test, TransitionRegex<TSet> thencase, TransitionRegex<TSet> elsecase) =>
            (thencase == elsecase || thencase._builder._solver.Full.Equals(test)) ? thencase :
            thencase._builder._solver.Empty.Equals(test) ? elsecase :
            GetOrCreate(thencase._builder, TransitionRegexKind.Conditional, test, thencase, elsecase, null);

        public static TransitionRegex<TSet> Lookaround(SymbolicRegexNode<TSet> nullabilityTest, TransitionRegex<TSet> thencase, TransitionRegex<TSet> elsecase) =>
            (thencase == elsecase) ? thencase : GetOrCreate(thencase._builder, TransitionRegexKind.Lookaround, default(TSet), thencase, elsecase, nullabilityTest);

        public static TransitionRegex<TSet> Effect(TransitionRegex<TSet> child, DerivativeEffect effect) =>
            child.IsNothing ? child :
            GetOrCreate(child._builder, TransitionRegexKind.Effect, default(TSet), child, null, null, effect);

        public override string ToString() =>
            _kind switch
            {
                TransitionRegexKind.Leaf => $"{_node}",
                TransitionRegexKind.Union => $"{_first} | {_second}",
                TransitionRegexKind.Conditional => $"if({_test}, {_first}, {_second})",
                TransitionRegexKind.Effect => _effect?.Kind switch
                {
                    DerivativeEffectKind.CaptureStart => $"captureStart({_effect?.CaptureNumber}, {_first})",
                    _ => $"captureEnd({_effect?.CaptureNumber}, {_first})",
                },
                _ => $"if (IsNull({_node}), {_first}, {_second})",
            };

        /// <summary>Enumerates all the paths in this transition regex excluding dead-end paths</summary>
        public IEnumerable<(TSet, SymbolicRegexNode<TSet>?, SymbolicRegexNode<TSet>)> EnumeratePaths(TSet pathCondition)
        {
            switch (_kind)
            {
                case TransitionRegexKind.Leaf:
                    Debug.Assert(_node is not null);
                    // Omit any path that leads to a deadend
                    if (!_node.IsNothing)
                    {
                        yield return (pathCondition, null, _node);
                    }
                    break;

                case TransitionRegexKind.Union:
                    Debug.Assert(_first is not null && _second is not null);
                    foreach ((TSet, SymbolicRegexNode<TSet>?, SymbolicRegexNode<TSet>) path in _first.EnumeratePaths(pathCondition))
                    {
                        yield return path;
                    }
                    foreach ((TSet, SymbolicRegexNode<TSet>?, SymbolicRegexNode<TSet>) path in _second.EnumeratePaths(pathCondition))
                    {
                        yield return path;
                    }
                    break;

                case TransitionRegexKind.Conditional:
                    Debug.Assert(_test is not null && _first is not null && _second is not null);
                    foreach ((TSet, SymbolicRegexNode<TSet>?, SymbolicRegexNode<TSet>) path in _first.EnumeratePaths(_builder._solver.And(pathCondition, _test)))
                    {
                        yield return path;
                    }
                    foreach ((TSet, SymbolicRegexNode<TSet>?, SymbolicRegexNode<TSet>) path in _second.EnumeratePaths(_builder._solver.And(pathCondition, _builder._solver.Not(_test))))
                    {
                        yield return path;
                    }
                    break;

                default:
                    Debug.Assert(_kind is TransitionRegexKind.Lookaround && _node is not null && _first is not null && _second is not null);
                    foreach ((TSet, SymbolicRegexNode<TSet>?, SymbolicRegexNode<TSet>) path in _first.EnumeratePaths(pathCondition))
                    {
                        SymbolicRegexNode<TSet> nullabilityTest = _node;
                        if (path.Item2 is not null)
                        {
                            nullabilityTest = _builder.And(path.Item2, nullabilityTest);
                        }
                        yield return (path.Item1, nullabilityTest, path.Item3);
                    }
                    foreach ((TSet, SymbolicRegexNode<TSet>?, SymbolicRegexNode<TSet>) path in _second.EnumeratePaths(pathCondition))
                    {
                        // Complement the nullability test
                        SymbolicRegexNode<TSet> nullabilityTest = _builder.Not(_node);
                        if (path.Item2 is not null)
                        {
                            nullabilityTest = _builder.And(path.Item2, nullabilityTest);
                        }
                        yield return (path.Item1, nullabilityTest, path.Item3);
                    }
                    break;
            }
        }
    }
}
#endif
