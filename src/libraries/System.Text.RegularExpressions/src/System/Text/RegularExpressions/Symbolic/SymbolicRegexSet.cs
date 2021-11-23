// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents a set of symbolic regexes that is either a disjunction or a conjunction</summary>
    internal sealed class SymbolicRegexSet<S> : IEnumerable<SymbolicRegexNode<S>> where S : notnull
    {
        internal readonly SymbolicRegexBuilder<S> _builder;

        private readonly HashSet<SymbolicRegexNode<S>> _set;

        /// <remarks>
        /// Symbolic regex A{0,k}?B is stored as (A,B,true) -> k  -- lazy
        /// Symbolic regex A{0,k}? is stored as (A,(),true) -> k  -- lazy
        /// Symbolic regex A{0,k}B is stored as (A,B,false) -> k  -- eager
        /// Symbolic regex A{0,k} is stored as (A,(),false) -> k  -- eager
        /// </remarks>
        private readonly Dictionary<(SymbolicRegexNode<S>, SymbolicRegexNode<S>, bool), int> _loops;

        /// <summary> the union (or intersection) of all singletons in the collection if any or null if none</summary>
        private readonly SymbolicRegexNode<S>? _singleton;

        internal readonly SymbolicRegexKind _kind;

        private int _hashCode;

        /// <summary>If >= 0 then the maximal length of a watchdog in the set</summary>
        internal int _watchdog = -1;

        private SymbolicRegexSet(SymbolicRegexBuilder<S> builder, SymbolicRegexKind kind, HashSet<SymbolicRegexNode<S>>? set, Dictionary<(SymbolicRegexNode<S>, SymbolicRegexNode<S>, bool), int>? loops, SymbolicRegexNode<S>? singleton)
        {
            Debug.Assert(kind is SymbolicRegexKind.And or SymbolicRegexKind.Or);
            Debug.Assert((set is null) == (loops is null));

            _builder = builder;
            _kind = kind;
            _set = set ?? new HashSet<SymbolicRegexNode<S>>();
            _loops = loops ?? new Dictionary<(SymbolicRegexNode<S>, SymbolicRegexNode<S>, bool), int>();
            _singleton = singleton;
        }

        /// <summary>Denotes the empty conjunction</summary>
        public bool IsEverything => _kind == SymbolicRegexKind.And && _set.Count == 0 && _loops.Count == 0 && _singleton == null;

        /// <summary>Denotes the empty disjunction</summary>
        public bool IsNothing => _kind == SymbolicRegexKind.Or && _set.Count == 0 && _loops.Count == 0 && _singleton == null;

        /// <summary>How many elements are there in this set</summary>
        public int Count => _set.Count + _loops.Count + (_singleton == null ? 0 : 1);

        /// <summary>True iff the set is a singleton</summary>
        public bool IsSingleton => Count == 1;

        internal static SymbolicRegexSet<S> CreateFull(SymbolicRegexBuilder<S> builder) => new SymbolicRegexSet<S>(builder, SymbolicRegexKind.And, null, null, null);

        internal static SymbolicRegexSet<S> CreateEmpty(SymbolicRegexBuilder<S> builder) => new SymbolicRegexSet<S>(builder, SymbolicRegexKind.Or, null, null, null);

        internal static SymbolicRegexSet<S> CreateMulti(SymbolicRegexBuilder<S> builder, IEnumerable<SymbolicRegexNode<S>> elems, SymbolicRegexKind kind)
        {
            // Loops contains the actual multi-set part of the collection
            var loops = new Dictionary<(SymbolicRegexNode<S>, SymbolicRegexNode<S>, bool), int>();

            // Other represents a normal set
            var other = new HashSet<SymbolicRegexNode<S>>();

            // Combination of singletons (when not null)
            SymbolicRegexNode<S>? singleton = null;

            int watchdog = -1;

            foreach (SymbolicRegexNode<S> elem in elems)
            {
                // Keep track of the maximal watchdog if this is a disjunction
                // this means for example if the regex is abc(3)|bc(2) and
                // the input is xxxabcyyy then two watchdogs will occur (3) and (2)
                // after reading c and the maximal one is taken
                // in a conjuctive setting this is undefined and the watchdog remains -1
                if (kind == SymbolicRegexKind.Or &&
                    elem._kind == SymbolicRegexKind.WatchDog && elem._lower > watchdog)
                {
                    watchdog = elem._lower;
                }

                #region start foreach
                if (elem == builder._anyStar)
                {
                    // .* is the absorbing element for disjunction
                    if (kind == SymbolicRegexKind.Or)
                    {
                        return builder._fullSet;
                    }
                }
                else if (elem == builder._nothing)
                {
                    // [] is the absorbing element for conjunction
                    if (kind == SymbolicRegexKind.And)
                    {
                        return builder._emptySet;
                    }
                }
                else
                {
                    switch (elem._kind)
                    {
                        case SymbolicRegexKind.And:
                        case SymbolicRegexKind.Or:
                            Debug.Assert(elem._alts is not null);
                            if (kind == elem._kind)
                            {
                                // Flatten the inner set
                                foreach (SymbolicRegexNode<S> alt in elem._alts)
                                {
                                    if (alt._kind == SymbolicRegexKind.Loop && alt._lower == 0)
                                    {
                                        AddLoopElement(builder, loops, other, alt, builder._epsilon, kind);
                                    }
                                    else
                                    {
                                        if (alt._kind == SymbolicRegexKind.Concat && alt._left!._kind == SymbolicRegexKind.Loop && alt._left._lower == 0)
                                        {
                                            Debug.Assert(alt._right is not null);
                                            AddLoopElement(builder, loops, other, alt._left, alt._right, kind);
                                        }
                                        else
                                        {
                                            if (alt._kind == SymbolicRegexKind.Singleton)
                                            {
                                                Debug.Assert(alt._set is not null);
                                                if (singleton is null)
                                                {
                                                    singleton = alt;
                                                }
                                                else
                                                {
                                                    Debug.Assert(singleton._kind == SymbolicRegexKind.Singleton && singleton._set is not null);
                                                    // Join the predicates either by Intersecting or Unioning
                                                    // which at the character predicate level translates to conjunction or disjunction in the underlying character algebra
                                                    S pred = kind == SymbolicRegexKind.Or ? builder._solver.Or(singleton._set, alt._set) : builder._solver.And(singleton._set, alt._set);
                                                    singleton = SymbolicRegexNode<S>.MkSingleton(builder, pred);
                                                }
                                            }
                                            else
                                            {
                                                other.Add(alt);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                other.Add(elem);
                            }
                            break;

                        case SymbolicRegexKind.Loop:
                            if (elem._lower == 0)
                            {
                                AddLoopElement(builder, loops, other, elem, builder._epsilon, kind);
                            }
                            else
                            {
                                other.Add(elem);
                            }
                            break;

                        case SymbolicRegexKind.Concat:
                            Debug.Assert(elem._left is not null && elem._right is not null);
                            if (elem._kind == SymbolicRegexKind.Concat && elem._left._kind == SymbolicRegexKind.Loop && elem._left._lower == 0)
                            {
                                AddLoopElement(builder, loops, other, elem._left, elem._right, kind);
                            }
                            else
                            {
                                other.Add(elem);
                            }
                            break;

                        case SymbolicRegexKind.Singleton:
                            Debug.Assert(elem._set is not null);
                            if (singleton is null)
                            {
                                singleton = elem;
                            }
                            else
                            {
                                Debug.Assert(singleton._kind == SymbolicRegexKind.Singleton && singleton._set is not null);
                                // Join the predicates either by Intersecting or Unioning
                                // which at the character predicate level translates to conjunction or disjunction in the underlying character algebra
                                S pred = kind == SymbolicRegexKind.Or ? builder._solver.Or(singleton._set, elem._set) : builder._solver.And(singleton._set, elem._set);
                                singleton = SymbolicRegexNode<S>.MkSingleton(builder, pred);
                            }
                            break;

                        default:
                            other.Add(elem);
                            break;
                    }
                }
                #endregion
            }

            // This optimization is only valid for a conjunction/intersection
            if (kind == SymbolicRegexKind.And && singleton is not null && singleton.Equals(builder._solver.False))
            {
                return builder._emptySet;
            }

            // The following is only valid for a disjunction/union
            if (kind == SymbolicRegexKind.Or)
            {
                // If any element of other is covered in loops then omit it
                var others1 = new HashSet<SymbolicRegexNode<S>>();
                foreach (SymbolicRegexNode<S> sr in other)
                {
                    // If there is an element A{0,m} then A is not needed because
                    // it is included by the loop due to the upper bound m > 0
                    if (loops.ContainsKey((sr, builder._epsilon, false)))
                    {
                        others1.Add(sr);
                    }
                }

                foreach (KeyValuePair<(SymbolicRegexNode<S>, SymbolicRegexNode<S>, bool), int> pair in loops)
                {
                    // If there is an element A{0,m}B then B is not needed because
                    // it is included by the concatenation due to the lower bound 0
                    if (other.Contains(pair.Key.Item2))
                    {
                        others1.Add(pair.Key.Item2);
                    }
                }

                other.ExceptWith(others1);
            }

            return
                other.Count != 0 || loops.Count != 0 || singleton is not null ? new SymbolicRegexSet<S>(builder, kind, other, loops, singleton) { _watchdog = watchdog } :
                kind == SymbolicRegexKind.Or ? builder._emptySet :
                builder._fullSet;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void AddLoopElement(
                SymbolicRegexBuilder<S> builder,
                Dictionary<(SymbolicRegexNode<S>, SymbolicRegexNode<S>, bool), int> loops,
                HashSet<SymbolicRegexNode<S>> other, SymbolicRegexNode<S> loop,
                SymbolicRegexNode<S> rest,
                SymbolicRegexKind kind)
            {
                if (loop._upper == 0 && rest.IsEpsilon)
                {
                    // In a set treat a loop with upper=lower=0 and no rest (no continuation after the loop)
                    // as () independent of whether it is lazy or eager
                    other.Add(builder._epsilon);
                }
                else
                {
                    Debug.Assert(loop._left is not null);
                    var key = (loop._left, rest, loop.IsLazy);
                    if (!loops.TryGetValue(key, out int count) ||
                        (kind == SymbolicRegexKind.Or ? count < loop._upper : count > loop._upper)) // If disjunction then map to the maximum of the upper bounds else to the minimum
                    {
                        loops[key] = loop._upper;
                    }
                }
            }
        }

        internal bool IsNullableFor(uint context)
        {
            Enumerator e = GetEnumerator();

            if (_kind == SymbolicRegexKind.Or)
            {
                // Some element must be nullable
                while (e.MoveNext())
                {
                    if (e.Current.IsNullableFor(context))
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                Debug.Assert(_kind == SymbolicRegexKind.And);

                // All elements must be nullable
                while (e.MoveNext())
                {
                    if (!e.Current.IsNullableFor(context))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                int hashCode = _kind.GetHashCode();

                if (_singleton is not null)
                {
                    hashCode ^= _singleton.GetHashCode();
                }

                foreach (SymbolicRegexNode<S> n in _set)
                {
                    hashCode ^= n.GetHashCode();
                }

                foreach (KeyValuePair<(SymbolicRegexNode<S>, SymbolicRegexNode<S>, bool), int> entry in _loops)
                {
                    hashCode ^= entry.Key.GetHashCode() + entry.Value.GetHashCode();
                }

                _hashCode = hashCode;
            }

            return _hashCode;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            if (obj is not SymbolicRegexSet<S> that ||
                _kind != that._kind ||
                _singleton is null && that._singleton is not null ||
                _singleton is not null && !_singleton.Equals(that._singleton) ||
                _set.Count != that._set.Count ||
                _loops.Count != that._loops.Count ||
                (_set.Count > 0 && !_set.SetEquals(that._set)))
            {
                return false;
            }

            foreach (KeyValuePair<(SymbolicRegexNode<S>, SymbolicRegexNode<S>, bool), int> c in _loops)
            {
                if (!that._loops.TryGetValue(c.Key, out int count) || !count.Equals(c.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public void ToString(StringBuilder sb)
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            if (IsNothing)
            {
                sb.Append(SymbolicRegexNode<S>.EmptyCharClass);
            }
            else if (IsEverything)
            {
                sb.Append(".*");
            }
            else
            {
                Enumerator enumerator = GetEnumerator();
                bool nonempty = enumerator.MoveNext();
                Debug.Assert(nonempty, "Collection must be nonempty because IsNothing is false and IsEverything is false");
                SymbolicRegexNode<S> node = enumerator.Current;
                if (!enumerator.MoveNext())
                {
                    // The collection only has one element
                    node.ToString(sb);
                }
                else
                {
                    // Union of two or more elements
                    sb.Append('(');
                    // Append the first two elements
                    node.ToString(sb);
                    // Using the operator & for intersection
                    char op = _kind == SymbolicRegexKind.Or ? '|' : '&';
                    sb.Append(op);
                    enumerator.Current.ToString(sb);
                    while (enumerator.MoveNext())
                    {
                        // Append all the remaining elements
                        sb.Append(op);
                        enumerator.Current.ToString(sb);
                    }
                    sb.Append(')');
                }
            }
        }

        internal SymbolicRegexSet<S> CreateDerivative(S elem, uint context)
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            return CreateMulti(_builder, MkDerivativesOfElems(elem, context), _kind);

            IEnumerable<SymbolicRegexNode<S>> MkDerivativesOfElems(S elem, uint context)
            {
                foreach (SymbolicRegexNode<S> s in this)
                {
                    yield return s.MkDerivative(elem, context);
                }
            }
        }

        internal SymbolicRegexSet<T> Transform<T>(SymbolicRegexBuilder<T> builderT, Func<S, T> predicateTransformer) where T : notnull
        {
            // This function is mutually recursive with the one in SymbolicRegexBuilder, which has stack overflow avoidance
            return SymbolicRegexSet<T>.CreateMulti(builderT, TransformElements(builderT, predicateTransformer), _kind);

            IEnumerable<SymbolicRegexNode<T>> TransformElements(SymbolicRegexBuilder<T> builderT, Func<S, T> predicateTransformer)
            {
                foreach (SymbolicRegexNode<S> sr in this)
                {
                    yield return _builder.Transform(sr, builderT, predicateTransformer);
                }
            }
        }

        internal SymbolicRegexNode<S> GetSingletonElement()
        {
            Debug.Assert(IsSingleton);

            Enumerator e = GetEnumerator();
            bool success = e.MoveNext();
            Debug.Assert(success);
            return e.Current;
        }

        internal SymbolicRegexSet<S> Reverse()
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            return CreateMulti(_builder, ReverseElements(), _kind);

            IEnumerable<SymbolicRegexNode<S>> ReverseElements()
            {
                foreach (SymbolicRegexNode<S> n in this)
                {
                    yield return n.Reverse();
                }
            }
        }

        internal bool StartsWithLoop(int upperBoundLowestValue)
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            foreach (SymbolicRegexNode<S> n in this)
            {
                if (n.StartsWithLoop(upperBoundLowestValue))
                {
                    return true;
                }
            }

            return false;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<SymbolicRegexNode<S>> IEnumerable<SymbolicRegexNode<S>>.GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        internal int GetFixedLength()
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            if (_loops.Count > 0)
            {
                return -1;
            }

            int length = -1;
            foreach (SymbolicRegexNode<S> node in _set)
            {
                int nodeLength = node.GetFixedLength();

                if (nodeLength == -1)
                {
                    return -1;
                }
                else if (length == -1)
                {
                    length = nodeLength;
                }
                else if (length != nodeLength)
                {
                    return -1;
                }
            }

            if (_singleton is not null && length != 1)
            {
                if (length == -1)
                {
                    length = 1;
                }
                else
                {
                    length = -1;
                }
            }

            return length;
        }

        /// <summary>Enumerates all symbolic regexes in the set</summary>
        internal struct Enumerator : IEnumerator<SymbolicRegexNode<S>>
        {
            private readonly SymbolicRegexSet<S> _set;
            private int _state; // 0 = return singleton, 1 == iterate set, 2 == iterate loops, 3 == done
            private SymbolicRegexNode<S>? _current;
            private HashSet<SymbolicRegexNode<S>>.Enumerator _setEnumerator;
            private Dictionary<(SymbolicRegexNode<S>, SymbolicRegexNode<S>, bool), int>.Enumerator _loopsEnumerator;

            internal Enumerator(SymbolicRegexSet<S> symbolicRegexSet)
            {
                _state = symbolicRegexSet._singleton is null ? 1 : 0;
                _set = symbolicRegexSet;
                _setEnumerator = symbolicRegexSet._set.GetEnumerator();
                _loopsEnumerator = symbolicRegexSet._loops.GetEnumerator();
                _current = null;
            }

            public SymbolicRegexNode<S> Current => _current!;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _state = 3;
                _setEnumerator.Dispose();
                _loopsEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                switch (_state)
                {
                    case 0:
                        Debug.Assert(_set._singleton is not null);
                        _current = _set._singleton;
                        _state = 1;
                        return true;

                    case 1:
                        if (_setEnumerator.MoveNext())
                        {
                            _current = _setEnumerator.Current;
                            return true;
                        }
                        _state = 2;
                        goto case 2;

                    case 2:
                        if (_loopsEnumerator.MoveNext())
                        {
                            // Recreate the symbolic regex from (body,rest)->k to body{0,k}rest
                            (SymbolicRegexNode<S> body, SymbolicRegexNode<S> rest, bool isLazy) = _loopsEnumerator.Current.Key;
                            int upper = _loopsEnumerator.Current.Value;
                            _current = _set._builder.MkConcat(_set._builder.MkLoop(body, isLazy, 0, upper), rest);
                            return true;
                        }
                        _state = 3;
                        goto default;

                    default:
                        _current = null!;
                        return false;
                }
            }

            public void Reset() => throw new NotSupportedException();
        }
    }
}
