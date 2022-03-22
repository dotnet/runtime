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
    internal sealed class TransitionRegex<S> where S : notnull
    {
        public readonly SymbolicRegexBuilder<S> _builder;
        public readonly TransitionRegexKind _kind;
        public readonly S? _test;
        public readonly TransitionRegex<S>? _first;
        public readonly TransitionRegex<S>? _second;
        public readonly SymbolicRegexNode<S>? _node;
        public readonly DerivativeEffect? _effect;

        private TransitionRegex(SymbolicRegexBuilder<S> builder, TransitionRegexKind kind, S? test, TransitionRegex<S>? first, TransitionRegex<S>? second, SymbolicRegexNode<S>? node, DerivativeEffect? effect)
        {
            Debug.Assert(builder is not null);
            Debug.Assert(
                kind is TransitionRegexKind.Leaf && node is not null && Equals(test, default(S)) && first is null && second is null && effect is null ||
                kind is TransitionRegexKind.Conditional && test is not null && first is not null && second is not null && node is null && effect is null ||
                kind is TransitionRegexKind.Union && Equals(test, default(S)) && first is not null && second is not null && node is null && effect is null ||
                kind is TransitionRegexKind.Lookaround && Equals(test, default(S)) && first is not null && second is not null && node is not null && effect is null ||
                kind is TransitionRegexKind.Effect && Equals(test, default(S)) && first is not null && second is null && node is null && effect is not null);

            _builder = builder;
            _kind = kind;
            _test = test;
            _first = first;
            _second = second;
            _node = node;
            _effect = effect;
        }

        private static TransitionRegex<S> GetOrCreate(SymbolicRegexBuilder<S> builder, TransitionRegexKind kind, S? test, TransitionRegex<S>? one, TransitionRegex<S>? two, SymbolicRegexNode<S>? node, DerivativeEffect? effect = null)
        {
            // Keep transition regexes internalized using the builder
            ref TransitionRegex<S>? tr = ref CollectionsMarshal.GetValueRefOrAddDefault(builder._trCache, (kind, test, one, two, node, effect), out _);
            return tr ??= new TransitionRegex<S>(builder, kind, test, one, two, node, effect);
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
        public TransitionRegex<S> Complement()
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
                    return GetOrCreate(_builder, _kind, default(S), null, null, _node._builder.Not(_node));

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

        public static TransitionRegex<S> Leaf(SymbolicRegexNode<S> node) =>
            GetOrCreate(node._builder, TransitionRegexKind.Leaf, default(S), null, null, node);

        /// <summary>Concatenate a node at the end of this transition regex</summary>
        public TransitionRegex<S> Concat(SymbolicRegexNode<S> node)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Concat, node);
            }

            switch (_kind)
            {
                case TransitionRegexKind.Leaf:
                    Debug.Assert(_node is not null);
                    return GetOrCreate(_builder, _kind, default(S), null, null, _node._builder.CreateConcat(_node, node));

                case TransitionRegexKind.Effect:
                    Debug.Assert(_first is not null);
                    return GetOrCreate(_builder, _kind, default(S), _first.Concat(node), null, null, _effect);

                default:
                    // All other three cases are disjunctive and obey the same laws of propagation of complement
                    Debug.Assert(_first is not null && _second is not null);
                    return GetOrCreate(_builder, _kind, _test, _first.Concat(node), _second.Concat(node), _node);
            }
        }

        /// <summary>Intersection of transition regexes</summary>
        public static TransitionRegex<S> Intersect(TransitionRegex<S> one, TransitionRegex<S> two)
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

            return one.IntersectWith(two, one._builder._solver.True);
        }

        private TransitionRegex<S> IntersectWith(TransitionRegex<S> that, S pathIn)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(IntersectWith, that, pathIn);
            }

            Debug.Assert(_builder._solver.IsSatisfiable(pathIn));

#region Conditional
            // Intersect when this is a Conditional
            if (_kind == TransitionRegexKind.Conditional)
            {
                Debug.Assert(_test is not null && _first is not null && _second is not null);
                S thenPath = _builder._solver.And(pathIn, _test);
                S elsePath = _builder._solver.And(pathIn, _builder._solver.Not(_test));

                if (!_builder._solver.IsSatisfiable(thenPath))
                {
                    // then case being infeasible implies that elsePath must be satisfiable
                    return _second.IntersectWith(that, elsePath);
                }

                if (!_builder._solver.IsSatisfiable(elsePath))
                {
                    // else case is infeasible
                    return _first.IntersectWith(that, thenPath);
                }

                TransitionRegex<S> thencase = _first.IntersectWith(that, thenPath);
                TransitionRegex<S> elsecase = _second.IntersectWith(that, elsePath);
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
        public static TransitionRegex<S> Union(TransitionRegex<S> one, TransitionRegex<S> two)
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

            return GetOrCreate(one._builder, TransitionRegexKind.Union, default(S), one, two, null);
        }

        public static TransitionRegex<S> Conditional(S test, TransitionRegex<S> thencase, TransitionRegex<S> elsecase) =>
            (thencase == elsecase || thencase._builder._solver.True.Equals(test)) ? thencase :
            thencase._builder._solver.False.Equals(test) ? elsecase :
            GetOrCreate(thencase._builder, TransitionRegexKind.Conditional, test, thencase, elsecase, null);

        public static TransitionRegex<S> Lookaround(SymbolicRegexNode<S> nullabilityTest, TransitionRegex<S> thencase, TransitionRegex<S> elsecase) =>
            (thencase == elsecase) ? thencase : GetOrCreate(thencase._builder, TransitionRegexKind.Lookaround, default(S), thencase, elsecase, nullabilityTest);

        public static TransitionRegex<S> Effect(TransitionRegex<S> child, DerivativeEffect effect) =>
            child.IsNothing ? child :
            GetOrCreate(child._builder, TransitionRegexKind.Effect, default(S), child, null, null, effect);

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
        public IEnumerable<(S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>)> EnumeratePaths(S pathCondition)
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
                    foreach ((S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>) path in _first.EnumeratePaths(pathCondition))
                    {
                        yield return path;
                    }
                    foreach ((S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>) path in _second.EnumeratePaths(pathCondition))
                    {
                        yield return path;
                    }
                    break;

                case TransitionRegexKind.Conditional:
                    Debug.Assert(_test is not null && _first is not null && _second is not null);
                    foreach ((S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>) path in _first.EnumeratePaths(_builder._solver.And(pathCondition, _test)))
                    {
                        yield return path;
                    }
                    foreach ((S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>) path in _second.EnumeratePaths(_builder._solver.And(pathCondition, _builder._solver.Not(_test))))
                    {
                        yield return path;
                    }
                    break;

                default:
                    Debug.Assert(_kind is TransitionRegexKind.Lookaround && _node is not null && _first is not null && _second is not null);
                    foreach ((S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>) path in _first.EnumeratePaths(pathCondition))
                    {
                        SymbolicRegexNode<S> nullabilityTest = _node;
                        if (path.Item2 is not null)
                        {
                            nullabilityTest = _builder.And(path.Item2, nullabilityTest);
                        }
                        yield return (path.Item1, nullabilityTest, path.Item3);
                    }
                    foreach ((S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>) path in _second.EnumeratePaths(pathCondition))
                    {
                        // Complement the nullability test
                        SymbolicRegexNode<S> nullabilityTest = _builder.Not(_node);
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
