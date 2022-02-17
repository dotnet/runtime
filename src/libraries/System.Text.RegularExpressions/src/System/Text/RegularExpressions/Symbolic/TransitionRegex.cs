﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents a symbolic derivative created from a symbolic regex without using minterms</summary>
    internal class TransitionRegex<S> : IEnumerable<(S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>)> where S : notnull
    {
        public readonly SymbolicRegexBuilder<S> _builder;
        public readonly TransitionRegexKind _kind;
        public readonly S? _test;
        public readonly TransitionRegex<S>? _first;
        public readonly TransitionRegex<S>? _second;
        public readonly SymbolicRegexNode<S>? _node;
        public readonly DerivativeEffect? _effect;

        private readonly int _hashCode;

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

        private TransitionRegex(SymbolicRegexBuilder<S> builder, TransitionRegexKind kind, S? test, TransitionRegex<S>? first, TransitionRegex<S>? second, SymbolicRegexNode<S>? node, DerivativeEffect? effect)
        {
            Debug.Assert(builder is not null);
            Debug.Assert(
                kind is TransitionRegexKind.Leaf && node is not null && Equals(test, default(S)) && first is null && second is null && effect is null ||
                kind is TransitionRegexKind.Conditional && test is not null && first is not null && second is not null && node is null && effect is null ||
                kind is TransitionRegexKind.Union && Equals(test, default(S)) && first is not null && second is not null && node is null && effect is null ||
                kind is TransitionRegexKind.OrderedUnion && Equals(test, default(S)) && first is not null && second is not null && node is null && effect is null ||
                kind is TransitionRegexKind.Lookaround && Equals(test, default(S)) && first is not null && second is not null && node is not null && effect is null ||
                kind is TransitionRegexKind.Effect && Equals(test, default(S)) && first is not null && second is null && node is null && effect is not null);
            _builder = builder;
            _kind = kind;
            _test = test;
            _first = first;
            _second = second;
            _node = node;
            _effect = effect;
            _hashCode = HashCode.Combine(kind, test, first, second, node);
        }

        private static TransitionRegex<S> Create(SymbolicRegexBuilder<S> builder, TransitionRegexKind kind, S? test, TransitionRegex<S>? one, TransitionRegex<S>? two, SymbolicRegexNode<S>? node, DerivativeEffect? effect = null)
        {
            // Keep transition regexes internalized using the builder
            (TransitionRegexKind, S?, TransitionRegex<S>?, TransitionRegex<S>?, SymbolicRegexNode<S>?, DerivativeEffect?) key = (kind, test, one, two, node, effect);
            TransitionRegex<S>? tr;
            if (!builder._trCache.TryGetValue(key, out tr))
            {
                tr = new TransitionRegex<S>(builder, kind, test, one, two, node, effect);
                builder._trCache[key] = tr;
            }
            return tr;
        }

        public TransitionRegex<S> Complement()
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Complement);
            }

            switch (_kind)
            {
                case TransitionRegexKind.Leaf:
                    Debug.Assert(_node is not null);
                    // Complement is propagated to the leaf
                    return Create(_builder, _kind, default(S), null, null, _node._builder.MkNot(_node));
                case TransitionRegexKind.Union:
                    Debug.Assert(_first is not null && _second is not null);
                    // Apply deMorgan's laws
                    return Intersect(_first.Complement(), _second.Complement());
                default:
                    Debug.Assert(_first is not null && _second is not null);
                    // Both Conditional and Nullability obey the same laws of propagation of complement
                    return Create(_builder, _kind, _test, _first.Complement(), _second.Complement(), _node);
            }
        }

        public static TransitionRegex<S> Leaf(SymbolicRegexNode<S> node) =>
            Create(node._builder, TransitionRegexKind.Leaf, default(S), null, null, node);

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
                    return Create(_builder, _kind, default(S), null, null, _node._builder.MkConcat(_node, node));
                case TransitionRegexKind.Effect:
                    Debug.Assert(_first is not null);
                    return Create(_builder, _kind, default(S), _first.Concat(node), null, null, _effect);
                default:
                    // All other three cases are disjunctive and obey the same laws of propagation of complement
                    Debug.Assert(_first is not null && _second is not null);
                    return Create(_builder, _kind, _test, _first.Concat(node), _second.Concat(node), _node);
            }
        }

        private static TransitionRegex<S> Intersect(TransitionRegex<S> one, TransitionRegex<S> two)
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
                return Create(_builder, TransitionRegexKind.Conditional, _test, thencase, elsecase, null);
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
            return Leaf(_builder.MkAnd(_node, that._node));
        }

        public static TransitionRegex<S> Union(TransitionRegex<S> one, TransitionRegex<S> two, bool ordered = false)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Union, one, two, ordered);
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

                // if(psi, t1, t2) | if(psi, s1, s2) = if(psi, t1|s1, t2|s2)
                if (one._test.Equals(two._test))
                {
                    return Conditional(one._test, Union(one._first, two._first, ordered), Union(one._second, two._second, ordered));
                }

                // if(psi, t, []) | if(phi, t, []) = if(psi or phi, t, [])
                if (one._second.IsNothing && two._second.IsNothing && one._first.Equals(two._first))
                {
                    return Conditional(one._builder._solver.Or(one._test, two._test), one._first, one._second);
                }
            }

            // TODO-NONBACKTRACKING: keep the representation of Union in right-associative form ordered by hashcode "as a list"
            // so that in a Union, _first is never a union and _first._hashcode is less than _second._hashcode (if _second is not a Union)
            // and if _second is a union then _first._hashcode is less than _second._first._hashcode, etc.
            // This will help to maintain a canonical representation of two equivalent unions and avoid equivalent unions being nonequal
            return Create(one._builder, ordered ? TransitionRegexKind.OrderedUnion : TransitionRegexKind.Union, default(S), one, two, null);
        }

        public static TransitionRegex<S> Conditional(S test, TransitionRegex<S> thencase, TransitionRegex<S> elsecase) =>
            (thencase == elsecase || thencase._builder._solver.True.Equals(test)) ? thencase :
            thencase._builder._solver.False.Equals(test) ? elsecase :
            Create(thencase._builder, TransitionRegexKind.Conditional, test, thencase, elsecase, null);

        public static TransitionRegex<S> Lookaround(SymbolicRegexNode<S> nullabilityTest, TransitionRegex<S> thencase, TransitionRegex<S> elsecase) =>
            (thencase == elsecase) ? thencase : Create(thencase._builder, TransitionRegexKind.Lookaround, default(S), thencase, elsecase, nullabilityTest);

        public static TransitionRegex<S> Effect(TransitionRegex<S> child, DerivativeEffect effect)
        {
            if (child.IsNothing)
                return child;

            return Create(child._builder, TransitionRegexKind.Effect, default(S), child, null, null, effect);
        }

        /// <summary>Intersection of transition regexes</summary>
        public static TransitionRegex<S> operator &(TransitionRegex<S> one, TransitionRegex<S> two) => Intersect(one, two);

        /// <summary>Union of transition regexes</summary>
        public static TransitionRegex<S> operator |(TransitionRegex<S> one, TransitionRegex<S> two) => Union(one, two);

        /// <summary>Complement of transition regex</summary>
        public static TransitionRegex<S> operator ~(TransitionRegex<S> tr) => tr.Complement();

        public override int GetHashCode() => _hashCode;

        public override string ToString()
        {
            switch (_kind)
            {
                case TransitionRegexKind.Leaf:
                    return $"{_node}";
                case TransitionRegexKind.Union:
                    return $"{_first}|{_second}";
                case TransitionRegexKind.OrderedUnion:
                    return $"{_first}||{_second}";
                case TransitionRegexKind.Conditional:
                    return $"if({_test},{_first},{_second})";
                case TransitionRegexKind.Effect:
                    switch (_effect?.Kind)
                    {
                        case DerivativeEffect.EffectKind.CaptureStart:
                            return $"captureStart({_effect?.CaptureNumber},{_first})";
                        default:
                            return $"captureEnd({_effect?.CaptureNumber},{_first})";
                    }
                default:
                    return $"if(IsNull({_node}),{_first},{_second})";
            }
        }

        public IEnumerator<(S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>)> GetEnumerator() => EnumeratePaths(_builder._solver.True).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => EnumeratePaths(_builder._solver.True).GetEnumerator();

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
                case TransitionRegexKind.OrderedUnion:
                    Debug.Assert(_first is not null && _second is not null);
                    foreach (var path in _first.EnumeratePaths(pathCondition))
                    {
                        yield return path;
                    }
                    foreach (var path in _second.EnumeratePaths(pathCondition))
                    {
                        yield return path;
                    }
                    break;

                case TransitionRegexKind.Conditional:
                    Debug.Assert(_test is not null && _first is not null && _second is not null);
                    foreach (var path in _first.EnumeratePaths(_builder._solver.And(pathCondition, _test)))
                    {
                        yield return path;
                    }
                    foreach (var path in _second.EnumeratePaths(_builder._solver.And(pathCondition, _builder._solver.Not(_test))))
                    {
                        yield return path;
                    }
                    break;

                default:
                    Debug.Assert(_kind is TransitionRegexKind.Lookaround && _node is not null && _first is not null && _second is not null);
                    foreach (var path in _first.EnumeratePaths(pathCondition))
                    {
                        SymbolicRegexNode<S> nullabilityTest = path.Item2 is null ? _node : _builder.MkAnd(path.Item2, _node);
                        yield return (path.Item1, nullabilityTest, path.Item3);
                    }
                    foreach (var path in _second.EnumeratePaths(pathCondition))
                    {
                        // Complement the nullability test
                        SymbolicRegexNode<S> nullabilityTest = path.Item2 is null ? _builder.MkNot(_node) : _builder.MkAnd(path.Item2, _builder.MkNot(_node));
                        yield return (path.Item1, nullabilityTest, path.Item3);
                    }
                    break;
            }
        }

        /// <summary>Enumerate all distinct leaves that are not nothing.</summary>
        public IEnumerable<TransitionRegex<S>> EnumerateLeaves()
        {
            Stack<TransitionRegex<S>> todo = new();
            HashSet<TransitionRegex<S>> done = new();
            todo.Push(this);
            while (todo.Count > 0)
            {
                TransitionRegex<S> top = todo.Pop();
                switch (top._kind)
                {
                    case TransitionRegexKind.Leaf:
                        // Omit any leaf that is nothing or has already been yielded
                        if (!top.IsNothing && done.Add(top))
                        {
                            yield return top;
                        }
                        break;

                    default:
                        Debug.Assert(top._first is not null && top._second is not null);
                        // In general the structure is a DAG so avoid yielding duplicates
                        if (done.Add(top._second))
                        {
                            todo.Push(top._second);
                        }
                        if (done.Add(top._first))
                        {
                            todo.Push(top._first);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Build the union of all leaves that are reachable with the given minterm and context.
        /// </summary>
        /// <param name="minterm">the minterm of the next character</param>
        /// <param name="context">the current context</param>
        /// <returns>a union of leaves</returns>
        public SymbolicRegexNode<S> Transition(S minterm, uint context)
        {
            // Collect the union of all target leaves
            SymbolicRegexNode<S> target = _builder._nothing;
            Stack<TransitionRegex<S>> todo = new();
            todo.Push(this);
            while (todo.Count > 0)
            {
                TransitionRegex<S> top = todo.Pop();
                switch (top._kind)
                {
                    case TransitionRegexKind.Leaf:
                        Debug.Assert(top._node is not null);
                        target = _builder.MkOr2(target, top._node);
                        break;

                    case TransitionRegexKind.Conditional:
                        Debug.Assert(top._test is not null && top._first is not null && top._second is not null);
                        if (_builder._solver.IsSatisfiable(_builder._solver.And(minterm, top._test)))
                        {
                            if (!top._first.IsNothing)
                            {
                                todo.Push(top._first);
                            }
                        }
                        else
                        {
                            if (!top._second.IsNothing)
                            {
                                todo.Push(top._second);
                            }
                        }
                        break;

                    case TransitionRegexKind.Union:
                    case TransitionRegexKind.OrderedUnion:
                        // Observe that without Union Transition returns excatly one of the leaves
                        Debug.Assert(top._first is not null && top._second is not null);
                        todo.Push(top._second);
                        todo.Push(top._first);
                        break;

                    case TransitionRegexKind.Effect:
                        // Effects are ignored here
                        Debug.Assert(top._first is not null && top._effect is not null);
                        todo.Push(top._first);
                        break;

                    default:
                        Debug.Assert(top._kind is TransitionRegexKind.Lookaround && top._node is not null && top._first is not null && top._second is not null);
                        // Branch according to the result of nullability
                        if (top._node.IsNullableFor(context))
                        {
                            if (!top._first.IsNothing)
                            {
                                todo.Push(top._first);
                            }
                        }
                        else
                        {
                            if (!top._second.IsNothing)
                            {
                                todo.Push(top._second);
                            }
                        }
                        break;
                }
            }
            return target;
        }

        /// <summary>
        /// Build the union of all leaves that are reachable with the given minterm and context.
        /// </summary>
        /// <remarks>
        /// This version respects the difference between Union and OrderedUnion, translating them to Or and OrderedOr
        /// nodes respectively.
        /// </remarks>
        /// <param name="minterm">the minterm of the next character</param>
        /// <param name="context">the current context</param>
        /// <returns>a union of leaves</returns>
        public SymbolicRegexNode<S> TransitionOrdered(S minterm, uint context)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(TransitionOrdered, minterm, context);
            }

            switch (_kind)
            {
                case TransitionRegexKind.Leaf:
                    Debug.Assert(_node is not null);
                    return _node;

                case TransitionRegexKind.Conditional:
                    Debug.Assert(_test is not null && _first is not null && _second is not null);
                    if (_builder._solver.IsSatisfiable(_builder._solver.And(minterm, _test)))
                    {
                        if (!_first.IsNothing)
                        {
                            return _first.TransitionOrdered(minterm, context);
                        }
                    }
                    else
                    {
                        if (!_second.IsNothing)
                        {
                            return _second.TransitionOrdered(minterm, context);
                        }
                    }
                    break;

                case TransitionRegexKind.Union:
                    // Observe that without Union Transition returns excatly one of the leaves
                    Debug.Assert(_first is not null && _second is not null);
                    return _builder.MkOr2(_first.TransitionOrdered(minterm, context), _second.TransitionOrdered(minterm, context));

                case TransitionRegexKind.OrderedUnion:
                    // Observe that without Union Transition returns excatly one of the leaves
                    Debug.Assert(_first is not null && _second is not null);
                    return _builder.MkOrderedOr(_first.TransitionOrdered(minterm, context), _second.TransitionOrdered(minterm, context));

                case TransitionRegexKind.Effect:
                    // Effects are ignored here
                    Debug.Assert(_first is not null && _effect is not null);
                    return _first.TransitionOrdered(minterm, context);

                default:
                    Debug.Assert(_kind is TransitionRegexKind.Lookaround && _node is not null && _first is not null && _second is not null);
                    // Branch according to the result of nullability
                    if (_node.IsNullableFor(context))
                    {
                        if (!_first.IsNothing)
                        {
                            return _first.TransitionOrdered(minterm, context);
                        }
                    }
                    else
                    {
                        if (!_second.IsNothing)
                        {
                            return _second.TransitionOrdered(minterm, context);
                        }
                    }
                    break;
            }
            return _builder._nothing;
        }

        /// <summary>
        /// Enumerate the leaves reachable with a given minterm and context, and collect the effects on the path to each leaf.
        /// Any transitions after the first unconditionally nullable one are ignored, as the backtracking engines would never
        /// take a path corresponding to those transitions.
        /// </summary>
        /// <param name="minterm">the minterm of the next character</param>
        /// <param name="context">the current context</param>
        /// <returns>an enumeration of pairs of leaves and the effects leading to them</returns>
        public IEnumerable<(SymbolicRegexNode<S>, List<DerivativeEffect>)> TransitionsWithEffects(S minterm, uint context)
        {
            // Collect all target leaves with their effects
            Stack<(TransitionRegex<S>, List<DerivativeEffect>)> todo = new();
            todo.Push((this, new List<DerivativeEffect>()));
            while (todo.Count > 0)
            {
                (TransitionRegex<S> top, List<DerivativeEffect> effects) = todo.Pop();
                switch (top._kind)
                {
                    case TransitionRegexKind.Leaf:
                        Debug.Assert(top._node is not null);
                        yield return (top._node, effects);
                        // If the leaf is nullable lower priority transitions would never get used anyway, so stop here
                        if (top._node.IsNullable)
                            yield break;
                        break;

                    case TransitionRegexKind.Conditional:
                        Debug.Assert(top._test is not null && top._first is not null && top._second is not null);
                        if (_builder._solver.IsSatisfiable(_builder._solver.And(minterm, top._test)))
                        {
                            if (!top._first.IsNothing)
                            {
                                todo.Push((top._first, new List<DerivativeEffect>(effects)));
                            }
                        }
                        else
                        {
                            if (!top._second.IsNothing)
                            {
                                todo.Push((top._second, effects));
                            }
                        }
                        break;

                    case TransitionRegexKind.Union:
                    case TransitionRegexKind.OrderedUnion:
                        Debug.Assert(top._first is not null && top._second is not null);
                        todo.Push((top._second, new List<DerivativeEffect>(effects)));
                        todo.Push((top._first, effects));
                        break;

                    case TransitionRegexKind.Effect:
                        Debug.Assert(top._first is not null && top._effect is not null);
                        effects.Add((DerivativeEffect)top._effect);
                        todo.Push((top._first, effects));
                        break;

                    default:
                        Debug.Assert(top._kind is TransitionRegexKind.Lookaround && top._node is not null && top._first is not null && top._second is not null);
                        // Branch according to the result of nullability
                        if (top._node.IsNullableFor(context))
                        {
                            if (!top._first.IsNothing)
                            {
                                todo.Push((top._first, new List<DerivativeEffect>(effects)));
                            }
                        }
                        else
                        {
                            if (!top._second.IsNothing)
                            {
                                todo.Push((top._second, effects));
                            }
                        }
                        break;
                }
            }
        }
    }
}
